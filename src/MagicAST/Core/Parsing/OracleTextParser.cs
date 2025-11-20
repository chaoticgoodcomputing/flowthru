using MagicAST.Core.AST.Nodes.Abilities;
using MagicAST.Core.Diagnostics;
using MagicAST.Core.Keywords;

namespace MagicAST.Core.Parsing;

/// <summary>
/// Parses Magic: The Gathering oracle text into ability nodes.
/// Phase 0: Simple keyword abilities only.
/// </summary>
public static class OracleTextParser
{
  private static readonly Dictionary<string, Keyword> KeywordMap =
    new(StringComparer.OrdinalIgnoreCase)
    {
      // Evasion
      ["flying"] = Keyword.Flying,
      ["menace"] = Keyword.Menace,
      ["fear"] = Keyword.Fear,
      ["intimidate"] = Keyword.Intimidate,
      ["shadow"] = Keyword.Shadow,
      ["horsemanship"] = Keyword.Horsemanship,
      ["skulk"] = Keyword.Skulk,
      ["unblockable"] = Keyword.Unblockable,
      // Combat
      ["vigilance"] = Keyword.Vigilance,
      ["haste"] = Keyword.Haste,
      ["first strike"] = Keyword.FirstStrike,
      ["double strike"] = Keyword.DoubleStrike,
      ["deathtouch"] = Keyword.Deathtouch,
      ["lifelink"] = Keyword.Lifelink,
      ["trample"] = Keyword.Trample,
      ["defender"] = Keyword.Defender,
      ["reach"] = Keyword.Reach,
      ["flanking"] = Keyword.Flanking,
      ["banding"] = Keyword.Banding,
      // Protection
      ["hexproof"] = Keyword.Hexproof,
      ["shroud"] = Keyword.Shroud,
      ["indestructible"] = Keyword.Indestructible,
      ["ward"] = Keyword.Ward,
      ["totem armor"] = Keyword.TotemArmor,
      // Graveyard/Recursion
      ["undying"] = Keyword.Undying,
      ["persist"] = Keyword.Persist,
      ["unearth"] = Keyword.Unearth,
      ["flashback"] = Keyword.Flashback,
      ["retrace"] = Keyword.Retrace,
      // Damage modification
      ["wither"] = Keyword.Wither,
      ["infect"] = Keyword.Infect,
      // Cost reduction / Casting
      ["flash"] = Keyword.Flash,
      ["convoke"] = Keyword.Convoke,
      ["delve"] = Keyword.Delve,
      ["affinity"] = Keyword.Affinity,
      ["improvise"] = Keyword.Improvise,
      // Tribal / Type changing
      ["changeling"] = Keyword.Changeling,
      ["prowl"] = Keyword.Prowl,
      // Triggered keyword abilities
      ["prowess"] = Keyword.Prowess,
      ["evolve"] = Keyword.Evolve,
      ["extort"] = Keyword.Extort,
      ["landfall"] = Keyword.Landfall,
      // Spell mechanics
      ["rebound"] = Keyword.Rebound,
      ["split second"] = Keyword.SplitSecond,
      ["storm"] = Keyword.Storm,
      ["cascade"] = Keyword.Cascade,
      ["ripple"] = Keyword.Ripple,
      // Other
      ["landwalk"] = Keyword.Landwalk,
      // Phase 1: Simple keywords
      ["devoid"] = Keyword.Devoid,
      ["partner"] = Keyword.Partner,
      ["companion"] = Keyword.Companion,
      ["mutate"] = Keyword.Mutate,
      ["foretell"] = Keyword.Foretell,
      ["boast"] = Keyword.Boast,
      ["disturb"] = Keyword.Disturb,
      ["decayed"] = Keyword.Decayed,
      ["training"] = Keyword.Training,
      ["reconfigure"] = Keyword.Reconfigure,
      ["toxic"] = Keyword.Toxic,
      ["backup"] = Keyword.Backup,
      ["bargain"] = Keyword.Bargain,
    };

