using Microsoft.CodeAnalysis;

namespace Flowthru.Core.SourceGenerators.SchemaAnalysis;

/// <summary>
/// Diagnostic descriptors for the schema interface source generator.
/// </summary>
public static class SchemaGeneratorDiagnostics
{
    private const string Category = "Flowthru.Core.Schema";

    /// <summary>
    /// FT1001: [FlowthruSchema] requires a partial type declaration.
    /// </summary>
    public static readonly DiagnosticDescriptor TypeMustBePartial =
      new(
        id: "FT1001",
        title: "FlowthruSchema type must be partial",
        messageFormat: "Type '{0}' is marked with [FlowthruSchema] but is not declared as partial. "
          + "The source generator cannot emit interface implementations for non-partial types.",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Types annotated with [FlowthruSchema] must be declared as partial so the "
          + "source generator can emit the appropriate marker interface implementations."
      );

    /// <summary>
    /// FT1002: Schema marked [FlowthruSchema] also has manually-applied marker interfaces that
    /// conflict with the generated classification.
    /// </summary>
    public static readonly DiagnosticDescriptor ConflictingManualInterface =
      new(
        id: "FT1002",
        title: "Conflicting manual schema interface",
        messageFormat: "Type '{0}' is marked with [FlowthruSchema] but also manually implements {1}. "
          + "The generator determined this type is {2}. Remove the manual interface — "
          + "the generator will apply the correct ones.",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "When using [FlowthruSchema], remove manually-applied IFlatSchema, INestedSchema, "
          + "ITextSerializable, IBinarySerializable, and IStructuredSerializable interfaces. "
          + "The generator derives these from the schema's property types."
      );
}
