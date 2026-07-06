using Microsoft.CodeAnalysis;

namespace Flowthru.Core.SourceGenerators.Storage;

/// <summary>
/// Diagnostic descriptors for storage-format capability analyzers. Lives in
/// the <c>FT12xx</c> block of the <c>FT1xxx</c> capability-shape range —
/// structural correctness of a format serializer's declared capabilities —
/// parallel to <see cref="Step.StepDiagnostics"/> (step-extension shape) and
/// <see cref="Schema.SchemaGeneratorDiagnostics"/> (schema shape).
/// </summary>
public static class StorageDiagnostics
{
  private const string Category = "Flowthru.Storage";

  /// <summary>
  /// FT1201: a format serializer declares <c>StorageTraits.CanStream = true</c>
  /// but does not actually stream — either it omits the structural
  /// <c>IFormatStreamReader&lt;TRow&gt;</c> marker, or its
  /// <c>DeserializeRows</c> body materialises the whole input (a whole-document
  /// <c>JsonSerializer.Deserialize</c>/<c>DeserializeAsync</c> call, or a
  /// <c>ToList</c>/<c>ToListAsync</c>/<c>ToArray</c>/<c>ToArrayAsync</c> over the
  /// input stream) before yielding. The <c>CanStream</c> trait promises O(batch)
  /// memory; a materialising body breaks that promise silently.
  /// </summary>
  public static readonly DiagnosticDescriptor StreamingTraitDishonest =
    new(
      id: "FT1201",
      title: "Format declares CanStream but does not stream",
      messageFormat: "Format serializer '{0}': {1}",
      category: Category,
      defaultSeverity: DiagnosticSeverity.Error,
      isEnabledByDefault: true,
      description: "StorageTraits.CanStream = true is a promise that DeserializeRows yields rows "
        + "incrementally in bounded (O(batch)) memory. The runtime trait/marker drift law "
        + "(IFormatSerializerLaws.TraitsAgreeWithMarkerInterfacesLaw) checks the CanStream flag against "
        + "the IFormatStreamReader<TRow> marker at test time; this analyzer adds the compile-time signal "
        + "the drift law cannot give — a DeserializeRows body that materialises the whole input while "
        + "declaring CanStream = true. Either make the read genuinely streaming, or set CanStream = false "
        + "and implement only IFormatRowReader<TRow>."
    );
}
