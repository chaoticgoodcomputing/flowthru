using MagicAST.Core.AST.Visitors;

namespace MagicAST.Core.AST.Nodes.Costs;

/// <summary>
/// Represents a tap cost ({T}).
/// The source permanent must be untapped to pay this cost.
/// </summary>
public class TapCostNode : CostNode
{
  public override T Accept<T>(IASTVisitor<T> visitor) => visitor.VisitTapCost(this);

  public override IEnumerable<ASTNode> GetChildren()
  {
    yield break; // No child nodes
  }
}