  /// <summary>
  /// Parses oracle text into ability nodes.
  /// Phase 0: Only parses simple keyword abilities.
  /// </summary>
  /// <param name="oracleText">The oracle text to parse.</param>
  /// <param name="cardName">Card name for diagnostic locations.</param>
  /// <returns>Parse result containing abilities and diagnostics.</returns>
  public static ParseResult Parse(string oracleText, string cardName)
  {
    var abilities = new List<AbilityNode>();
    var diagnostics = new DiagnosticBag();
    var sourceText = SourceText.From(oracleText);

    if (string.IsNullOrWhiteSpace(oracleText))
    {
      return new ParseResult(abilities, diagnostics.ToImmutableArray().ToList());
    }

    // Split into lines - each line is typically a separate ability or sentence
    var lines = oracleText.Split('\n', StringSplitOptions.RemoveEmptyEntries);
    int position = 0;
    bool anyAbilityParsed = false;

    foreach (var line in lines)
    {
      var trimmedLine = line.Trim();

      if (string.IsNullOrWhiteSpace(trimmedLine))
      {
        position += line.Length + 1; // +1 for newline
        continue;
      }

      // Phase 3: Try to parse as triggered ability first (starts with When/Whenever/At)
      if (
        trimmedLine.StartsWith("When ", StringComparison.OrdinalIgnoreCase)
        || trimmedLine.StartsWith("Whenever ", StringComparison.OrdinalIgnoreCase)
        || trimmedLine.StartsWith("At ", StringComparison.OrdinalIgnoreCase)
      )
      {
        var triggeredResult = TriggeredAbilityParser.Parse(trimmedLine, cardName);
        if (triggeredResult.Abilities.Count > 0)
        {
          abilities.AddRange(triggeredResult.Abilities);
          anyAbilityParsed = true;
          // Add any diagnostics from the parser
          diagnostics.AddRange(triggeredResult.Diagnostics);
          position += line.Length + 1;
          continue;
        }
      }

      // Phase 2: Try to parse as activated ability (contains colon)
      if (trimmedLine.Contains(':'))
      {
        var activatedResult = ActivatedAbilityParser.Parse(trimmedLine, cardName);
        if (activatedResult.Abilities.Count > 0)
        {
          abilities.AddRange(activatedResult.Abilities);
          anyAbilityParsed = true;
          // Add any diagnostics from the parser
          diagnostics.AddRange(activatedResult.Diagnostics);
          position += line.Length + 1;
          continue;
        }
      }

      // Try to parse as keywords
      var result = TryParseKeywords(trimmedLine, sourceText, position, cardName, diagnostics);

      if (result.Parsed)
      {
        abilities.AddRange(result.Abilities);
        anyAbilityParsed = true;
      }
      else
      {
        // Not keywords - report as not implemented
        var location = Location.Create(
          sourceText,
          new TextSpan(position, trimmedLine.Length),
          cardName
        );
        diagnostics.Report(Descriptors.UnknownAbilityPattern, location, trimmedLine);
      }

      position += line.Length + 1; // +1 for newline
    }

    // If we didn't parse anything, report overall failure
    if (!anyAbilityParsed && !string.IsNullOrWhiteSpace(oracleText))
    {
      var location = Location.Create(sourceText, new TextSpan(0, oracleText.Length), cardName);
      diagnostics.Report(Descriptors.OracleTextNotImplemented, location);
    }

    return new ParseResult(abilities, diagnostics.ToImmutableArray().ToList());
  }

  /// <summary>
  /// Splits a keyword line by commas, but not commas inside parentheses (reminder text).
  /// Example: "Flying, Cycling {2} ({2}, Discard: Draw.)" splits into ["Flying", "Cycling {2} ({2}, Discard: Draw.)"]
  /// </summary>
  private static List<string> SplitKeywords(string line)
  {
    var parts = new List<string>();
    var currentPart = new System.Text.StringBuilder();
    int parenDepth = 0;

    foreach (char c in line)
    {
      if (c == '(')
      {
        parenDepth++;
        currentPart.Append(c);
      }
      else if (c == ')')
      {
        parenDepth--;
        currentPart.Append(c);
      }
      else if (c == ',' && parenDepth == 0)
      {
        // Comma outside parentheses - split here
        var part = currentPart.ToString().Trim();
        if (!string.IsNullOrWhiteSpace(part))
        {
          parts.Add(part);
        }
        currentPart.Clear();
      }
      else
      {
        currentPart.Append(c);
      }
    }

    // Add the last part
    var lastPart = currentPart.ToString().Trim();
    if (!string.IsNullOrWhiteSpace(lastPart))
    {
      parts.Add(lastPart);
    }

    return parts;
  }

  /// <summary>
  /// Extracts keyword text and reminder text from an ability string.
  /// Reminder text is enclosed in parentheses.
  /// </summary>
  /// <param name="text">The text to parse (e.g., "Flying (This creature can't be blocked...)")</param>
  /// <returns>Tuple of (keyword text, reminder text or null)</returns>
  private static (string Keyword, string? ReminderText) ExtractReminderText(string text)
  {
    int parenStart = text.IndexOf('(');
    if (parenStart >= 0 && text.EndsWith(')'))
    {
      var keyword = text.Substring(0, parenStart).Trim();
      var reminder = text.Substring(parenStart + 1, text.Length - parenStart - 2).Trim();
      return (keyword, reminder);
    }
    return (text, null);
  }

