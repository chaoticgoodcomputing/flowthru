using MagicAST.Core.AST.Nodes.Costs;
using MagicAST.Core.AST.Nodes.Effects;
using MagicAST.Core.AST.Visitors;

namespace MagicAST.Core.AST.Nodes.Abilities;

/// <summary>
/// Represents an activated ability: [Costs]: [Effects]
/// Example: "{T}: Add {C}{C}" or "{2}{R}, Sacrifice a creature: Deal 3 damage to target creature."
/// </summary>
public class ActivatedAbilityNode : AbilityNode
{
  /// <summary>
  /// Costs that must be paid to activate.
  /// Ordered as they appear (important for variable binding).
  /// </summary>
  public List<CostNode> Costs { get; init; } = new();

  /// <summary>
  /// Effects that occur on resolution.
  /// Execute in order.
  /// </summary>
  public List<EffectNode> Effects { get; init; } = new();

  public override T Accept<T>(IASTVisitor<T> visitor) => visitor.VisitActivatedAbility(this);

  public override IEnumerable<ASTNode> GetChildren()
  {
    foreach (var cost in Costs)
    {
      yield return cost;
    }
    foreach (var effect in Effects)
    {
      yield return effect;
    }
  }
}
