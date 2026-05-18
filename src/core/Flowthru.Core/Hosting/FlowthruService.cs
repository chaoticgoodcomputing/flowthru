using System.Diagnostics;
using Flowthru.Caching;
using Flowthru.Data.Catalog;
using Flowthru.Data.Storage;
using Flowthru.Diagnostics;
using Flowthru.Flow;
using Flowthru.Step;
using Microsoft.Extensions.DependencyInjection;

namespace Flowthru.Hosting;

/// <summary>
/// Concrete runtime that resolves catalogs from DI, materialises
/// flows, runs the pre-flight pipeline, executes flows, and
/// orchestrates metadata providers. Resolved via DI as
/// <see cref="IFlowthruService"/>; consumers (CLI, hosted apps) call
/// <c>RunAsync</c> with an optional flow label and execution
/// options.
/// </summary>
/// <remarks>
/// <para>
/// Per §2.4, "all flows registered with the same FlowthruService
/// merge into a single DAG. The flow label becomes a <em>slicing
/// key</em>." Each <c>RegisterFlow(label, factory)</c> contributes
/// its steps to one combined <see cref="BuiltFlow"/>; calling
/// <c>RunAsync</c> with a non-null label slices the merged
/// DAG to the subgraph reachable from that label's declared
/// outputs (via <see cref="FlowSliceStrategy"/>); calling it with
/// a null label runs the entire merged DAG.
/// </para>
/// <para>
/// Per §2.6, the service materialises descriptions from factories
/// rather than holding instances directly — services come in via
/// the <see cref="IServiceProvider"/>; descriptions come out as
/// <see cref="BuiltFlow"/> values.
/// </para>
/// </remarks>
public sealed class FlowthruService : IFlowthruService
{
  private readonly IServiceProvider _services;
  private readonly FlowthruServiceBuilder _registry;
  private readonly Lazy<MergedFlow> _merged;

  // Registration-validation cache. Holds Valid (success) once every
  // hook has reported success — re-running becomes a no-op. Failed
  // hooks re-run on every call so transient failures eventually clear
  // without requiring a process restart.
  private readonly SemaphoreSlim _registrationGate = new(1, 1);
  private Validated<PreFlightError, FlowUnit>? _registrationCache;

  public FlowthruService(IServiceProvider services, FlowthruServiceBuilder registry)
  {
    _services = services ?? throw new ArgumentNullException(nameof(services));
    _registry = registry ?? throw new ArgumentNullException(nameof(registry));
    _merged = new Lazy<MergedFlow>(BuildMergedFlow);
  }

  /// <inheritdoc/>
  public IReadOnlyList<string> RegisteredFlowLabels =>
    _merged.Value.OutputsByLabel.Keys.ToList();

  /// <inheritdoc/>
  public Task<FlowResult> RunAsync(
    string? flowLabel = null,
    ExecutionOptions? options = null,
    CancellationToken cancellationToken = default
  ) => RunAsyncCore(flowLabel: flowLabel, strategy: null, options, cancellationToken);

  /// <summary>
  /// Run the merged DAG sliced by <paramref name="strategy"/>. The
  /// strategy may compose primitives (<see cref="FlowSliceStrategy.From"/>,
  /// <see cref="FlowSliceStrategy.To"/>, <see cref="FlowSliceStrategy.Only"/>,
  /// <see cref="FlowSliceStrategy.Flows"/>) via
  /// <see cref="FlowSliceStrategy.And"/> / <see cref="FlowSliceStrategy.Or"/>
  /// and may use glob wildcards in step / item labels.
  /// </summary>
  public Task<FlowResult> RunAsync(
    FlowSliceStrategy strategy,
    ExecutionOptions? options = null,
    CancellationToken cancellationToken = default
  )
  {
    if (strategy is null) throw new ArgumentNullException(nameof(strategy));
    return RunAsyncCore(flowLabel: null, strategy: strategy, options, cancellationToken);
  }

