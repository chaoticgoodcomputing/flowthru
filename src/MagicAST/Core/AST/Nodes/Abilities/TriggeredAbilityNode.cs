using MagicAST.Core.AST.Nodes.Effects;
using MagicAST.Core.AST.Nodes.Triggers;
using MagicAST.Core.AST.Visitors;

namespace MagicAST.Core.AST.Nodes.Abilities;

/// <summary>
/// Represents a triggered ability: "Whenever/When/At [event], [effect]"
/// Example: "Whenever a nontoken creature you control dies, this creature deals 1 damage to you and you draw a card."
/// </summary>
public class TriggeredAbilityNode : AbilityNode
{
  /// <summary>
  /// The trigger event that causes this ability to activate.
  /// </summary>
  public required TriggerEvent Trigger { get; init; }

  /// <summary>
  /// Effects that occur when triggered.
  /// </summary>
  public List<EffectNode> Effects { get; init; } = new();

  public override T Accept<T>(IASTVisitor<T> visitor) => visitor.VisitTriggeredAbility(this);

  public override IEnumerable<ASTNode> GetChildren()
  {
    yield return Trigger;
    foreach (var effect in Effects)
    {
      yield return effect;
    }
  }
}
