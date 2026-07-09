using System.Reflection;
using Flowthru.Data.Schema;
using Flowthru.Flow;
using Flowthru.Prelude;
using Flowthru.Step;
using Flowthru.Step.Python;
using Flowthru.Validation.PreFlight;
using Flowthru.Validation.Runtime;
using Flowthru.Validation.Runtime.Python;

namespace Flowthru.Validation.PreFlight.Python;

/// <summary>
/// Pre-flight validation hook that walks every Python step in the
/// built flow and verifies, for each: the module imports, the
/// function exists, the <c>@step</c> decorator is present, the
/// decorator-declared schemas agree with the C# generic type
/// parameters at every position, and the function arity matches the
/// declared input count. All failures accumulate into a single
/// <see cref="Validated{TError, TValue}"/> result.
/// </summary>
/// <remarks>
/// <para>
/// Mirrors the legacy <c>PythonStepValidator</c> but replaces its
/// reflection-on-private-fields probe of the legacy
/// <c>PythonStepWrapper</c> with a clean pattern-match on the open
/// generic <see cref="PythonStep{TIn, TOut}"/>. Each Python step
/// publicly exposes <see cref="PythonStep{TIn, TOut}.ModuleName"/> and
/// <see cref="PythonStep{TIn, TOut}.FunctionName"/>, eliminating the
/// fragility of the legacy probe.
/// </para>
/// <para>
/// Per CONTRIBUTING.md's "Decision rule 2", every environmental
/// concern about the Python side lives here — module-import,
/// decorator-presence, schema agreement, arity. <c>AddPythonStep</c>
/// at flow-construction time does no IO; failures aggregate here so
/// the user sees every Python problem at once.
/// </para>
/// </remarks>
public sealed class PythonStepValidationHook : IFlowValidationHook
{
  private readonly IPythonExecutor _executor;

  /// <summary>Construct the hook with the executor it uses for schema introspection.</summary>
  public PythonStepValidationHook(IPythonExecutor executor)
  {
    _executor = executor ?? throw new ArgumentNullException(nameof(executor));
  }

  /// <inheritdoc/>
  public string HookId => "python.step-shape";

  /// <inheritdoc/>
  public FlowIO<Validated<PreFlightError, FlowUnit>> Validate(BuiltFlow flow)
  {
    if (flow is null) throw new ArgumentNullException(nameof(flow));

    return FlowIO.LiftAsync<Validated<PreFlightError, FlowUnit>>(
      async ct =>
      {
        var perStep = new List<Validated<PreFlightError, FlowUnit>>();
        foreach (var step in flow.Steps)
        {
          if (!TryGetPythonStepDescriptor(step, out var descriptor)) continue;
          perStep.Add(await ValidateStepAsync(descriptor, ct).ConfigureAwait(false));
        }

        // Accumulate every step's outcome into a single result. Empty
        // input list is the identity (Pure(FlowUnit)). ZipAll
        // accumulates failures across siblings without short-circuiting.
        return Validated.ZipAll<PreFlightError, FlowUnit>(perStep)
          .Map(_ => FlowUnit.Default);
      },
      source: HookId
    );
  }

  // ── Per-step validation ──────────────────────────────────────────────

  private async Task<Validated<PreFlightError, FlowUnit>> ValidateStepAsync(
    PythonStepDescriptor descriptor,
    CancellationToken cancellationToken
  )
  {
    var metadataResult = await _executor
      .ValidateStep(descriptor.ModuleName, descriptor.FunctionName)
      .Run(cancellationToken)
      .ConfigureAwait(false);

    return metadataResult switch
    {
      EffResult<PythonStepMetadata>.Failure f => TranslateValidateFailure(descriptor, f.Error),
      EffResult<PythonStepMetadata>.Success ok => CheckSchemaAgreement(descriptor, ok.Value),
      _ => Validated<PreFlightError, FlowUnit>.Pure(FlowUnit.Default),
    };
  }

  /// <summary>
  /// Map executor-surfaced <see cref="RuntimeError"/>s into typed
  /// <see cref="PreFlightError.External"/> wrappers — module-not-found,
  /// function-missing, decorator-absent all become structured
  /// pre-flight failures rather than runtime exceptions.
  /// </summary>
  private static Validated<PreFlightError, FlowUnit> TranslateValidateFailure(
    PythonStepDescriptor descriptor,
    RuntimeError error
  )
  {
    var detail = error switch
    {
      RuntimeError.ExtensionError ext => ext.Cause.Message,
      _ => error.Message,
    };
    return Validated<PreFlightError, FlowUnit>.Fail(
      new PreFlightError.External(new PythonPreFlightError.ServiceInspectionFailed(
        ServiceClassPath: $"{descriptor.ModuleName}.{descriptor.FunctionName}",
        Detail: detail
      ))
    );
  }