  /// <summary>
  /// Attempts to parse a line as keyword abilities.
  /// Phase 1: Extended to handle parametric keywords (Cycling {N}, Equip {N}).
  /// </summary>
  private static (bool Parsed, List<AbilityNode> Abilities) TryParseKeywords(
    string line,
    SourceText sourceText,
    int lineStart,
    string cardName,
    DiagnosticBag diagnostics
  )
  {
    var abilities = new List<AbilityNode>();

    // Quick heuristic: keywords are short lines without colons or trigger words
    if (!LooksLikeKeywordLine(line))
    {
      return (false, abilities);
    }

    // Split by comma, but not commas inside parentheses (reminder text)
    var parts = SplitKeywords(line);
    bool anyParsed = false;

    foreach (var part in parts)
    {
      var trimmed = part.Trim();

      // Extract reminder text BEFORE removing it
      var (keywordText, reminderText) = ExtractReminderText(trimmed);

      // Phase 1: Try to parse parametric keywords first
      var parametricResult = TryParseParametricKeyword(keywordText, reminderText, cardName);
      if (parametricResult.Parsed)
      {
        abilities.Add(parametricResult.Ability!);
        anyParsed = true;
      }
      // Try to match as simple keyword
      else if (KeywordMap.TryGetValue(keywordText, out var keyword))
      {
        abilities.Add(new KeywordAbilityNode { Keyword = keyword, ReminderText = reminderText });
        anyParsed = true;
      }
      else
      {
        // Unknown keyword - report warning
        var partStart = line.IndexOf(trimmed, StringComparison.Ordinal);
        if (partStart >= 0)
        {
          var location = Location.Create(
            sourceText,
            new TextSpan(lineStart + partStart, trimmed.Length),
            cardName
          );
          diagnostics.Report(Descriptors.UnsupportedKeyword, location, keywordText);
        }
      }
    }

    return (anyParsed, abilities);
  }

  /// <summary>
  /// Quick heuristic to check if a line looks like keywords.
  /// Phase 1: Updated to allow parametric keywords (Cycling {N}, Equip {N}).
  /// </summary>
  private static bool LooksLikeKeywordLine(string line)
  {
    // FIRST: Strip reminder text before checking length/content
    var (withoutReminder, _) = ExtractReminderText(line);

    // Keywords without reminder text are short (under 100 characters for parametric keywords)
    // Phase 1: Increased from 50 to 100 to accommodate "Cycling {2}" patterns
    if (withoutReminder.Length > 100)
      return false;

    // Phase 1: Allow colons only if they appear after "Equip" (for reminder text)
    // Activated abilities have colons in the format "Cost: Effect"
    if (
      withoutReminder.Contains(':')
      && !withoutReminder.StartsWith("Equip", StringComparison.OrdinalIgnoreCase)
    )
      return false;

    // Doesn't start with trigger words
    var lower = withoutReminder.ToLowerInvariant();
    if (
      lower.StartsWith("when ")
      || lower.StartsWith("whenever ")
      || lower.StartsWith("at ")
      || lower.StartsWith("during ")
    )
      return false;

    // Reject static ability patterns
    if (
      lower.Contains("can't")
      || lower.Contains("gets +")
      || lower.Contains("gets -")
      || lower.Contains("has protection")
      || lower.Contains("you control")
      || lower.Contains("you may")
      || lower.Contains("equipped creature")
      || lower.Contains("enchanted creature")
      || lower.Contains("enters the battlefield")
      || lower.Contains("deals damage")
      || lower.Contains("attacks")
      || lower.Contains("dies")
    )
      return false;

    // Must contain at least one known keyword or parametric keyword pattern
    var parts = withoutReminder.Split(
      ',',
      StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries
    );
    return parts.Any(part =>
    {
      var (keywordText, _) = ExtractReminderText(part.Trim());

      // Check simple keywords
      if (KeywordMap.ContainsKey(keywordText))
        return true;

      // Phase 1: Check parametric keyword patterns (Cycling {N}, Equip {N})
      return IsParametricKeywordPattern(keywordText);
    });
  }

