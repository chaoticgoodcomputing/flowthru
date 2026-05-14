using Microsoft.CodeAnalysis;

namespace Flowthru.Core.SourceGenerators.Step;

/// <summary>
/// Diagnostic descriptors for step-shape analyzers. Lives in the
/// <c>FT1xxx</c> range (algebra shape — interpreter conformance), parallel
/// to <see cref="Schema.SchemaGeneratorDiagnostics"/>.
/// </summary>
public static class StepDiagnostics
{
  private const string Category = "Flowthru.Step";

  /// <summary>
  /// FT1101: a step factory class referenced from <c>FlowBuilder.AddStep(transform: …)</c>
  /// is not annotated with <c>[FlowthruStep]</c>. Inline lambdas are exempted.
  /// </summary>
  public static readonly DiagnosticDescriptor MissingFlowthruStepAttribute =
    new(
      id: "FT1101",
      title: "Step factory class missing [FlowthruStep] attribute",
      messageFormat: "Step factory '{0}' is referenced from FlowBuilder.AddStep but is not annotated with "
        + "[FlowthruStep]. Add the attribute so source generators and downstream tooling (FUnit test "
        + "discovery, metadata emission) can recognize it as a step.",
      category: Category,
      defaultSeverity: DiagnosticSeverity.Warning,
      isEnabledByDefault: true,
      description: "Every named-class step factory passed to AddStep should carry [FlowthruStep]. "
        + "Inline lambdas (e.g., transform: x => x) are exempted — extract to a step class only when "
        + "the transform is non-trivial. The attribute makes the step discoverable by FUnit's test "
        + "scaffolding and by future metadata generators."
    );

  /// <summary>
  /// FT1102: a step's <c>outputs:</c> argument is (or contains) a value
  /// whose type implements <c>IReadOnlyItem&lt;T&gt;</c>. Read-only
  /// items — the canonical example being
  /// <c>Flowthru.Data.Catalog.Configuration.ConfigurationItem&lt;T&gt;</c> —
  /// always fail on <c>Save</c>, so wiring them as a step output is a
  /// runtime failure the type system can catch at build time.
  /// </summary>
  public static readonly DiagnosticDescriptor ReadOnlyItemInOutputPosition =
    new(
      id: "FT1102",
      title: "Read-only catalog item used as step output",
      messageFormat: "Item '{0}' implements IReadOnlyItem<T> and cannot be a step output. "
        + "Read-only items (e.g. ConfigurationItem<T>) are inputs only — Save always fails. "
        + "Replace the outputs argument with a writable item, or remove the step's write side-effect.",
      category: Category,
      defaultSeverity: DiagnosticSeverity.Error,
      isEnabledByDefault: true,
      description: "Read-only catalog items declare their read-only contract via the IReadOnlyItem<T> "
        + "marker. Passing one to FlowBuilder.AddStep's outputs parameter would only fail at runtime "
        + "during Save — the analyzer pushes that error to build-time so the user sees it as a "
        + "compile error, not a wasted pipeline run. To opt out of this check, narrow the item's "
        + "static type to IItem<T> before passing it."
    );
}
