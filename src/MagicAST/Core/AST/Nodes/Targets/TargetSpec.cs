using MagicAST.Core.AST.Nodes.Filters;
using MagicAST.Core.AST.Visitors;

namespace MagicAST.Core.AST.Nodes.Targets;

/// <summary>
/// Represents a target specification for a spell or ability.
/// </summary>
public class TargetSpec : ASTNode
{
  /// <summary>
  /// The type of target.
  /// </summary>
  public required TargetType Type { get; init; }

  /// <summary>
  /// Optional filter for legal targets.
  /// </summary>
  public ObjectFilter? Filter { get; init; }

  public override T Accept<T>(IASTVisitor<T> visitor) => visitor.VisitTargetSpec(this);

  public override IEnumerable<ASTNode> GetChildren()
  {
    if (Filter != null)
    {
      yield return Filter;
    }
  }
}

/// <summary>
/// Type of target for spells and abilities.
/// </summary>
public enum TargetType
{
  /// <summary>
  /// Target creature.
  /// </summary>
  Creature,

  /// <summary>
  /// Target player.
  /// </summary>
  Player,

  /// <summary>
  /// Any target (player, creature, or planeswalker).
  /// </summary>
  AnyTarget,

  /// <summary>
  /// Target permanent.
  /// </summary>
  Permanent,

  /// <summary>
  /// Target spell.
  /// </summary>
  Spell,
}
