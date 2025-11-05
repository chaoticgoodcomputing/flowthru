using System.Text.RegularExpressions;
using MagicAtlas.Data._02_Processed.Schemas;

namespace MagicAtlas.Pipelines.CardProcessing.Nodes;

/// <summary>
/// Processes card oracle text by refining symbols and categorizing abilities.
/// </summary>
public static class RefineOracleTextNode
{
  private static readonly Regex _cardSymbolPattern = new(@"\{[^}]+\}", RegexOptions.Compiled);

  /// <summary>
  /// Creates an oracle text refinement function that processes card text and abilities.
  /// </summary>
  /// <returns>
  /// A function that takes card core data and symbol dictionary, and produces refined oracle text
  /// with expanded symbols and categorized abilities.
  /// </returns>
  public static Func<
    (IEnumerable<CardCoreData>, CardSymbolDictionary),
    Task<IEnumerable<RefinedOracleText>>
  > Create()
  {
    return async (input) =>
    {
      var (cards, symbolDict) = input;

      var refined = cards
        .Where(card => !string.IsNullOrWhiteSpace(card.OracleText))
        .Select(card => RefineCard(card, symbolDict.Symbols))
        .ToList();

      return await Task.FromResult(refined);
    };
  }

  /// <summary>
  /// Refines a single card's oracle text.
  /// </summary>
  private static RefinedOracleText RefineCard(
    CardCoreData card,
    Dictionary<string, CardSymbol> symbols
  )
  {
    var rawText = card.OracleText ?? "";
    var refinedText = RefineSymbols(rawText, symbols);
    var lines = rawText.Split('\n', StringSplitOptions.RemoveEmptyEntries);

    var keywordAbilities = new List<KeywordAbility>();
    var triggeredAbilities = new List<TriggeredAbility>();
    var activatedAbilities = new List<ActivatedAbility>();
    var passiveAbilities = new List<PassiveAbility>();

    foreach (var line in lines)
    {
      var trimmedLine = line.Trim();
      if (string.IsNullOrWhiteSpace(trimmedLine))
      {
        continue;
      }

      // Check for keyword ability (contains em dash —)
      if (IsKeywordAbility(trimmedLine))
      {
        keywordAbilities.Add(ParseKeywordAbility(trimmedLine));
      }
      // Check for triggered ability (starts with "When", "Whenever", or "At")
      else if (IsTriggeredAbility(trimmedLine))
      {
        triggeredAbilities.Add(ParseTriggeredAbility(trimmedLine));
      }
      // Check for activated ability (contains colon before any opening parenthesis)
      else if (IsActivatedAbility(trimmedLine))
      {
        activatedAbilities.Add(ParseActivatedAbility(trimmedLine));
      }
      // Otherwise, it's a passive ability
      else
      {
        passiveAbilities.Add(new PassiveAbility { Effect = trimmedLine });
      }
    }

    return new RefinedOracleText
    {
      Id = card.Id,
      Name = card.Name,
      RawText = rawText,
      RefinedText = refinedText,
      KeywordAbilities = keywordAbilities,
      TriggeredAbilities = triggeredAbilities,
      ActivatedAbilities = activatedAbilities,
      PassiveAbilities = passiveAbilities,
    };
  }

  /// <summary>
  /// Checks if a line represents a keyword ability.
  /// A keyword ability contains " - " (space-hyphen-space) after normalization.
  /// </summary>
  private static bool IsKeywordAbility(string line)
  {
    var dashIndex = line.IndexOf(" - ");
    if (dashIndex == -1)
    {
      return false;
    }

    // Ensure there's content before and after the dash
    return dashIndex > 0 && dashIndex + 3 < line.Length;
  }

  /// <summary>
  /// Replaces symbol placeholders (e.g., {T}, {2}) with their English descriptions.
  /// </summary>
  private static string RefineSymbols(string text, Dictionary<string, CardSymbol> symbols)
  {
    return _cardSymbolPattern.Replace(
      text,
      match =>
      {
        var symbol = match.Value;
        if (symbols.TryGetValue(symbol, out var symbolData))
        {
          return symbolData.English;
        }
        return symbol; // Keep original if not found
      }
    );
  }

  /// <summary>
  /// Checks if a line represents a triggered ability.
  /// A triggered ability starts with "When", "Whenever", or "At".
  /// </summary>
  private static bool IsTriggeredAbility(string line)
  {
    return line.StartsWith("When ", StringComparison.Ordinal)
      || line.StartsWith("Whenever ", StringComparison.Ordinal)
      || line.StartsWith("At ", StringComparison.Ordinal);
  }

  /// <summary>
  /// Checks if a line represents an activated ability.
  /// An activated ability has a colon that appears before any opening parenthesis.
  /// </summary>
  private static bool IsActivatedAbility(string line)
  {
    var colonIndex = line.IndexOf(':');
    if (colonIndex == -1)
    {
      return false;
    }

    var parenIndex = line.IndexOf('(');
    // If there's no paren, or the colon comes before the paren, it's an activated ability
    return parenIndex == -1 || colonIndex < parenIndex;
  }

  /// <summary>
  /// Parses a keyword ability line (format: "Keyword - Effect").
  /// </summary>
  private static KeywordAbility ParseKeywordAbility(string line)
  {
    var parts = line.Split(" - ", 2, StringSplitOptions.TrimEntries);
    return new KeywordAbility
    {
      RawText = line,
      Keyword = parts.Length > 0 ? parts[0] : "",
      Effect = parts.Length > 1 ? parts[1] : "",
    };
  }

  /// <summary>
  /// Parses a triggered ability line (format: "When/Whenever/At [trigger], [effect]").
  /// </summary>
  private static TriggeredAbility ParseTriggeredAbility(string line)
  {
    var parts = line.Split(',', 2, StringSplitOptions.TrimEntries);
    return new TriggeredAbility
    {
      RawText = line,
      Trigger = parts.Length > 0 ? parts[0] : "",
      Effect = parts.Length > 1 ? parts[1] : "",
    };
  }

  /// <summary>
  /// Parses an activated ability line (format: "Cost1, Cost2, Cost3: Effect").
  /// </summary>
  private static ActivatedAbility ParseActivatedAbility(string line)
  {
    var parts = line.Split(':', 2, StringSplitOptions.TrimEntries);
    var costString = parts.Length > 0 ? parts[0] : "";
    var effect = parts.Length > 1 ? parts[1] : "";

    // Split costs by ", " and trim each
    var costs = costString
      .Split(", ", StringSplitOptions.RemoveEmptyEntries)
      .Select(c => c.Trim())
      .ToList();

    return new ActivatedAbility
    {
      RawText = line,
      Costs = costs,
      Effect = effect,
    };
  }
}
