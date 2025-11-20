using MagicAST.Core.AST.Nodes.Costs;
using MagicAST.Core.Diagnostics;
using MagicAST.Core.ManaSystem;
using Superpower;
using Superpower.Model;
using Superpower.Parsers;
using DiagnosticTextSpan = MagicAST.Core.Diagnostics.TextSpan;

namespace MagicAST.Core.Parsing;

/// <summary>
/// Parses mana cost strings into ManaCostNode objects.
/// Phase 1: Handles common patterns like {N}, {C}, {W}, {U}, {B}, {R}, {G}, {X}.
/// Future phases: Will expand to hybrid, Phyrexian, and complex costs.
/// </summary>
public static class ManaCostParser
{
  /// <summary>
  /// Parses a mana cost string (e.g., "{2}", "{2}{R}{R}", "{X}{G}") into a ManaCostNode.
  /// </summary>
  /// <param name="costString">The mana cost string to parse.</param>
  /// <param name="cardName">Card name for diagnostic locations.</param>
  /// <returns>Parse result containing ManaCostNode and diagnostics.</returns>
  public static ParseResult<ManaCostNode> Parse(string costString, string cardName)
  {
    var diagnostics = new DiagnosticBag();
    var sourceText = SourceText.From(costString);

    if (string.IsNullOrWhiteSpace(costString))
    {
      var location = Location.Create(sourceText, new DiagnosticTextSpan(0, 0), cardName);
      diagnostics.Report(Descriptors.InvalidManaCost, location, "Mana cost is empty");
      return new ParseResult<ManaCostNode>(null, diagnostics.ToImmutableArray().ToList());
    }

    try
    {
      // Use the existing ManaCost.Parse method which handles all mana cost patterns
      var manaCost = ManaCost.Parse(costString);

      var node = new ManaCostNode { Cost = manaCost };

      return new ParseResult<ManaCostNode>(node, diagnostics.ToImmutableArray().ToList());
    }
    catch (ArgumentException ex)
    {
      var location = Location.Create(
        sourceText,
        new DiagnosticTextSpan(0, costString.Length),
        cardName
      );
      diagnostics.Report(Descriptors.InvalidManaCost, location, ex.Message);
      return new ParseResult<ManaCostNode>(null, diagnostics.ToImmutableArray().ToList());
    }
  }

  /// <summary>
  /// Attempts to extract a mana cost from a text fragment.
  /// Looks for patterns like "{N}" or "{N}{C}" where N is a number and C is a color.
  /// </summary>
  /// <param name="text">Text potentially containing a mana cost.</param>
  /// <returns>Extracted mana cost string if found, or null.</returns>
  public static string? ExtractManaCost(string text)
  {
    if (string.IsNullOrWhiteSpace(text))
      return null;

    // Match mana cost patterns: one or more {X} symbols
    var match = System.Text.RegularExpressions.Regex.Match(
      text,
      @"(?:\{[^}]+\})+",
      System.Text.RegularExpressions.RegexOptions.IgnoreCase
    );

    return match.Success ? match.Value : null;
  }
}

/// <summary>
/// Generic parse result with diagnostics.
/// </summary>
public record ParseResult<T>(T? Result, List<Diagnostic> Diagnostics)
  where T : class;
