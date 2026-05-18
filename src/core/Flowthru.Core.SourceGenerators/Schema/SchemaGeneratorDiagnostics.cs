using Microsoft.CodeAnalysis;

namespace Flowthru.Core.SourceGenerators.Schema;

/// <summary>
/// Diagnostic descriptors for the schema interface source generator.
/// Lives in the FT1xxx range (algebra shape — interpreter conformance and
/// generator-shape rules).
/// </summary>
public static class SchemaGeneratorDiagnostics
{
  private const string Category = "Flowthru.Schema";

  /// <summary>FT1001: <c>[FlowthruSchema]</c> requires a partial type declaration.</summary>
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
  /// FT1002: schema marked <c>[FlowthruSchema]</c> manually applies marker interfaces that
  /// would conflict with the generated classification.
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

  /// <summary>
  /// FT2010: a property on a <c>[FlowthruSchema]</c>-decorated record uses
  /// a set-shaped collection type (<c>HashSet&lt;T&gt;</c>,
  /// <c>SortedSet&lt;T&gt;</c>, <c>ISet&lt;T&gt;</c>,
  /// <c>IReadOnlySet&lt;T&gt;</c>, and the immutable variants). These
  /// types can lose the converter-dispatch race in System.Text.Json,
  /// serialising as the set wrapper's public surface
  /// (<c>{Count, Capacity, Comparer}</c>) instead of as a JSON array —
  /// the failure mode MagicAtlas hit when a <c>.Memory()</c> catalog
  /// item was swapped to <c>.Json()</c>. Arrays, <c>List&lt;T&gt;</c>,
  /// dictionaries, and the read-only family interfaces are not affected
  /// and remain allowed.
  /// </summary>
  /// <remarks>
  /// <para>
  /// Scope is deliberately narrow — this catches the exact STJ
  /// round-trip hazard documented in
  /// <c>docs/scratch/reports/magic-atlas-cache-issues.md</c> and
  /// nothing more. The broader schema-boundary contract (schemas
  /// declare shape, not storage; collection slots use interface types
  /// so the producer picks the concrete container) is Wave 3+ work
  /// tracked under the container-dispatch matrix in
  /// <c>docs/scratch/flowthru-trax-roadmap.md</c>; analyzers for that
  /// contract will ship alongside the matrix.
  /// </para>
  /// </remarks>
  public static readonly DiagnosticDescriptor SchemaPropertyUsesUnsafeSetType =
    new(
      id: "FT2010",
      title: "Schema property uses a set type that may not round-trip through System.Text.Json",
      messageFormat: "Schema '{0}' property '{1}' is declared as '{2}'. Set types can lose "
        + "System.Text.Json's converter-dispatch race and serialize as "
        + "{{Count, Capacity, Comparer}} instead of a JSON array. Use IReadOnlyList<T> "
        + "(or another non-set collection shape) so the producer step picks the concrete "
        + "container.",
      category: Category,
      defaultSeverity: DiagnosticSeverity.Warning,
      isEnabledByDefault: true,
      description: "HashSet<T>, SortedSet<T>, ISet<T>, IReadOnlySet<T>, and their immutable "
        + "variants can fall through System.Text.Json's collection converter to the object "
        + "converter, serializing the set wrapper's public properties instead of its "
        + "elements. The deserialize side then fails with \"Property set method not "
        + "found.\" Arrays, List<T>, and the read-only family interfaces use STJ's "
        + "dedicated converters and don't share the hazard. If you need set semantics "
        + "inside a step, materialize a temporary HashSet<T> locally and store back as "
        + "IReadOnlyList<T>. See docs/scratch/reports/magic-atlas-cache-issues.md for the "
        + "originating incident."
    );
}