  private async Task<FlowResult> RunAsyncCore(
    string? flowLabel,
    FlowSliceStrategy? strategy,
    ExecutionOptions? options,
    CancellationToken cancellationToken
  )
  {
    options ??= ExecutionOptions.Default;

    // Registration validation runs once per process before any flow
    // touches data. Successful runs cache the result so subsequent
    // RunAsync calls are no-ops here; failures re-run every call.
    var registrationOutcome = await ValidateRegistrationAsync(cancellationToken).ConfigureAwait(false);
    if (registrationOutcome is Validated<PreFlightError, FlowUnit>.Invalid registrationInvalid)
    {
      // Surface every hook failure as its own synthetic step result so
      // the FlowResult shape stays consistent with per-flow pre-flight
      // failures. Distinct hookId labels per registration check make
      // the source of each failure visible in the report.
      var registrationFailures = registrationInvalid.Errors
        .Select((err, i) =>
        {
          var label = err is PreFlightError.RegistrationCheckFailed rcf
            ? $"preflight:registration:{rcf.HookId}"
            : $"preflight:registration:[{i}]";
          return (StepResult)new StepResult.Failed(
            label,
            new RuntimeError.PreFlightFailed(err),
            TimeSpan.Zero
          );
        })
        .ToList();
      return new FlowResult(registrationFailures);
    }

    var merged = _merged.Value;

    // Three slicing modes:
    //   • flowLabel non-null → legacy "slice to flow's declared outputs" path
    //   • strategy non-null  → apply the closed-sum algebra to the merged DAG
    //   • both null          → run the full merged DAG
    BuiltFlow effectiveFlow;
    if (strategy is not null)
    {
      var slicedSteps = strategy.Apply(merged.Flow.Steps, merged.ProducerByItemLabel);
      effectiveFlow = new BuiltFlow(
        label: merged.Flow.Label,
        orderedSteps: slicedSteps,
        producerByItemLabel: merged.ProducerByItemLabel
      );
    }
    else if (flowLabel is null)
    {
      effectiveFlow = merged.Flow;
    }
    else
    {
      if (!merged.OutputsByLabel.TryGetValue(flowLabel, out var targets))
      {
        throw new InvalidOperationException(
          $"No flow registered with label '{flowLabel}'. Registered flows: "
          + string.Join(", ", merged.OutputsByLabel.Keys)
        );
      }
      effectiveFlow = new BuiltFlow(
        label: flowLabel,
        orderedSteps: FlowSlicing.SliceTo(merged.Flow.Steps, merged.ProducerByItemLabel, targets),
        producerByItemLabel: merged.ProducerByItemLabel
      );
    }

    var isSliced = flowLabel is not null || strategy is not null;
    using var runActivity = FlowthruActivitySource.Source.StartActivity(
      FlowthruActivitySource.RunActivityName,
      ActivityKind.Internal,
      default(ActivityContext),
      new KeyValuePair<string, object?>[]
      {
        new(FlowthruActivitySource.TagFlowLabel, flowLabel ?? "(merged)"),
        new(FlowthruActivitySource.TagFlowStepCount, effectiveFlow.Steps.Count),
        new(FlowthruActivitySource.TagFlowSliced, isSliced),
      }
    );

    // Acquire FlowResources declared on registered catalogs.
    // Per §2.6 / FlowResource.Use's bracket spec, this runs BEFORE
    // pre-flight (so probes can exercise live handles) and releases
    // AFTER post-run metadata, LIFO. The release closure is fed:
    //   • the body's primary RuntimeError (if any) so policies like
    //     PreserveOnFailure see the actual body failure;
    //   • or null on clean body success.
    // Release failures behave per the bracket contract:
    //   • body success + release fail → release errors surface as
    //     additional StepResult.Failed entries in the FlowResult;
    //   • body fail + release fail → release errors are suppressed
    //     (the body's diagnostic wins; release was best-effort cleanup).
    var acquired = new List<(IFlowResource Resource, object? Scope)>();
    foreach (var catalogType in _registry.CatalogTypes)
    {
      if (_services.GetService(catalogType) is CatalogAbstract catalog
          && catalog.Resource is { } resource)
      {
        var acquireResult = await resource.AcquireUntyped().Run(cancellationToken).ConfigureAwait(false);
        if (acquireResult is EffResult<object?>.Failure acquireFailure)
        {
          // Acquire failed — release already-acquired LIFO, feeding
          // the acquire error as bodyError so any release closures
          // see the failure context.
          await ReleaseLifoAsync(acquired, acquireFailure.Error, cancellationToken).ConfigureAwait(false);
          runActivity?.SetStatus(ActivityStatusCode.Error, acquireFailure.Error.Message);
          return new FlowResult(new[]
          {
            (StepResult)new StepResult.Failed("resource.acquire", acquireFailure.Error, TimeSpan.Zero),
          });
        }
        acquired.Add((resource, ((EffResult<object?>.Success)acquireResult).Value));
      }
    }

    // Build the metadata context once — providers see the merged DAG,
    // the slice they're actually running, the active step labels, and
    // the requested flow label. Third-party providers can render the
    // full graph with the active slice highlighted, filter inactive
    // nodes, or annotate cross-flow edges from this single envelope.
    var metadataContext = new FlowMetadataContext
    {
      MergedFlow = merged.Flow,
      EffectiveFlow = effectiveFlow,
      ActiveStepLabels = effectiveFlow.Steps
        .Select(s => s.Label)
        .ToHashSet(StringComparer.Ordinal),
      RequestedFlowLabel = flowLabel,
      BypassCacheReads = options.BypassCacheReads,
    };

    // Run the body, capture the FlowResult, then release LIFO with
    // the body's primary error fed back into release closures.
    FlowResult bodyResult;
    try
    {
      bodyResult = await RunCoreAsync(
        effectiveFlow, metadataContext, options, runActivity, cancellationToken
      ).ConfigureAwait(false);
    }
    catch (Exception ex)
    {
      // Defensive: RunCoreAsync routes failures through the FlowResult
      // sum, so this branch is unreachable in well-formed code. If we
      // get here anyway, treat the throw as the body error so release
      // closures still see the right context, then rethrow.
      var bodyError = new RuntimeError.External("RunCoreAsync", ex);
      await ReleaseLifoAsync(acquired, bodyError, cancellationToken).ConfigureAwait(false);
      throw;
    }

    var releaseErrors = await ReleaseLifoAsync(
      acquired,
      bodyResult.FirstFailure?.Error,
      cancellationToken
    ).ConfigureAwait(false);

    // Bracket discipline: surface release errors only on body success.
    // Body-failure paths suppress them — the user already has the
    // body diagnostic, and the failed cleanup is incidental.
    if (!bodyResult.HasFailures && releaseErrors.Count > 0)
    {
      var augmented = bodyResult.StepResults.ToList();
      for (var i = 0; i < releaseErrors.Count; i++)
      {
        augmented.Add(new StepResult.Failed(
          $"resource.release[{i}]", releaseErrors[i], TimeSpan.Zero));
      }
      return new FlowResult(augmented, bodyResult.Duration);
    }
    return bodyResult;
  }

