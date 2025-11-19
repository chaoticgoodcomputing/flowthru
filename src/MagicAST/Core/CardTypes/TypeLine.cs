namespace MagicAST.Core.CardTypes;

/// <summary>
/// Represents a complete type line for a Magic card.
/// Includes supertypes, card types, and subtypes.
/// </summary>
public class TypeLine
{
  /// <summary>
  /// Supertypes that modify the card (e.g., Legendary, Basic).
  /// </summary>
  public List<Supertype> Supertypes { get; init; } = new();

  /// <summary>
  /// Primary card types (e.g., Creature, Artifact).
  /// A card must have at least one card type.
  /// </summary>
  public List<CardType> CardTypes { get; init; } = new();

  /// <summary>
  /// Subtypes specific to the card types (e.g., Human, Equipment).
  /// Subtypes are card-type dependent.
  /// </summary>
  public List<string> Subtypes { get; init; } = new();

  /// <summary>
  /// Parses a type line string (e.g., "Legendary Creature — Dog", "Artifact", "Instant") into a TypeLine object.
  /// </summary>
  /// <param name="typeLineString">The type line string to parse.</param>
  /// <returns>A TypeLine object representing the parsed type line.</returns>
  public static TypeLine Parse(string typeLineString)
  {
    var supertypes = new List<Supertype>();
    var cardTypes = new List<CardType>();
    var subtypes = new List<string>();

    // Split on em dash or regular dash for subtypes
    string[] parts = typeLineString.Split(new[] { " — ", " - " }, StringSplitOptions.None);
    string mainTypeLine = parts[0].Trim();
    string subtypeLine = parts.Length > 1 ? parts[1].Trim() : string.Empty;

    // Parse main type line (supertypes and card types)
    string[] typeWords = mainTypeLine.Split(' ', StringSplitOptions.RemoveEmptyEntries);
    foreach (string word in typeWords)
    {
      // Try to parse as supertype
      if (Enum.TryParse<Supertype>(word, ignoreCase: true, out Supertype supertype))
      {
        supertypes.Add(supertype);
      }
      // Try to parse as card type
      else if (Enum.TryParse<CardType>(word, ignoreCase: true, out CardType cardType))
      {
        cardTypes.Add(cardType);
      }
    }

    // Parse subtypes
    if (!string.IsNullOrEmpty(subtypeLine))
    {
      subtypes.AddRange(subtypeLine.Split(' ', StringSplitOptions.RemoveEmptyEntries));
    }

    return new TypeLine
    {
      Supertypes = supertypes,
      CardTypes = cardTypes,
      Subtypes = subtypes,
    };
  }

  /// <summary>
  /// Returns the string representation of this type line.
  /// </summary>
  public override string ToString()
  {
    var parts = new List<string>();

    // Add supertypes
    parts.AddRange(Supertypes.Select(s => s.ToString()));

    // Add card types
    parts.AddRange(CardTypes.Select(c => c.ToString()));

    string mainPart = string.Join(" ", parts);

    // Add subtypes if present
    if (Subtypes.Count > 0)
    {
      return $"{mainPart} — {string.Join(" ", Subtypes)}";
    }

    return mainPart;
  }
}
