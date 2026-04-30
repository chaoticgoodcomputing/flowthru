using Microsoft.CodeAnalysis;

namespace Flowthru.Core.SourceGenerators.StepAnalysis;

/// <summary>
/// Diagnostic descriptors for step-related analyzers.
/// </summary>
public static class StepDiagnostics
{
  private const string Category = "Flowthru.Core.Steps";

  /// <summary>
  /// FT4001: A step factory class referenced from <c>FlowBuilder.AddStep(transform: …)</c>
  /// is not annotated with <c>[FlowthruStep]</c>. Inline lambdas are exempted.
  /// </summary>
  public static readonly DiagnosticDescriptor MissingFlowthruStepAttribute =
    new(
      id: "FT4001",
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
}