  /// <summary>
  /// Phase 1: Checks if text matches a parametric keyword pattern.
  /// Patterns: "Cycling {N}", "Equip {N}", "Absorb N", etc.
  /// </summary>
  private static bool IsParametricKeywordPattern(string text)
  {
    // Patterns with mana costs
    if (
      System.Text.RegularExpressions.Regex.IsMatch(
        text,
        @"^(Cycling|Equip|Ninjutsu|Madness|Transmute)\s+\{",
        System.Text.RegularExpressions.RegexOptions.IgnoreCase
      )
    )
      return true;

    // Patterns with amounts
    if (
      System.Text.RegularExpressions.Regex.IsMatch(
        text,
        @"^(Absorb|Afflict|Amplify|Annihilator|Bushido|Rampage|Fading|Vanishing|Modular|Crew)\s+\d+",
        System.Text.RegularExpressions.RegexOptions.IgnoreCase
      )
    )
      return true;

    // Patterns with filters
    if (
      System.Text.RegularExpressions.Regex.IsMatch(
        text,
        @"^Protection from ",
        System.Text.RegularExpressions.RegexOptions.IgnoreCase
      )
    )
      return true;

    return false;
  }

  /// <summary>
  /// Phase 1: Attempts to parse a parametric keyword (Cycling {N}, Equip {N}, Absorb N).
  /// </summary>
  private static (bool Parsed, KeywordAbilityNode? Ability) TryParseParametricKeyword(
    string keywordText,
    string? reminderText,
    string cardName
  )
  {
    // Try mana cost keywords (Cycling, Equip, etc.)
    var manaCostMatch = System.Text.RegularExpressions.Regex.Match(
      keywordText,
      @"^(Cycling|Equip|Ninjutsu|Madness|Transmute)\s+(\{[^}]+\}(?:\{[^}]+\})*)",
      System.Text.RegularExpressions.RegexOptions.IgnoreCase
    );

    if (manaCostMatch.Success)
    {
      var keywordName = manaCostMatch.Groups[1].Value;
      var costString = manaCostMatch.Groups[2].Value;

      // Map keyword name to enum
      var keyword = keywordName.ToLowerInvariant() switch
      {
        "cycling" => Keyword.Cycling,
        "equip" => Keyword.Equip,
        "ninjutsu" => Keyword.Ninjutsu,
        "madness" => Keyword.Madness,
        "transmute" => Keyword.Transmute,
        _ => (Keyword?)null,
      };

      if (keyword.HasValue)
      {
        var costResult = ManaCostParser.Parse(costString, cardName);
        if (costResult.Result != null)
        {
          return (
            true,
            new KeywordAbilityNode
            {
              Keyword = keyword.Value,
              Cost = costResult.Result,
              ReminderText = reminderText,
            }
          );
        }
      }
    }

    // Try amount keywords (Absorb, Afflict, etc.)
    var amountMatch = System.Text.RegularExpressions.Regex.Match(
      keywordText,
      @"^(Absorb|Afflict|Amplify|Annihilator|Bushido|Rampage|Fading|Vanishing|Modular|Crew)\s+(\d+)",
      System.Text.RegularExpressions.RegexOptions.IgnoreCase
    );

    if (amountMatch.Success)
    {
      var keywordName = amountMatch.Groups[1].Value;
      var amountString = amountMatch.Groups[2].Value;

      if (int.TryParse(amountString, out var amount))
      {
        var keyword = keywordName.ToLowerInvariant() switch
        {
          "absorb" => Keyword.Absorb,
          "afflict" => Keyword.Afflict,
          "amplify" => Keyword.Amplify,
          "annihilator" => Keyword.Annihilator,
          "bushido" => Keyword.Bushido,
          "rampage" => Keyword.Rampage,
          "fading" => Keyword.Fading,
          "vanishing" => Keyword.Vanishing,
          "modular" => Keyword.Modular,
          "crew" => Keyword.Crew,
          _ => (Keyword?)null,
        };

        if (keyword.HasValue)
        {
          return (
            true,
            new KeywordAbilityNode
            {
              Keyword = keyword.Value,
              Amount = amount,
              ReminderText = reminderText,
            }
          );
        }
      }
    }

    // Try protection keywords
    var protectionMatch = System.Text.RegularExpressions.Regex.Match(
      keywordText,
      @"^Protection from (.+)$",
      System.Text.RegularExpressions.RegexOptions.IgnoreCase
    );

    if (protectionMatch.Success)
    {
      var filter = protectionMatch.Groups[1].Value.Trim();
      return (
        true,
        new KeywordAbilityNode
        {
          Keyword = Keyword.Protection,
          Filter = filter,
          ReminderText = reminderText,
        }
      );
    }

    return (false, null);
  }
}

/// <summary>
/// Result of parsing oracle text.
/// </summary>
public record ParseResult(List<AbilityNode> Abilities, List<Diagnostic> Diagnostics);
