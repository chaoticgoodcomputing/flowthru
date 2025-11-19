using MagicAST.Core.AST.Visitors;

namespace MagicAST.Core.AST.Nodes.Expressions;

/// <summary>
/// Base class for value expressions.
/// Represents numeric values that can be static, variable, or computed.
/// </summary>
public abstract class ValueExpression : ASTNode { }
