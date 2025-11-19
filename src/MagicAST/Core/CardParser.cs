using MagicAST.Core.AST;
using MagicAST.Core.AST.Nodes;
using MagicAST.Core.AST.Nodes.Abilities;
using MagicAST.Core.CardTypes;
using MagicAST.Core.ManaSystem;
using MagicAST.DTOs;

namespace MagicAST.Core;

/// <summary>
/// Parses Magic: The Gathering card data into an Abstract Syntax Tree (AST).
/// </summary>
/// <remarks>
/// This is the primary entry point for the MagicAST library.
/// Converts CardInputDto into a rich, strongly-typed CardNode AST.
/// </remarks>
public static class CardParser
{
  /// <summary>
  /// Parses a CardInputDto into a CardNode AST.
  /// </summary>
  /// <param name="input">The card data to parse.</param>
  /// <returns>A CardNode representing the parsed card, including any diagnostics.</returns>
  /// <remarks>
  /// This method never throws exceptions for parse failures.
  /// All errors are captured in the CardNode.Diagnostics list.
  /// Partial AST construction is supported - basic card properties will be
  /// populated even if oracle text parsing fails.
  /// </remarks>
  public static CardNode Parse(CardInputDto input)
  {
    var diagnostics = new List<ParseDiagnostic>();

    // Parse mana cost
    ManaCost? manaCost = null;
    if (!string.IsNullOrEmpty(input.ManaCost))
    {
      try
      {
        manaCost = ManaCost.Parse(input.ManaCost);
      }
      catch (Exception ex)
      {
        diagnostics.Add(
          ParseDiagnostic.Error(
            "AST001",
            $"Failed to parse mana cost: {ex.Message}",
            input.ManaCost
          )
        );
      }
    }

    // Parse type line
    TypeLine typeLine;
    try
    {
      typeLine = TypeLine.Parse(input.TypeLine);
    }
    catch (Exception ex)
    {
      diagnostics.Add(
        ParseDiagnostic.Error("AST002", $"Failed to parse type line: {ex.Message}", input.TypeLine)
      );
      // Provide a minimal fallback type line
      typeLine = new TypeLine { CardTypes = new List<CardType>() };
    }

    // Parse power/toughness for creatures
    PowerToughness? powerToughness = null;
    if (!string.IsNullOrEmpty(input.Power) && !string.IsNullOrEmpty(input.Toughness))
    {
      try
      {
        var ptString = $"{input.Power}/{input.Toughness}";
        powerToughness = PowerToughness.Parse(ptString);
      }
      catch (Exception ex)
      {
        diagnostics.Add(
          ParseDiagnostic.Warning(
            "AST003",
            $"Failed to parse power/toughness: {ex.Message}",
            $"{input.Power}/{input.Toughness}"
          )
        );
      }
    }

    // TODO: Parse oracle text into abilities
    // For now, we stub this with a diagnostic if oracle text exists
    if (!string.IsNullOrEmpty(input.OracleText))
    {
      diagnostics.Add(
        ParseDiagnostic.Error(
          "AST999",
          "Oracle text parsing not yet implemented - abilities list will be empty",
          input.OracleText
        )
      );
    }

    // Construct the CardNode
    return new CardNode
    {
      Name = input.Name,
      ManaCost = manaCost,
      TypeLine = typeLine,
      PowerToughness = powerToughness,
      Abilities = new List<AbilityNode>(), // Empty for now
      Diagnostics = diagnostics,
    };
  }
}
