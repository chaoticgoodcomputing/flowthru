using MagicAST.Core.AST.Nodes.Abilities;
using MagicAST.Core.AST.Visitors;
using MagicAST.Core.CardTypes;
using MagicAST.Core.Diagnostics;
using MagicAST.Core.ManaSystem;

namespace MagicAST.Core.AST.Nodes;

/// <summary>
/// Root node representing a complete Magic: The Gathering card.
/// Contains all card characteristics and abilities.
/// </summary>
public class CardNode : ASTNode
{
  /// <summary>
  /// Card name. For split cards, use "Name1 // Name2" format.
  /// </summary>
  public required string Name { get; init; }

  /// <summary>
  /// Mana cost representation.
  /// Null for lands and cards with "no cost".
  /// </summary>
  public ManaCost? ManaCost { get; init; }

  /// <summary>
  /// Type line (supertypes, card types, subtypes).
  /// </summary>
  public required TypeLine TypeLine { get; init; }

  /// <summary>
  /// Power/Toughness for creatures.
  /// Null for non-creatures.
  /// </summary>
  public PowerToughness? PowerToughness { get; init; }

  /// <summary>
  /// All abilities on the card.
  /// Ordered as they appear in oracle text.
  /// </summary>
  public List<AbilityNode> Abilities { get; init; } = new();

  /// <summary>
  /// Parse diagnostics (warnings, errors) encountered during AST construction.
  /// Empty if the card parsed without issues.
  /// </summary>
  public List<Diagnostic> Diagnostics { get; init; } = new();

  public override T Accept<T>(IASTVisitor<T> visitor) => visitor.VisitCard(this);

  public override IEnumerable<ASTNode> GetChildren()
  {
    foreach (var ability in Abilities)
    {
      yield return ability;
    }
  }
}
