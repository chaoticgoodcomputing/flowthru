using Flowthru.Abstractions;
using MagicAtlas.Data.Enums.Card;

namespace MagicAtlas.Data._05_ModelInput.Schemas;

/// <summary>
/// Flattened oracle text entry suitable for embedding model input.
/// Each card with oracle text produces multiple entries (one full text + one per ability).
/// </summary>
public record EmbeddingModelOracleInput : IFlatSchema, IBinarySerializable, ITextSerializable
{
  /// <summary>
  /// Scryfall card ID.
  /// </summary>
  public Guid CardId { get; init; }

  /// <summary>
  /// Type of oracle text entry.
  /// </summary>
  public OracleTextType TextType { get; init; }

  /// <summary>
  /// Raw text content for this entry.
  /// </summary>
  public string Text { get; init; } = "";
}