  /// <inheritdoc/>
  public async Task<Validated<PreFlightError, FlowUnit>> ValidateRegistrationAsync(
    CancellationToken cancellationToken = default
  )
  {
    // Fast-path: a previous successful pass cached the result. Re-running
    // is a no-op; failed hooks bypass the cache and re-run every call.
    if (_registrationCache is Validated<PreFlightError, FlowUnit>.Valid cachedSuccess)
    {
      return cachedSuccess;
    }

    await _registrationGate.WaitAsync(cancellationToken).ConfigureAwait(false);
    try
    {
      // Re-check inside the gate — another caller may have populated the
      // cache while we were waiting.
      if (_registrationCache is Validated<PreFlightError, FlowUnit>.Valid alreadyCached)
      {
        return alreadyCached;
      }

      var hooks = _registry.RegistrationHooks;
      if (hooks.Count == 0)
      {
        var empty = Validated<PreFlightError, FlowUnit>.Pure(FlowUnit.Default);
        _registrationCache = empty;
        return empty;
      }

      // Run every hook independently — one hook's failure does not skip
      // subsequent hooks. Aggregating Validateds via Combine accumulates
      // findings so the user sees the full set in one pass.
      var aggregate = Validated<PreFlightError, FlowUnit>.Pure(FlowUnit.Default);
      foreach (var hook in hooks)
      {
        var hookResult = await hook.Validate(_services).Run(cancellationToken).ConfigureAwait(false);
        var hookOutcome = hookResult switch
        {
          EffResult<Validated<PreFlightError, FlowUnit>>.Success ok => ok.Value,
          EffResult<Validated<PreFlightError, FlowUnit>>.Failure f =>
            // Hook itself threw / failed — surface as a registration
            // check failure attributed to the hook id.
            Validated<PreFlightError, FlowUnit>.Fail(
              new PreFlightError.RegistrationCheckFailed(
                HookId: hook.HookId,
                CheckMessage: $"hook implementation failed: {f.Error.Message}"
              )
            ),
          _ => throw new InvalidOperationException("Unreachable: EffResult is a closed sum"),
        };
        aggregate = Combine(aggregate, hookOutcome);
      }

      // Cache only on success. Failed hooks re-run next call.
      if (aggregate is Validated<PreFlightError, FlowUnit>.Valid)
      {
        _registrationCache = aggregate;
      }
      return aggregate;
    }
    finally
    {
      _registrationGate.Release();
    }
  }

