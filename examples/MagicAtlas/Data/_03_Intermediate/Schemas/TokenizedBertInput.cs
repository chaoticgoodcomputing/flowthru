using Flowthru.Abstractions;
using MagicAtlas.Data.Enums.Card;

namespace MagicAtlas.Data._03_Intermediate.Schemas;

/// <summary>
/// Tokenized BERT input tensors for oracle text entries.
/// Intermediate representation between raw text and ONNX model inference.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Transformation:</strong> EmbeddingModelOracleInput → TokenizedBertInput
/// </para>
/// <para>
/// Each oracle text string is tokenized into three tensor arrays required by BERT models:
/// </para>
/// <list type="bullet">
/// <item><strong>input_ids:</strong> Token vocabulary indices</item>
/// <item><strong>attention_mask:</strong> Valid token positions (1) vs padding (0)</item>
/// <item><strong>token_type_ids:</strong> Segment IDs (typically all 0s for single sentences)</item>
/// </list>
/// <para>
/// <strong>Tokenizer:</strong> BertUncasedBaseTokenizer (matches all-MiniLM-L6-v2 base)
/// </para>
/// <para>
/// <strong>Max Length:</strong> 512 tokens (entries exceeding this are dropped)
/// </para>
/// <para>
/// <strong>Storage:</strong> In-memory only (not persisted to disk)
/// </para>
/// </remarks>
public record TokenizedBertInput : IFlatSchema, IBinarySerializable
{
  /// <summary>
  /// Scryfall card ID.
  /// </summary>
  [SerializedLabel("card_id")]
  public Guid CardId { get; init; }

  /// <summary>
  /// Type of oracle text entry (full text, keyword ability, etc.).
  /// </summary>
  [SerializedLabel("text_type")]
  public OracleTextType TextType { get; init; }

  /// <summary>
  /// Token vocabulary indices (input_ids tensor).
  /// </summary>
  /// <remarks>
  /// Shape: [sequence_length]
  /// Vocabulary size: 30,522 (BERT-base-uncased)
  /// </remarks>
  [SerializedLabel("input_ids")]
  public long[] InputIds { get; init; } = Array.Empty<long>();

  /// <summary>
  /// Attention mask indicating valid tokens (1) vs padding (0).
  /// </summary>
  /// <remarks>
  /// Shape: [sequence_length]
  /// Values: 1 for real tokens, 0 for padding
  /// </remarks>
  [SerializedLabel("attention_mask")]
  public long[] AttentionMask { get; init; } = Array.Empty<long>();

  /// <summary>
  /// Token type IDs for segment distinction.
  /// </summary>
  /// <remarks>
  /// Shape: [sequence_length]
  /// Values: Typically all 0s for single-sentence inputs
  /// </remarks>
  [SerializedLabel("token_type_ids")]
  public long[] TokenTypeIds { get; init; } = Array.Empty<long>();
}
