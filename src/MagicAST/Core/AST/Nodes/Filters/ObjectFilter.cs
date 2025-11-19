using MagicAST.Core.AST.Visitors;

namespace MagicAST.Core.AST.Nodes.Filters;

/// <summary>
/// Base class for object filters (from Forge Valid$ pattern).
/// Filters specify which game objects match certain criteria.
/// </summary>
public abstract class ObjectFilter : ASTNode { }