  /// <summary>
  /// Combine two <see cref="Validated{TError, TValue}"/> results,
  /// accumulating errors from both sides. Both invalid → concat
  /// errors; valid + invalid → invalid; valid + valid → valid (the
  /// second value wins by convention since both are FlowUnit.Default).
  /// </summary>
  private static Validated<PreFlightError, FlowUnit> Combine(
    Validated<PreFlightError, FlowUnit> a,
    Validated<PreFlightError, FlowUnit> b
  ) => (a, b) switch
  {
    (Validated<PreFlightError, FlowUnit>.Invalid ai, Validated<PreFlightError, FlowUnit>.Invalid bi) =>
      Validated<PreFlightError, FlowUnit>.Fail((IReadOnlyList<PreFlightError>)ai.Errors.Concat(bi.Errors).ToArray()),
    (Validated<PreFlightError, FlowUnit>.Invalid ai, _) => ai,
    (_, Validated<PreFlightError, FlowUnit>.Invalid bi) => bi,
    _ => b,
  };

  /// <summary>
  /// Release every acquired resource LIFO, feeding
  /// <paramref name="bodyError"/> through to each release closure
  /// (per the FlowResource.Use bracket spec). Releases run
  /// independently — a failing release does not skip subsequent
  /// releases. Returns the collected release errors as values; the
  /// caller decides whether to surface or suppress them.
  /// </summary>
  private static async Task<List<RuntimeError>> ReleaseLifoAsync(
    List<(IFlowResource Resource, object? Scope)> acquired,
    RuntimeError? bodyError,
    CancellationToken cancellationToken
  )
  {
    var errors = new List<RuntimeError>();
    for (var i = acquired.Count - 1; i >= 0; i--)
    {
      var (resource, scope) = acquired[i];
      var releaseResult = await resource.ReleaseUntyped(scope, bodyError).Run(cancellationToken).ConfigureAwait(false);
      if (releaseResult is EffResult<FlowUnit>.Failure failure)
      {
        errors.Add(failure.Error);
      }
    }
    return errors;
  }

