using MagicAST.Core.AST.Visitors;

namespace MagicAST.Core.AST.Nodes.Filters;

/// <summary>
/// Filter by token status.
/// Example: "nontoken creature", "token"
/// </summary>
public class TokenFilter : ObjectFilter
{
  /// <summary>
  /// Whether to match tokens or nontokens.
  /// </summary>
  public required TokenType TokenType { get; init; }

  public override T Accept<T>(IASTVisitor<T> visitor) => visitor.VisitTokenFilter(this);

  public override IEnumerable<ASTNode> GetChildren()
  {
    yield break; // No child nodes
  }
}

/// <summary>
/// Token filter type.
/// </summary>
public enum TokenType
{
  /// <summary>
  /// Match tokens only.
  /// </summary>
  Token,

  /// <summary>
  /// Match nontokens only.
  /// </summary>
  Nontoken,

  /// <summary>
  /// Match both tokens and nontokens.
  /// </summary>
  Any,
}
