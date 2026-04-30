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

  /// <summary>
  /// FT4002: A <c>[FlowthruStep]</c> class has a service-typed <c>Create(...)</c> parameter
  /// for which no <c>services.AddFlowthruInspect&lt;T&gt;(...)</c> registration is visible
  /// in the host project. Best-effort static analysis — the runtime preflight backstop
  /// catches missed registrations definitively.
  /// </summary>
  public static readonly DiagnosticDescriptor MissingFlowthruInspector =
    new(
      id: "FT4002",
      title: "Step service has no registered IFlowthruInspector",
      messageFormat: "Step '{0}' takes service parameter '{1}', but no "
        + "services.AddFlowthruInspect<{1}>(...) registration is visible in the project. "
        + "Pre-flight cannot validate this service's reachability — register an inspector "
        + "or suppress this warning if the service is genuinely not preflight-meaningful.",
      category: Category,
      defaultSeverity: DiagnosticSeverity.Warning,
      isEnabledByDefault: true,
      description: "Best-effort scan — registrations factored into helper methods or "
        + "guarded by conditional code may produce false positives. The runtime preflight "
        + "pass authoritatively logs warnings for services lacking inspectors.",
      customTags: WellKnownDiagnosticTags.CompilationEnd
    );

  /// <summary>
  /// FT4003: A <c>[FlowthruStep]</c> class with service-typed <c>Create(...)</c> parameters
  /// does not declare <c>IsIdempotent</c> / <c>HasSideEffects</c> on the attribute.
  /// </summary>
  public static readonly DiagnosticDescriptor MissingStepTraits =
    new(
      id: "FT4003",
      title: "Step with service dependencies lacks declared traits",
      messageFormat: "Step '{0}' takes service dependencies but does not declare "
        + "IsIdempotent / HasSideEffects on its [FlowthruStep] attribute. Declaring traits "
        + "documents the step's behavior for retry-policy selection and DAG metadata.",
      category: Category,
      defaultSeverity: DiagnosticSeverity.Hidden,
      isEnabledByDefault: true,
      description: "Suggestion-only: the IDE shows a lightbulb on the class declaration. "
        + "Steps that take service parameters typically have side effects; declaring the traits "
        + "makes the contract explicit."
    );
}
