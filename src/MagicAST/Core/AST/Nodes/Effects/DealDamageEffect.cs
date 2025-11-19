using MagicAST.Core.AST.Nodes.Expressions;
using MagicAST.Core.AST.Nodes.Targets;
using MagicAST.Core.AST.Visitors;

namespace MagicAST.Core.AST.Nodes.Effects;

/// <summary>
/// Represents an effect that deals damage.
/// Example: "Lightning Strike deals 3 damage to any target", "Midnight Reaper deals 1 damage to you"
/// </summary>
public class DealDamageEffect : EffectNode
{
  /// <summary>
  /// Source of the damage (usually the card itself or "this").
  /// </summary>
  public DamageSource Source { get; init; } = DamageSource.Self;

  /// <summary>
  /// Target of the damage.
  /// </summary>
  public required DamageTarget Target { get; init; }

  /// <summary>
  /// Amount of damage to deal.
  /// </summary>
  public required ValueExpression Amount { get; init; }

  public override T Accept<T>(IASTVisitor<T> visitor) => visitor.VisitDealDamageEffect(this);

  public override IEnumerable<ASTNode> GetChildren()
  {
    yield return Amount;
  }
}

/// <summary>
/// Source of damage (simplified for Phase 1).
/// </summary>
public enum DamageSource
{
  /// <summary>
  /// The card itself deals the damage.
  /// </summary>
  Self,

  /// <summary>
  /// Referenced from spell target.
  /// </summary>
  Target,
}

/// <summary>
/// Target of damage.
/// </summary>
public enum DamageTarget
{
  /// <summary>
  /// Damage to the controller (you).
  /// </summary>
  You,

  /// <summary>
  /// Damage to an opponent.
  /// </summary>
  Opponent,

  /// <summary>
  /// Damage to "any target" (player, creature, or planeswalker).
  /// </summary>
  AnyTarget,

  /// <summary>
  /// Damage to a targeted creature.
  /// </summary>
  TargetCreature,

  /// <summary>
  /// Damage to a targeted player.
  /// </summary>
  TargetPlayer,
}
