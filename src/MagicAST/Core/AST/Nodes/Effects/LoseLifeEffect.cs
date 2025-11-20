using MagicAST.Core.AST.Nodes.Expressions;
using MagicAST.Core.AST.Visitors;

namespace MagicAST.Core.AST.Nodes.Effects;

/// <summary>
/// Represents an effect that causes a player to lose life.
/// Example: "target player loses 2 life", "each opponent loses 1 life"
/// </summary>
public class LoseLifeEffect : EffectNode
{
  /// <summary>
  /// Amount of life to lose.
  /// </summary>
  public required ValueExpression Amount { get; init; }

  /// <summary>
  /// Who loses the life.
  /// </summary>
  public LifeTarget Target { get; init; } = LifeTarget.TargetPlayer;

  public override T Accept<T>(IASTVisitor<T> visitor) => visitor.VisitLoseLifeEffect(this);

  public override IEnumerable<ASTNode> GetChildren()
  {
    yield return Amount;
  }
}
