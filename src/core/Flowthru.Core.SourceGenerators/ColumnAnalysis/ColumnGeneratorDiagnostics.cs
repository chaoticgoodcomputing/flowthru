using Microsoft.CodeAnalysis;

namespace Flowthru.Core.SourceGenerators.ColumnAnalysis;

/// <summary>
/// Diagnostic descriptors for the <c>[FlowthruColumn]</c> source generator.
/// </summary>
internal static class ColumnGeneratorDiagnostics
{
  private const string Category = "Flowthru.Schema";

  /// <summary>
  /// FT1003: The backing type provided to [FlowthruColumn] is not a recognized scalar type.
  /// Only CLR primitives, enums, byte[], BCL scalar structs, and IScalar implementors are valid.
  /// </summary>
  public static readonly DiagnosticDescriptor InvalidBackingType = new(
    id: "FT1003",
    title: "FlowthruColumn backing type must be a recognized scalar",
    messageFormat: "The backing type '{0}' is not a recognized scalar. Use a CLR primitive, enum, byte[], BCL scalar struct, or IScalar implementor.",
    category: Category,
    defaultSeverity: DiagnosticSeverity.Error,
    isEnabledByDefault: true
  );

  /// <summary>
  /// FT1004: The same NewType name is declared with conflicting backing types.
  /// All uses of <c>[FlowthruColumn]</c> with the same property type must agree on a single backing type.
  /// </summary>
  public static readonly DiagnosticDescriptor InconsistentBackingType = new(
    id: "FT1004",
    title: "FlowthruColumn declarations disagree on backing type",
    messageFormat: "The NewType '{0}' is declared with conflicting backing types: {1}. All uses of [FlowthruColumn] referencing the same NewType name must agree on the backing type.",
    category: Category,
    defaultSeverity: DiagnosticSeverity.Error,
    isEnabledByDefault: true
  );
}