  /// <summary>
  /// Compare decorator-declared schema names against the C# generic
  /// type names. Every mismatch — count or per-position — becomes a
  /// structured <see cref="PythonPreFlightError"/>; results accumulate
  /// rather than short-circuit so the user sees every problem at once.
  /// </summary>
  private static Validated<PreFlightError, FlowUnit> CheckSchemaAgreement(
    PythonStepDescriptor descriptor,
    PythonStepMetadata metadata
  )
  {
    var failures = new List<PreFlightError>();
    var label = descriptor.StepLabel;

    var csInputNames = ExtractSchemaNames(descriptor.InputType);
    var csOutputNames = ExtractSchemaNames(descriptor.OutputType);

    AppendCountMismatch(failures, label, PythonSchemaSide.Input, csInputNames.Count, metadata.Inputs.Count);
    AppendCountMismatch(failures, label, PythonSchemaSide.Output, csOutputNames.Count, metadata.Outputs.Count);

    AppendPositionalMismatches(failures, label, PythonSchemaSide.Input, csInputNames, metadata.Inputs);
    AppendPositionalMismatches(failures, label, PythonSchemaSide.Output, csOutputNames, metadata.Outputs);

    // Function arity vs declared input count — the decorator's input
    // arity must match the function's parameter count exactly. We
    // compare against the C#-declared input count because the
    // decorator already had to agree with that for the count check
    // above to pass; if it didn't, we'd already have flagged it.
    if (metadata.Inputs.Count != csInputNames.Count)
    {
      failures.Add(new PreFlightError.External(new PythonPreFlightError.ArityMismatch(
        StepLabel: label,
        Module: descriptor.ModuleName,
        Function: descriptor.FunctionName,
        Expected: csInputNames.Count,
        Actual: metadata.Inputs.Count
      )));
    }

    return failures.Count == 0
      ? Validated<PreFlightError, FlowUnit>.Pure(FlowUnit.Default)
      : Validated<PreFlightError, FlowUnit>.Fail(failures);
  }

  private static void AppendCountMismatch(
    List<PreFlightError> failures,
    string stepLabel,
    PythonSchemaSide side,
    int expected,
    int actual
  )
  {
    if (expected == actual) return;
    failures.Add(new PreFlightError.External(new PythonPreFlightError.SchemaCountMismatch(
      StepLabel: stepLabel,
      Side: side,
      Expected: expected,
      Actual: actual
    )));
  }

  private static void AppendPositionalMismatches(
    List<PreFlightError> failures,
    string stepLabel,
    PythonSchemaSide side,
    IReadOnlyList<string?> csNames,
    IReadOnlyList<string> declaredNames
  )
  {
    var common = Math.Min(csNames.Count, declaredNames.Count);
    for (var i = 0; i < common; i++)
    {
      // A null entry is a non-schema payload position (byte[] artifact,
      // directory, scalar): arity is still checked, names are not.
      if (csNames[i] is null || csNames[i] == declaredNames[i]) continue;
      failures.Add(new PreFlightError.External(new PythonPreFlightError.SchemaNameMismatch(
        StepLabel: stepLabel,
        Side: side,
        Position: i,
        ExpectedName: csNames[i]!,
        ActualName: declaredNames[i]
      )));
    }
  }

  // ── Step-discovery & generic-args extraction ─────────────────────────

  /// <summary>
  /// Pattern-match (via reflection on the open generic
  /// <see cref="PythonStep{TIn, TOut}"/>) to extract the descriptor
  /// fields needed for validation. Returns <c>false</c> when
  /// <paramref name="step"/> is not a Python step.
  /// </summary>
  private static bool TryGetPythonStepDescriptor(IStepNode step, out PythonStepDescriptor descriptor)
  {
    descriptor = default!;
    var stepType = step.GetType();
    if (!stepType.IsGenericType) return false;
    if (stepType.GetGenericTypeDefinition() != typeof(PythonStep<,>)) return false;

    var args = stepType.GetGenericArguments();
    var moduleNameProp = stepType.GetProperty(nameof(PythonStep<int, int>.ModuleName))!;
    var functionNameProp = stepType.GetProperty(nameof(PythonStep<int, int>.FunctionName))!;

    descriptor = new PythonStepDescriptor(
      StepLabel: step.Label,
      ModuleName: (string)moduleNameProp.GetValue(step)!,
      FunctionName: (string)functionNameProp.GetValue(step)!,
      InputType: args[0],
      OutputType: args[1]
    );
    return true;
  }

  /// <summary>
  /// Extract the list of schema names from a C# step's generic type
  /// parameter. Handles ValueTuple (multi-I/O), <c>IEnumerable&lt;T&gt;</c>
  /// (tabular), and bare scalar/byte[] types. Each entry's name is
  /// the underlying schema type's <c>Type.Name</c> — matched
  /// for string equality against decorator-declared names. A position
  /// whose underlying type is not a <c>[FlowthruSchema]</c> type (a
  /// <c>byte[]</c> artifact, a directory payload, a plain scalar) has
  /// no C# schema name to agree with — the decorator's name there is
  /// descriptive, not a schema binding — so the entry is <c>null</c>
  /// and the position is exempt from name comparison.
  /// </summary>
  private static IReadOnlyList<string?> ExtractSchemaNames(Type type)
  {
    if (IsValueTuple(type))
    {
      return type.GetGenericArguments().Select(ExtractSingleSchemaName).ToList();
    }
    return new[] { ExtractSingleSchemaName(type) };
  }

  private static string? ExtractSingleSchemaName(Type type)
  {
    var underlying = type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IEnumerable<>)
      ? type.GetGenericArguments()[0]
      : type;
    return underlying.IsDefined(typeof(FlowthruSchemaAttribute), inherit: false)
      ? underlying.Name
      : null;
  }

  private static bool IsValueTuple(Type type)
  {
    if (!type.IsValueType || !type.IsGenericType) return false;
    return type.GetGenericTypeDefinition().FullName?.StartsWith(
      "System.ValueTuple`",
      StringComparison.Ordinal
    ) ?? false;
  }

  /// <summary>
  /// Carrier for the per-step state the validator needs. Built via
  /// reflection at the entry point and passed by value to every
  /// per-step check.
  /// </summary>
  private sealed record PythonStepDescriptor(
    string StepLabel,
    string ModuleName,
    string FunctionName,
    Type InputType,
    Type OutputType
  );
}
