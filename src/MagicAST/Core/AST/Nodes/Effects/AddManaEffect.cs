using MagicAST.Core.AST.Visitors;
using MagicAST.Core.ManaSystem;

namespace MagicAST.Core.AST.Nodes.Effects;

/// <summary>
/// Represents an effect that adds mana to a player's mana pool.
/// Example: "Add {C}{C}", "Add {G}{G}{G}"
/// </summary>
public class AddManaEffect : EffectNode
{
  /// <summary>
  /// The mana value to add to the mana pool.
  /// </summary>
  public required ManaValue ManaToAdd { get; init; }

  public override T Accept<T>(IASTVisitor<T> visitor) => visitor.VisitAddManaEffect(this);

  public override IEnumerable<ASTNode> GetChildren()
  {
    yield break; // No child nodes
  }
}
