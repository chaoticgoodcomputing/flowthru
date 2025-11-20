using MagicAST.Core.AST.Nodes.Abilities;
using MagicAST.Core.Diagnostics;
using MagicAST.Core.Keywords;
using Superpower;
using Superpower.Model;
using Superpower.Parsers;
using DiagnosticTextSpan = MagicAST.Core.Diagnostics.TextSpan;

namespace MagicAST.Core.Parsing;

/// <summary>
/// Parses keyword abilities using Superpower.
/// Phase 0: Simple keywords only (Flying, Haste, etc.)
/// </summary>
public static class KeywordAbilityParser
{
  // Map of keyword strings to MTGToken enum values
  private static readonly Dictionary<string, MTGToken> KeywordTokenMap =
    new(StringComparer.OrdinalIgnoreCase)
    {
      // Evasion
      ["flying"] = MTGToken.Flying,
      ["menace"] = MTGToken.Menace,
      ["fear"] = MTGToken.Fear,
      ["intimidate"] = MTGToken.Intimidate,
      ["shadow"] = MTGToken.Shadow,
      ["horsemanship"] = MTGToken.Horsemanship,
      ["skulk"] = MTGToken.Skulk,
      // Combat
      ["vigilance"] = MTGToken.Vigilance,
      ["haste"] = MTGToken.Haste,
      ["first strike"] = MTGToken.FirstStrike,
      ["double strike"] = MTGToken.DoubleStrike,
      ["deathtouch"] = MTGToken.Deathtouch,
      ["lifelink"] = MTGToken.Lifelink,
      ["trample"] = MTGToken.Trample,
      ["defender"] = MTGToken.Defender,
      ["reach"] = MTGToken.Reach,
      ["flanking"] = MTGToken.Flanking,
      ["banding"] = MTGToken.Banding,
      // Protection
      ["hexproof"] = MTGToken.Hexproof,
      ["shroud"] = MTGToken.Shroud,
      ["indestructible"] = MTGToken.Indestructible,
      ["ward"] = MTGToken.Ward,
      ["flash"] = MTGToken.Flash,
      // Additional keywords
      ["prowess"] = MTGToken.Prowess,
      ["changeling"] = MTGToken.Changeling,
      ["rebound"] = MTGToken.Rebound,
      ["split second"] = MTGToken.SplitSecond,
      ["storm"] = MTGToken.Storm,
      ["cascade"] = MTGToken.Cascade,
      ["evolve"] = MTGToken.Evolve,
      ["extort"] = MTGToken.Extort,
      ["undying"] = MTGToken.Undying,
      ["persist"] = MTGToken.Persist,
      ["wither"] = MTGToken.Wither,
      ["infect"] = MTGToken.Infect,
      ["convoke"] = MTGToken.Convoke,
      ["delve"] = MTGToken.Delve,
      ["prowl"] = MTGToken.Prowl,
      ["totem armor"] = MTGToken.TotemArmor,
    };

  // Map of MTGToken to Keyword enum
  private static readonly Dictionary<MTGToken, Keyword> TokenToKeywordMap =
    new()
    {
      // Evasion
      [MTGToken.Flying] = Keyword.Flying,
      [MTGToken.Menace] = Keyword.Menace,
      [MTGToken.Fear] = Keyword.Fear,
      [MTGToken.Intimidate] = Keyword.Intimidate,
      [MTGToken.Shadow] = Keyword.Shadow,
      [MTGToken.Horsemanship] = Keyword.Horsemanship,
      [MTGToken.Skulk] = Keyword.Skulk,
      // Combat
      [MTGToken.Vigilance] = Keyword.Vigilance,
      [MTGToken.Haste] = Keyword.Haste,
      [MTGToken.FirstStrike] = Keyword.FirstStrike,
      [MTGToken.DoubleStrike] = Keyword.DoubleStrike,
      [MTGToken.Deathtouch] = Keyword.Deathtouch,
      [MTGToken.Lifelink] = Keyword.Lifelink,
      [MTGToken.Trample] = Keyword.Trample,
      [MTGToken.Defender] = Keyword.Defender,
      [MTGToken.Reach] = Keyword.Reach,
      [MTGToken.Flanking] = Keyword.Flanking,
      [MTGToken.Banding] = Keyword.Banding,
      // Protection
      [MTGToken.Hexproof] = Keyword.Hexproof,
      [MTGToken.Shroud] = Keyword.Shroud,
      [MTGToken.Indestructible] = Keyword.Indestructible,
      [MTGToken.Ward] = Keyword.Ward,
      [MTGToken.Flash] = Keyword.Flash,
      // Additional keywords
      [MTGToken.Prowess] = Keyword.Prowess,
      [MTGToken.Changeling] = Keyword.Changeling,
      [MTGToken.Rebound] = Keyword.Rebound,
      [MTGToken.SplitSecond] = Keyword.SplitSecond,
      [MTGToken.Storm] = Keyword.Storm,
      [MTGToken.Cascade] = Keyword.Cascade,
      [MTGToken.Evolve] = Keyword.Evolve,
      [MTGToken.Extort] = Keyword.Extort,
      [MTGToken.Undying] = Keyword.Undying,
      [MTGToken.Persist] = Keyword.Persist,
      [MTGToken.Wither] = Keyword.Wither,
      [MTGToken.Infect] = Keyword.Infect,
      [MTGToken.Convoke] = Keyword.Convoke,
      [MTGToken.Delve] = Keyword.Delve,
      [MTGToken.Prowl] = Keyword.Prowl,
      [MTGToken.TotemArmor] = Keyword.TotemArmor,
    };

  /// <summary>
  /// Parses a keyword ability string into a list of KeywordAbilityNode.
  /// Phase 0: Simple text-based parsing (comma-separated keywords).
  /// Future phases will use Superpower for complex parsing.
  /// </summary>
  /// <param name="abilityText">The keyword ability text (e.g., "Flying, vigilance")</param>
  /// <param name="cardName">The name of the card (for diagnostics)</param>
  /// <returns>Parse result with ability nodes and diagnostics</returns>
  public static ParseResult Parse(string abilityText, string cardName)
  {
    var diagnostics = new DiagnosticBag();
    var sourceText = SourceText.From(abilityText);
    var abilities = new List<AbilityNode>();

    if (string.IsNullOrWhiteSpace(abilityText))
    {
      return new ParseResult(abilities, diagnostics.ToImmutableArray().ToList());
    }

    try
    {
      // Phase 0: Simple comma-separated parsing
      var keywords = abilityText.Split(
        ',',
        StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries
      );

      foreach (var keywordText in keywords)
      {
        if (KeywordTokenMap.TryGetValue(keywordText, out var token))
        {
          if (TokenToKeywordMap.TryGetValue(token, out var keyword))
          {
            abilities.Add(new KeywordAbilityNode { Keyword = keyword, ReminderText = null });
          }
        }
        else
        {
          // Unknown keyword - report as unsupported
          var location = Location.Create(
            sourceText,
            new DiagnosticTextSpan(0, abilityText.Length),
            cardName
          );
          diagnostics.Report(Descriptors.UnsupportedKeyword, location, keywordText);
        }
      }

      return new ParseResult(abilities, diagnostics.ToImmutableArray().ToList());
    }
    catch (Exception ex)
    {
      var location = Location.Create(
        sourceText,
        new DiagnosticTextSpan(0, abilityText.Length),
        cardName
      );
      diagnostics.Report(Descriptors.UnknownAbilityPattern, location, ex.Message);
      return new ParseResult(new List<AbilityNode>(), diagnostics.ToImmutableArray().ToList());
    }
  }
}
