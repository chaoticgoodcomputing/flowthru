using MagicAST.Core.AST.Nodes.Expressions;
using MagicAST.Core.AST.Visitors;

namespace MagicAST.Core.AST.Nodes.Effects;

/// <summary>
/// Represents an effect that causes a player to gain life.
/// Example: "you gain 2 life", "target player gains 5 life"
/// </summary>
public class GainLifeEffect : EffectNode
{
  /// <summary>
  /// Amount of life to gain.
  /// </summary>
  public required ValueExpression Amount { get; init; }

  /// <summary>
  /// Who gains the life.
  /// </summary>
  public LifeTarget Target { get; init; } = LifeTarget.You;

  public override T Accept<T>(IASTVisitor<T> visitor) => visitor.VisitGainLifeEffect(this);

  public override IEnumerable<ASTNode> GetChildren()
  {
    yield return Amount;
  }
}

/// <summary>
/// Target for life gain/loss effects.
/// </summary>
public enum LifeTarget
{
  /// <summary>
  /// You gain/lose life.
  /// </summary>
  You,

  /// <summary>
  /// Target player gains/loses life.
  /// </summary>
  TargetPlayer,

  /// <summary>
  /// Each opponent gains/loses life.
  /// </summary>
  EachOpponent,

  /// <summary>
  /// An opponent gains/loses life.
  /// </summary>
  Opponent,
}
