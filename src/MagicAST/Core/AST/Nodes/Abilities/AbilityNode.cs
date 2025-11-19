using MagicAST.Core.AST.Visitors;

namespace MagicAST.Core.AST.Nodes.Abilities;

/// <summary>
/// Base class for all ability nodes.
/// </summary>
public abstract class AbilityNode : ASTNode
{
  /// <summary>
  /// Unique identifier for this ability instance.
  /// Used for tracking and references.
  /// </summary>
  public string? AbilityId { get; init; }
}