  private async Task<FlowResult> RunCoreAsync(
    BuiltFlow effectiveFlow,
    FlowMetadataContext metadataContext,
    ExecutionOptions options,
    Activity? runActivity,
    CancellationToken cancellationToken
  )
  {
    if (options.ValidationDepth != ValidationDepth.None)
    {
      var inspectionLevel = options.ValidationDepth == ValidationDepth.Deep
        ? InspectionLevel.Deep
        : InspectionLevel.Shallow;

      var probes = _registry.Inspectors
        .Select(reg => reg.Probe(_services))
        .ToList();

      // Extension-supplied service-ref dispatchers — resolved from DI as
      // a plural surface so multiple extensions (Python + SQL + ...) can
      // coexist. Layer 4 inside PreFlightPipeline matches each step's
      // ServiceRef.External by Category to find its dispatcher; an
      // unregistered category surfaces as PreFlightError.RegistrationCheckFailed.
      var dispatchers = _services
        .GetServices<Flowthru.Validation.Runtime.IServiceRefDispatcher>()
        .ToList();

      var preFlightResult = await PreFlightPipeline
        .Run(effectiveFlow, _registry.ValidationHooks, probes, dispatchers, inspectionLevel)
        .Run(cancellationToken)
        .ConfigureAwait(false);

      var preFlightOutcome = preFlightResult switch
      {
        EffResult<Validated<PreFlightError, FlowUnit>>.Success ok => ok.Value,
        EffResult<Validated<PreFlightError, FlowUnit>>.Failure f =>
          Validated<PreFlightError, FlowUnit>.Fail(
            new PreFlightError.InspectionFailed("preflight", f.Error.Message)
          ),
        _ => throw new InvalidOperationException("Unreachable: EffResult is a closed sum"),
      };

      if (preFlightOutcome is Validated<PreFlightError, FlowUnit>.Invalid invalid)
      {
        // Surface every pre-flight error as its own synthetic StepResult so
        // the FlowResult preserves per-error granularity (and per-cause
        // FT3xxx codes via PreFlightFailed → classifier delegation). Labels
        // identify the source: input items, registration hooks, services,
        // etc. — matching the cause's natural addressee.
        var preFlightFailures = invalid.Errors
          .Select((err, i) =>
          {
            var label = err switch
            {
              PreFlightError.MissingInput mi => $"preflight:input:{mi.ItemId}",
              PreFlightError.SchemaDrift sd => $"preflight:input:{sd.ItemId}",
              PreFlightError.InspectionFailed iff => $"preflight:input:{iff.ItemId}",
              PreFlightError.DuplicateProducer dp => $"preflight:dag:{dp.ItemId}",
              PreFlightError.CircularDependency => $"preflight:dag:cycle[{i}]",
              PreFlightError.RegistrationCheckFailed rcf => $"preflight:registration:{rcf.HookId}",
              PreFlightError.External ext => $"preflight:external:{ext.Cause.Category}",
              _ => $"preflight:[{i}]",
            };
            return (StepResult)new StepResult.Failed(
              label,
              new RuntimeError.PreFlightFailed(err),
              TimeSpan.Zero
            );
          })
          .ToList();
        return new FlowResult(preFlightFailures);
      }

      // Pre-flight passed — build the cache plan from the
      // framework-managed manifest. Plan is consumed by the scheduler
      // (short-circuits fresh steps) and exposed on the metadata
      // context (rendered by Mermaid/JSON providers). Skipped under
      // DryRun (a dry run shouldn't pretend to know what's cached) or
      // BypassCacheReads (--no-cache: skip the read but still write
      // updates post-run so the next run benefits).
      if (options.DryRun != DryRunOption.On && !options.BypassCacheReads)
      {
        var cacheItem = _services.GetService<IItem<CacheManifest>>();
        if (cacheItem is not null)
        {
          var manifest = await CacheManifestStore
            .LoadAsync(cacheItem, cancellationToken)
            .ConfigureAwait(false);
          var cachePlan = await CachePlanBuilder
            .BuildAsync(effectiveFlow, manifest, cancellationToken)
            .ConfigureAwait(false);
          options = options with { CachePlan = cachePlan };
          metadataContext = metadataContext with { CachePlan = cachePlan };

          // Surface every uncacheable-step decision via the
          // FlowthruActivitySource so the CLI's FlowthruActivityLogger
          // (and any other listener) can render each as an Information-
          // level log line. The MagicAtlas report flagged that a
          // single .Memory() input cascaded through 7+ Python steps
          // with no observable signal — the warm run looked identical
          // to the cold run, which made the cascade undebuggable.
          //
          // Tags must be passed at construction so OnStarted sees them
          // (SetTag after StartActivity fires too late — the listener
          // already snapshotted the activity).
          foreach (var label in cachePlan.UncacheableStepLabels)
          {
            if (!cachePlan.UncacheableReasons.TryGetValue(label, out var reason))
              continue;
            using var uncacheableActivity = FlowthruActivitySource.Source.StartActivity(
              FlowthruActivitySource.CacheUncacheableActivityName,
              System.Diagnostics.ActivityKind.Internal,
              default(System.Diagnostics.ActivityContext),
              new[]
              {
                new System.Collections.Generic.KeyValuePair<string, object?>(
                  FlowthruActivitySource.TagStepLabel, label),
                new System.Collections.Generic.KeyValuePair<string, object?>(
                  FlowthruActivitySource.TagCacheUncacheableReason, reason.Describe()),
              });
          }
        }
      }
    }

    // Pre-run metadata.
    var preRun = await _registry.MetadataBuilder
      .EmitPreRun(metadataContext)
      .Run(cancellationToken)
      .ConfigureAwait(false);
    if (preRun is EffResult<FlowUnit>.Failure preRunFailure)
    {
      return new FlowResult(new[]
      {
        (StepResult)new StepResult.Failed("metadata.preRun", preRunFailure.Error, TimeSpan.Zero),
      });
    }

    // Execute via the DI-resolved scheduler. Core ships
    // ParallelFlowScheduler as the default; extensions can register
    // an alternative IFlowScheduler before AddFlowthru runs.
    var scheduler = _services.GetService<IFlowScheduler>() ?? new ParallelFlowScheduler();
    var flowResult = await scheduler
      .ExecuteAsync(effectiveFlow, options, cancellationToken)
      .ConfigureAwait(false);

    // Cache manifest upsert — for every successfully-run step (real
    // or cached short-circuit), record its composite fingerprint with
    // the current timestamp. Fresh steps re-stamp their existing
    // entries; stale-that-ran steps record newly-computed composites
    // derived from their post-run input fingerprints. Failed and
    // skipped steps contribute nothing.
    //
    // This path is independent of options.CachePlan — under --no-cache
    // the plan is null but the upsert still runs, so a "force re-run"
    // populates the manifest for next time.
    if (options.DryRun != DryRunOption.On)
    {
      var cacheItem = _services.GetService<IItem<CacheManifest>>();
      if (cacheItem is not null)
      {
        var ranSuccessfully = new HashSet<string>(
          flowResult.StepResults
            .OfType<StepResult.Succeeded>()
            .Where(s => s.Reason != "cached")
            .Select(s => s.StepLabel),
          StringComparer.Ordinal
        );

        var postRunFingerprints = await CacheManifestStore
          .ComputePostRunFingerprintsAsync(effectiveFlow, ranSuccessfully, cancellationToken)
          .ConfigureAwait(false);

        // Build the combined Steps + Items maps. Phase 8: items get their
        // own manifest entries alongside steps, so the persisted state
        // mirrors the per-DAG-node fingerprinting design.
        var stepUpserts = new Dictionary<string, string>(StringComparer.Ordinal);
        var itemUpserts = new Dictionary<string, string>(postRunFingerprints.Items, StringComparer.Ordinal);

        // Fresh steps (when a plan exists): refresh RecordedAt with the
        // same composite, plus persist the pre-flight-probed item
        // fingerprints (external inputs + freshly-confirmed outputs of
        // cached steps). Under --no-cache the plan is null and only the
        // post-run-computed entries land.
        if (options.CachePlan is { } finalPlan)
        {
          foreach (var label in finalPlan.FreshStepLabels)
          {
            if (finalPlan.NewStepFingerprints.TryGetValue(label, out var composite))
            {
              stepUpserts[label] = composite;
            }
          }
          foreach (var (label, fp) in finalPlan.NewItemFingerprints)
          {
            // Pre-flight's Items map seeds the upsert; the post-run map
            // overlays any updates from steps that actually ran.
            if (!itemUpserts.ContainsKey(label)) itemUpserts[label] = fp;
          }
        }
        // Stale-that-ran: record newly-derived composites.
        foreach (var (label, composite) in postRunFingerprints.Steps)
        {
          stepUpserts[label] = composite;
        }

        if (stepUpserts.Count > 0 || itemUpserts.Count > 0)
        {
          await CacheManifestStore
            .UpsertEntriesAsync(
              cacheItem,
              stepUpserts,
              itemUpserts,
              DateTimeOffset.UtcNow,
              cancellationToken)
            .ConfigureAwait(false);
        }
      }
    }

    // Post-run metadata — same static context plus the run result.
    var runMetadataContext = new FlowRunMetadataContext
    {
      Static = metadataContext,
      Result = flowResult,
    };
    var postRun = await _registry.MetadataBuilder
      .EmitPostRun(runMetadataContext)
      .Run(cancellationToken)
      .ConfigureAwait(false);
    if (postRun is EffResult<FlowUnit>.Failure postRunFailure)
    {
      var augmented = flowResult.StepResults.ToList();
      augmented.Add(new StepResult.Failed(
        "metadata.postRun", postRunFailure.Error, TimeSpan.Zero));
      var augmentedResult = new FlowResult(augmented, flowResult.Duration);
      runActivity?.SetStatus(ActivityStatusCode.Error, postRunFailure.Error.Message);
      return augmentedResult;
    }

    runActivity?.SetStatus(
      flowResult.HasFailures ? ActivityStatusCode.Error : ActivityStatusCode.Ok,
      flowResult.FirstFailure?.Error.Message
    );
    return flowResult;
  }

