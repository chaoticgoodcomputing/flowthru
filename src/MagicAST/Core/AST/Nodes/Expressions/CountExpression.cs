using MagicAST.Core.AST.Nodes.Filters;
using MagicAST.Core.AST.Visitors;

namespace MagicAST.Core.AST.Nodes.Expressions;

/// <summary>
/// Represents a count expression (from Forge Count$ pattern).
/// Example: "each Swamp you control", "number of creatures in all graveyards"
/// </summary>
public class CountExpression : ValueExpression
{
  /// <summary>
  /// What is being counted.
  /// </summary>
  public required CountType Type { get; init; }

  /// <summary>
  /// Filter for what objects to count.
  /// </summary>
  public ObjectFilter? Filter { get; init; }

  /// <summary>
  /// Multiplier for the count (e.g., "for each" implies multiplier of 1 per object).
  /// Default is 1.
  /// </summary>
  public int Multiplier { get; init; } = 1;

  public override T Accept<T>(IASTVisitor<T> visitor) => visitor.VisitCountExpression(this);

  public override IEnumerable<ASTNode> GetChildren()
  {
    if (Filter != null)
    {
      yield return Filter;
    }
  }
}

/// <summary>
/// Type of count expression.
/// </summary>
public enum CountType
{
  /// <summary>
  /// Count permanents on the battlefield.
  /// </summary>
  Permanents,

  /// <summary>
  /// Count cards in a specific zone.
  /// </summary>
  CardsInZone,

  /// <summary>
  /// Count something about game state (life total, etc.).
  /// </summary>
  GameState,
}
