using MagicAST.Core.AST.Nodes.Costs;
using MagicAST.Core.AST.Visitors;
using MagicAST.Core.Keywords;

namespace MagicAST.Core.AST.Nodes.Abilities;

/// <summary>
/// Represents a keyword ability (e.g., Flying, Vigilance, Haste).
/// Keywords may have reminder text.
/// Phase 1: Extended to support parametric keywords (Equip {N}, Cycling {N}, Absorb N, Protection from X).
/// </summary>
public class KeywordAbilityNode : AbilityNode
{
  /// <summary>
  /// The keyword being granted.
  /// </summary>
  public required Keyword Keyword { get; init; }

  /// <summary>
  /// Optional reminder text for the keyword.
  /// </summary>
  public string? ReminderText { get; init; }

  /// <summary>
  /// Amount parameter for keywords like Absorb N, Afflict N, Annihilator N.
  /// Phase 1: Supports numeric parameters.
  /// </summary>
  public int? Amount { get; init; }

  /// <summary>
  /// Mana cost parameter for keywords like Equip {N}, Cycling {N}.
  /// Phase 1: Supports mana cost parameters.
  /// </summary>
  public ManaCostNode? Cost { get; init; }

  /// <summary>
  /// Filter parameter for keywords like Protection from X, Landwalk.
  /// Phase 1: String-based filter (color, type, or card characteristic).
  /// Future: May be replaced with FilterNode for complex filters.
  /// </summary>
  public string? Filter { get; init; }

  public override T Accept<T>(IASTVisitor<T> visitor) => visitor.VisitKeywordAbility(this);

  public override IEnumerable<ASTNode> GetChildren()
  {
    // Phase 1: Cost is a child node if present
    if (Cost != null)
      yield return Cost;
  }
}