  /// <summary>
  /// Materialise every registered flow's <see cref="BuiltFlow"/>,
  /// then merge the union of their steps into a single
  /// <see cref="BuiltFlow"/>. Each registration's output items are
  /// stored under its label so <c>RunAsync</c> can slice
  /// when called with that label.
  /// </summary>
  private MergedFlow BuildMergedFlow()
  {
    var allSteps = new List<IStepNode>();
    var outputsByLabel = new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal);
    var stepLabelsSeen = new HashSet<string>(StringComparer.Ordinal);

    foreach (var registration in _registry.Flows)
    {
      var perFlow = registration.Resolver(_services);
      if (outputsByLabel.ContainsKey(registration.Label))
      {
        throw new InvalidOperationException(
          $"Two flows registered with the same label '{registration.Label}'. "
          + "Flow labels must be unique within a single FlowthruService."
        );
      }

      foreach (var step in perFlow.Steps)
      {
        if (!stepLabelsSeen.Add(step.Label))
        {
          throw new InvalidOperationException(
            $"Step label '{step.Label}' appears in more than one registered flow. "
            + "Step labels must be unique across the merged DAG (§2.4)."
          );
        }
        allSteps.Add(step);
      }

      outputsByLabel[registration.Label] =
        perFlow.Steps.SelectMany(s => s.Outputs.Select(o => o.Label)).Distinct().ToList();
    }

