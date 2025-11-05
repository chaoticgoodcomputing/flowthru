using MagicAtlas.Data._02_Processed.Schemas;
using MagicAtlas.Data.Enums.Card;

namespace MagicAtlas.Pipelines.CardProcessing.Nodes;

/// <summary>
/// Flattens refined oracle text into individual entries for embedding model input.
/// Each card produces multiple entries: one for full text and one per ability.
/// </summary>
public static class EmbeddingModelOracleInputNode
{
  /// <summary>
  /// Creates a function that flattens refined oracle text into embedding model inputs.
  /// </summary>
  /// <returns>
  /// A function that takes refined oracle text and produces flattened entries suitable for embedding.
  /// </returns>
  public static Func<
    IEnumerable<RefinedOracleText>,
    Task<IEnumerable<EmbeddingModelOracleInput>>
  > Create()
  {
    return async (refinedTexts) =>
    {
      var flattened = new List<EmbeddingModelOracleInput>();

      foreach (var card in refinedTexts)
      {
        // Add full oracle text entry
        flattened.Add(
          new EmbeddingModelOracleInput
          {
            CardId = card.Id,
            TextType = OracleTextType.Full,
            Text = card.RawText,
          }
        );

        // Add keyword abilities
        foreach (var ability in card.KeywordAbilities)
        {
          flattened.Add(
            new EmbeddingModelOracleInput
            {
              CardId = card.Id,
              TextType = OracleTextType.KeywordAbility,
              Text = ability.RawText,
            }
          );
        }

        // Add triggered abilities
        foreach (var ability in card.TriggeredAbilities)
        {
          flattened.Add(
            new EmbeddingModelOracleInput
            {
              CardId = card.Id,
              TextType = OracleTextType.TriggeredAbility,
              Text = ability.RawText,
            }
          );
        }

        // Add activated abilities
        foreach (var ability in card.ActivatedAbilities)
        {
          flattened.Add(
            new EmbeddingModelOracleInput
            {
              CardId = card.Id,
              TextType = OracleTextType.ActivatedAbility,
              Text = ability.RawText,
            }
          );
        }

        // Add passive abilities
        foreach (var ability in card.PassiveAbilities)
        {
          flattened.Add(
            new EmbeddingModelOracleInput
            {
              CardId = card.Id,
              TextType = OracleTextType.PassiveAbility,
              Text = ability.Effect,
            }
          );
        }
      }

      return await Task.FromResult(flattened);
    };
  }
}
