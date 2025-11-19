using MagicAST.Core.AST.Visitors;

namespace MagicAST.Core.AST.Nodes.Expressions;

/// <summary>
/// Represents a static numeric value.
/// Example: 3, 1, 5
/// </summary>
public class StaticValue : ValueExpression
{
  /// <summary>
  /// The static numeric value.
  /// </summary>
  public required int Value { get; init; }

  public override T Accept<T>(IASTVisitor<T> visitor) => visitor.VisitStaticValue(this);

  public override IEnumerable<ASTNode> GetChildren()
  {
    yield break; // No child nodes
  }
}
