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
        + "discovery, metadata emission, cache identity registration) can recognize it as a step.",
      category: Category,
      defaultSeverity: DiagnosticSeverity.Error,
      isEnabledByDefault: true,
      description: "Every named-class step factory passed to AddStep must carry [FlowthruStep]. "
        + "Inline lambdas (e.g., transform: x => x) are exempted — extract to a step class only when "
        + "the transform is non-trivial. Phase 8 of the smart-caching RFC made this build-breaking: "
        + "the attribute is the trigger for the source-generator-emitted CodeVersion identity that "
        + "the cache plan consumes, so without it the step is permanently uncacheable and the "
        + "framework has no way to detect code changes."
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

  /// <summary>
  /// FT1301: a step extension declares <c>[StepExtensionCapabilities]</c>
  /// but its <c>Inputs</c> or <c>Outputs</c> bitmask omits the
  /// minimum floor of <c>Singleton | Enumerable</c>. The default
  /// severity is <see cref="DiagnosticSeverity.Error"/>; the analyzer
  /// downgrades to <see cref="DiagnosticSeverity.Warning"/> at report
  /// time when the attribute's <c>Status</c> field is
  /// <c>ExtensionStatus.InDevelopment</c>.
  /// </summary>
  public static readonly DiagnosticDescriptor ExtensionMissesMinimumContainerSupport =
    new(
      id: "FT1301",
      title: "Step extension misses minimum container support",
      messageFormat: "Step extension '{0}' declares {1} support of {2} but the production minimum "
        + "is Singleton | Enumerable. Add the missing kinds (and the corresponding marshaller marker "
        + "interfaces) or set Status = ExtensionStatus.InDevelopment to downgrade this diagnostic to "
        + "a warning while iterating.",
      category: Category,
      defaultSeverity: DiagnosticSeverity.Error,
      isEnabledByDefault: true,
      description: "Per Phase 9 of the smart-caching/extensibility RFCs, step extensions that ship "
        + "to NuGet must cover Singleton and Enumerable container shapes at minimum so that catalog "
        + "items of either shape (including ConfigurationItem<T> scalars) can flow into the extension. "
        + "Authors iterating on a new extension can set Status = InDevelopment to downgrade the "
        + "diagnostic to a warning until the extension's algebra is complete."
    );

  /// <summary>
  /// FT1303: the container kinds declared on
  /// <c>[StepExtensionCapabilities]</c> don't line up with the
  /// marshaller marker interfaces the extension class implements.
  /// Capability disclosure and implementation evidence are
  /// co-authoritative — declaring <c>Queryable</c> without
  /// <c>IQueryableMarshaller</c>, or vice versa, is a contract drift
  /// the analyzer catches at build time.
  /// </summary>
  public static readonly DiagnosticDescriptor ExtensionCapabilityImplementationMismatch =
    new(
      id: "FT1303",
      title: "Step extension capability/marshaller mismatch",
      messageFormat: "Step extension '{0}': {1}",
      category: Category,
      defaultSeverity: DiagnosticSeverity.Error,
      isEnabledByDefault: true,
      description: "The [StepExtensionCapabilities] attribute and the IContainerMarshaller / "
        + "IQueryableMarshaller / IAsyncStreamMarshaller marker interfaces are two halves of the same "
        + "contract — capability disclosure and implementation evidence. They must agree. "
        + "An attribute declaring a kind without the matching marker interface (or a marker interface "
        + "implemented without the matching kind declared) is silent drift that breaks at runtime."
    );
}
