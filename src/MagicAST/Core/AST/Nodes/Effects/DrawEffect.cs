using MagicAST.Core.AST.Nodes.Expressions;
using MagicAST.Core.AST.Visitors;

namespace MagicAST.Core.AST.Nodes.Effects;

/// <summary>
/// Represents an effect that draws cards.
/// Example: "draw a card", "draw two cards"
/// </summary>
public class DrawEffect : EffectNode
{
  /// <summary>
  /// Number of cards to draw.
  /// </summary>
  public required ValueExpression NumberOfCards { get; init; }

  /// <summary>
  /// Player who draws the cards.
  /// </summary>
  public DrawTarget Player { get; init; } = DrawTarget.You;

  public override T Accept<T>(IASTVisitor<T> visitor) => visitor.VisitDrawEffect(this);

  public override IEnumerable<ASTNode> GetChildren()
  {
    yield return NumberOfCards;
  }
}

/// <summary>
/// Target player for draw effect.
/// </summary>
public enum DrawTarget
{
  /// <summary>
  /// You draw.
  /// </summary>
  You,

  /// <summary>
  /// Opponent draws.
  /// </summary>
  Opponent,

  /// <summary>
  /// Target player draws.
  /// </summary>
  TargetPlayer,
}
