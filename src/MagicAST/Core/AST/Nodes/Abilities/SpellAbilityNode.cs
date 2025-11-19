using MagicAST.Core.AST.Nodes.Effects;
using MagicAST.Core.AST.Nodes.Targets;
using MagicAST.Core.AST.Visitors;

namespace MagicAST.Core.AST.Nodes.Abilities;

/// <summary>
/// Represents a spell ability (instant or sorcery).
/// Example: "Lightning Strike deals 3 damage to any target."
/// </summary>
public class SpellAbilityNode : AbilityNode
{
  /// <summary>
  /// Targets for the spell (declared on cast).
  /// </summary>
  public List<TargetSpec> Targets { get; init; } = new();

  /// <summary>
  /// Effects that occur on resolution.
  /// </summary>
  public List<EffectNode> Effects { get; init; } = new();

  public override T Accept<T>(IASTVisitor<T> visitor) => visitor.VisitSpellAbility(this);

  public override IEnumerable<ASTNode> GetChildren()
  {
    foreach (var target in Targets)
    {
      yield return target;
    }
    foreach (var effect in Effects)
    {
      yield return effect;
    }
  }
}
