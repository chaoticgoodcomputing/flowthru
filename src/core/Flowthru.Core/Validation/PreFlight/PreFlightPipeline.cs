using System.Diagnostics;
using Flowthru.Data.Catalog;
using Flowthru.Data.Storage;
using Flowthru.Diagnostics;
using Flowthru.Flow;
using Flowthru.Validation.Runtime;

namespace Flowthru.Validation.PreFlight;

/// <summary>
/// Runs the three pre-flight contribution layers against a built
/// flow and combines their outcomes via
/// <see cref="Validated.ZipAll{TError, TValue}"/>: adapter-internal
/// inspections of every input item, registered
/// <see cref="IFlowValidationHook"/>s, and any caller-supplied
/// service inspections. The user sees every problem at once, not
/// one error per re-run.
/// </summary>
/// <remarks>
/// <para>
/// Per §2.5, the pre-flight pipeline is the system-level invariant
/// that a flow which passes pre-flight should always complete
/// successfully. Adding a new closed <see cref="PreFlightError"/>
/// case is the way Core lifts a runtime invariant into a build-time
/// or pre-run check.
/// </para>
/// </remarks>
public static class PreFlightPipeline
{
  /// <summary>
  /// Run every layer for <paramref name="flow"/>. The adapter layer
  /// is always run; <paramref name="hooks"/>,
  /// <paramref name="serviceProbes"/>, and
  /// <paramref name="serviceRefDispatchers"/> default to empty.
  /// </summary>
  /// <param name="flow">The built flow whose external inputs are inspected.</param>
  /// <param name="hooks">Flow-level validation hooks; null = none.</param>
  /// <param name="serviceProbes">Service-inspector probes; null = none.</param>
  /// <param name="serviceRefDispatchers">
  /// Extension-supplied dispatchers for
  /// <see cref="ServiceRef.External"/> service references. Each dispatcher
  /// declares the <see cref="IServiceRefDispatcher.Category"/> it handles;
  /// the pipeline routes every external service ref encountered in the
  /// flow's <c>ServiceDependencies</c> to its matching dispatcher's
  /// <see cref="IServiceRefDispatcher.Inspect"/>. A category with no
  /// registered dispatcher surfaces as
  /// <see cref="PreFlightError.RegistrationCheckFailed"/>.
  /// </param>
  /// <param name="inspectionLevel">Adapter inspection depth.</param>
  /// <param name="maxDegreeOfParallelism">
  /// Maximum number of adapter inspections to run concurrently. The
  /// default <c>1</c> preserves the historical sequential behaviour
  /// (deterministic ordering, no concurrency); values <c>&gt; 1</c>
  /// dispatch up to N inspections in flight at once. Errors aggregate
  /// regardless of concurrency — none are dropped.
  /// </param>
  public static FlowIO<Validated<PreFlightError, FlowUnit>> Run(
    BuiltFlow flow,
    IReadOnlyList<IFlowValidationHook>? hooks = null,
    IReadOnlyList<FlowIO<Validated<PreFlightError, FlowUnit>>>? serviceProbes = null,
    IReadOnlyList<IServiceRefDispatcher>? serviceRefDispatchers = null,
    InspectionLevel inspectionLevel = InspectionLevel.Shallow,
    int maxDegreeOfParallelism = 1
  )
  {
    if (flow is null) throw new ArgumentNullException(nameof(flow));
    if (maxDegreeOfParallelism < 1)
    {
      throw new ArgumentOutOfRangeException(
        nameof(maxDegreeOfParallelism),
        maxDegreeOfParallelism,
        "maxDegreeOfParallelism must be >= 1."
      );
    }

    return FlowIO.LiftAsync(async ct =>
    {
      using var activity = FlowthruActivitySource.Source.StartActivity(
        FlowthruActivitySource.PreFlightActivityName,
        ActivityKind.Internal
      );

      var aggregated = new List<Validated<PreFlightError, FlowUnit>>();

      // Layer 1 — adapter-internal inspection of every external input. An
      // "external" input is one whose label is NOT produced by any step in
      // this flow — intermediate items that some upstream step writes
      // won't exist until the flow runs, so inspecting them at pre-flight
      // time is a category error.
      var producedItemLabels = new HashSet<string>(
        flow.Steps.SelectMany(s => s.Outputs.Select(o => o.Label)),
        StringComparer.Ordinal
      );

      // Collect every distinct external input so each adapter is
      // inspected at most once even if more than one step consumes it.
      var externalInputs = new List<IItem>();
      var seenInputLabels = new HashSet<string>(StringComparer.Ordinal);
      foreach (var step in flow.Steps)
      {
        foreach (var input in step.Inputs)
        {
          if (producedItemLabels.Contains(input.Label)) continue;
          if (!seenInputLabels.Add(input.Label)) continue;
          externalInputs.Add(input);
        }
      }

      // Per-input inspection — runs the level-appropriate Inspect* IO
      // and lifts the ValidationResult into a Validated.
      async Task<Validated<PreFlightError, FlowUnit>> InspectOne(IItem input)
      {
        // Per-item cap: when the item declares a ceiling via
        // IItem<T>.WithMaxInspectionLevel(...), the effective level
        // is the tighter of the caller-requested level and the cap.
        // Items without a cap run at the caller-requested level.
        var effectiveLevel = input.MaxInspectionLevel is { } cap
          ? (InspectionLevel)Math.Min((int)inspectionLevel, (int)cap)
          : inspectionLevel;

        // Skip the item entirely when the cap drives effective to
        // None — lets a catalog author flag "trust the producer;
        // don't probe" on a per-item basis.
        if (effectiveLevel == InspectionLevel.None)
        {
          return Validated<PreFlightError, FlowUnit>.Pure(FlowUnit.Default);
        }

        var inspectIO = effectiveLevel switch
        {
          InspectionLevel.Deep => input.InspectDeep(),
          InspectionLevel.Target => input.InspectTarget(),
          _ => input.InspectShallow(),
        };
        var inspectResult = await inspectIO.Run(ct).ConfigureAwait(false);
        return inspectResult switch
        {
          EffResult<ValidationResult>.Success ok =>
            ToValidated(ok.Value, input.Label),
          EffResult<ValidationResult>.Failure failure =>
            Validated<PreFlightError, FlowUnit>.Fail(
              new PreFlightError.InspectionFailed(input.Label, failure.Error.Message)
            ),
          _ => throw new InvalidOperationException("Unreachable: EffResult is a closed sum"),
        };
      }

      if (maxDegreeOfParallelism == 1 || externalInputs.Count <= 1)
      {
        // Sequential fast path — preserves deterministic ordering.
        foreach (var input in externalInputs)
        {
          aggregated.Add(await InspectOne(input).ConfigureAwait(false));
        }
      }
      else
      {
        // Bounded parallel fan-out. SemaphoreSlim caps in-flight
        // inspections at maxDegreeOfParallelism; we don't rely on
        // Parallel.ForEachAsync so we keep async-friendly semantics
        // (each FlowIO awaits independently) and aggregate
        // every result regardless of failures elsewhere.
        var gate = new SemaphoreSlim(maxDegreeOfParallelism, maxDegreeOfParallelism);
        var tasks = new List<Task<Validated<PreFlightError, FlowUnit>>>(externalInputs.Count);
        foreach (var input in externalInputs)
        {
          var captured = input;
          tasks.Add(Task.Run(async () =>
          {
            await gate.WaitAsync(ct).ConfigureAwait(false);
            try
            {
              return await InspectOne(captured).ConfigureAwait(false);
            }
            finally
            {
              gate.Release();
            }
          }, ct));
        }
        var perInput = await Task.WhenAll(tasks).ConfigureAwait(false);
        aggregated.AddRange(perInput);
      }

      // Layer 2 — caller-supplied flow validation hooks.
      if (hooks is not null)
      {
        foreach (var hook in hooks)
        {
          var result = await hook.Validate(flow).Run(ct).ConfigureAwait(false);
          aggregated.Add(result switch
          {
            EffResult<Validated<PreFlightError, FlowUnit>>.Success ok => ok.Value,
            EffResult<Validated<PreFlightError, FlowUnit>>.Failure failure =>
              Validated<PreFlightError, FlowUnit>.Fail(
                new PreFlightError.InspectionFailed(hook.HookId, failure.Error.Message)
              ),
            _ => throw new InvalidOperationException("Unreachable: EffResult is a closed sum"),
          });
        }
      }

      // Layer 3 — caller-supplied service-inspector probes.
      if (serviceProbes is not null)
      {
        foreach (var probe in serviceProbes)
        {
          var result = await probe.Run(ct).ConfigureAwait(false);
          aggregated.Add(result switch
          {
            EffResult<Validated<PreFlightError, FlowUnit>>.Success ok => ok.Value,
            EffResult<Validated<PreFlightError, FlowUnit>>.Failure failure =>
              Validated<PreFlightError, FlowUnit>.Fail(
                new PreFlightError.InspectionFailed("service-probe", failure.Error.Message)
              ),
            _ => throw new InvalidOperationException("Unreachable: EffResult is a closed sum"),
          });
        }
      }

      // Layer 4 — dispatcher-resolved external service refs. Walk every
      // step's ServiceDependencies, filter to ServiceRef.External, dedupe
      // by DagId so each unique extension service is probed at most once
      // per flow, then route to the dispatcher registered for the ref's
      // Category. An external category with no registered dispatcher is
      // a registration error — extensions that introduce a service-ref
      // category must also register a dispatcher to resolve it. The loop
      // runs unconditionally: zero dispatchers + zero External refs is a
      // no-op; zero dispatchers + any External ref surfaces as
      // RegistrationCheckFailed (the fail-loud default for incomplete
      // extension wiring).
      {
        // Build a category → dispatcher index up front; last-write-wins
        // on duplicate categories (matches the IServiceRefDispatcher
        // contract: "categories must be unique").
        var dispatchersByCategory = new Dictionary<string, IServiceRefDispatcher>(
          StringComparer.Ordinal
        );
        if (serviceRefDispatchers is not null)
        {
          foreach (var dispatcher in serviceRefDispatchers)
          {
            dispatchersByCategory[dispatcher.Category] = dispatcher;
          }
        }

        var seenServiceRefIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (var step in flow.Steps)
        {
          foreach (var dependency in step.ServiceDependencies)
          {
            if (dependency is not ServiceRef.External external) continue;
            if (!seenServiceRefIds.Add(external.Cause.DagId)) continue;

            if (!dispatchersByCategory.TryGetValue(external.Cause.Category, out var dispatcher))
            {
              aggregated.Add(Validated<PreFlightError, FlowUnit>.Fail(
                new PreFlightError.RegistrationCheckFailed(
                  HookId: $"service-ref-dispatch:{external.Cause.Category}",
                  CheckMessage: $"No IServiceRefDispatcher registered for category "
                    + $"'{external.Cause.Category}' (referenced by service ref "
                    + $"'{external.Cause.DagId}' on step '{step.Label}').",
                  Details: "Extensions that introduce a ServiceRef.External category must also "
                    + "register an IServiceRefDispatcher with that Category via DI."
                )
              ));
              continue;
            }

            var result = await dispatcher.Inspect(external.Cause).Run(ct).ConfigureAwait(false);
            aggregated.Add(result switch
            {
              EffResult<Validated<PreFlightError, FlowUnit>>.Success ok => ok.Value,
              EffResult<Validated<PreFlightError, FlowUnit>>.Failure failure =>
                Validated<PreFlightError, FlowUnit>.Fail(
                  new PreFlightError.InspectionFailed(external.Cause.DagId, failure.Error.Message)
                ),
              _ => throw new InvalidOperationException("Unreachable: EffResult is a closed sum"),
            });
          }
        }
      }

      var combined = Validated.ZipAll(aggregated).Map(_ => FlowUnit.Default);
      if (combined is Validated<PreFlightError, FlowUnit>.Invalid invalid)
      {
        activity?.SetTag(FlowthruActivitySource.TagPreFlightErrorCount, invalid.Errors.Count);
        activity?.SetStatus(ActivityStatusCode.Error, $"{invalid.Errors.Count} pre-flight error(s)");
      }
      else
      {
        activity?.SetStatus(ActivityStatusCode.Ok);
      }
      return combined;
    });
  }

  /// <summary>
  /// Translate a <see cref="ValidationResult"/> emitted by an adapter
  /// into a <see cref="Validated{TError, TValue}"/> over
  /// <see cref="PreFlightError"/>. Each
  /// <see cref="ValidationError"/> maps to the closest matching
  /// closed-sum case.
  /// </summary>
  private static Validated<PreFlightError, FlowUnit> ToValidated(
    ValidationResult result,
    string itemLabel
  )
  {
    if (result.IsValid) return Validated<PreFlightError, FlowUnit>.Pure(FlowUnit.Default);
    var errors = result.Errors.Select<ValidationError, PreFlightError>(e => e.ErrorType switch
    {
      ValidationErrorType.NotFound =>
        new PreFlightError.MissingInput(itemLabel, e.Message),
      ValidationErrorType.SchemaMismatch =>
        new PreFlightError.SchemaDrift(itemLabel, "expected schema", e.Message),
      _ => new PreFlightError.InspectionFailed(itemLabel, e.Message),
    }).ToList();
    return new Validated<PreFlightError, FlowUnit>.Invalid(errors);
  }
}

