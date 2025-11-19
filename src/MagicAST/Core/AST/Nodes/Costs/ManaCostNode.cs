using MagicAST.Core.AST.Visitors;
using MagicAST.Core.ManaSystem;

namespace MagicAST.Core.AST.Nodes.Costs;

/// <summary>
/// Represents a mana cost.
/// Example: {2}{R}, {T}, {X}{G}{G}
/// </summary>
public class ManaCostNode : CostNode
{
  /// <summary>
  /// The mana cost to pay.
  /// </summary>
  public required ManaCost Cost { get; init; }

  public override T Accept<T>(IASTVisitor<T> visitor) => visitor.VisitManaCost(this);

  public override IEnumerable<ASTNode> GetChildren()
  {
    yield break; // No child nodes
  }
}