    // Run DependencyAnalyzer over the union — surfaces cycles and
    // duplicate-producer violations across registered flows, not
    // just within one of them.
    var analysis = DependencyAnalyzer.Analyse(allSteps);
    var mergedBuiltFlow = analysis switch
    {
      DependencyAnalyzer.Result.Ok ok =>
        new BuiltFlow("__merged__", ok.Order, ok.ProducerByItemLabel),
      DependencyAnalyzer.Result.CycleDetected c => throw new FlowBuildException(c.Message),
      DependencyAnalyzer.Result.DuplicateProducer d => throw new FlowBuildException(d.Message),
      _ => throw new InvalidOperationException(
        "Unreachable: DependencyAnalyzer.Result is a closed sum"
      ),
    };

    return new MergedFlow(
      mergedBuiltFlow,
      ((DependencyAnalyzer.Result.Ok)analysis).ProducerByItemLabel,
      outputsByLabel
    );
  }

  /// <summary>
  /// Merged-DAG bundle: the combined <see cref="BuiltFlow"/> plus
  /// the producer map and per-label-output index used by slicing.
  /// </summary>
  private sealed record MergedFlow(
    BuiltFlow Flow,
    IReadOnlyDictionary<string, IStepNode> ProducerByItemLabel,
    IReadOnlyDictionary<string, IReadOnlyList<string>> OutputsByLabel
  );
}
