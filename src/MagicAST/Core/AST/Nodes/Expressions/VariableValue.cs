using MagicAST.Core.AST.Visitors;

namespace MagicAST.Core.AST.Nodes.Expressions;

/// <summary>
/// Represents a variable value (X).
/// The value is determined when the spell or ability is cast/activated.
/// </summary>
public class VariableValue : ValueExpression
{
  /// <summary>
  /// The variable name (typically "X", but could be "Y" or "Z").
  /// </summary>
  public required string VariableName { get; init; }

  /// <summary>
  /// Whether this is a positive or negative variable.
  /// For effects like "+X/-X".
  /// </summary>
  public bool IsNegative { get; init; }

  public override T Accept<T>(IASTVisitor<T> visitor) => visitor.VisitVariableValue(this);

  public override IEnumerable<ASTNode> GetChildren()
  {
    yield break; // No child nodes
  }
}
