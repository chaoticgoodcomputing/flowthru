using Flowthru.Data;
using MagicAtlas.Data._03_Intermediate.Schemas;

namespace MagicAtlas.Data;

/// <summary>
/// Intermediate data catalog entries (Layer 3).
/// These entries exist only during pipeline execution and are not persisted to disk.
/// </summary>
public partial class Catalog
{
  /// <summary>
  /// Tokenized oracle text entries ready for ONNX model inference.
  /// </summary>
  /// <remarks>
  /// <para>
  /// <strong>Transformation:</strong> EmbeddingModelOracleInput → TokenizedBertInput
  /// </para>
  /// <para>
  /// Contains BERT token tensors (input_ids, attention_mask, token_type_ids) for each
  /// oracle text entry that passed tokenization validation.
  /// </para>
  /// <para>
  /// <strong>Storage:</strong> In-memory only (not persisted)
  /// </para>
  /// <para>
  /// <strong>Lifecycle:</strong> Created during tokenization, consumed by ONNX inference,
  /// then garbage collected.
  /// </para>
  /// <para>
  /// <strong>Layer:</strong> 3 (Intermediate - transient data)
  /// </para>
  /// </remarks>
  public ICatalogEntry<IEnumerable<TokenizedBertInput>> TokenizedOracleText =>
    GetOrCreateEntry(
      () => CatalogEntries.Enumerable.Memory<TokenizedBertInput>(label: "TokenizedOracleText")
    );
}
