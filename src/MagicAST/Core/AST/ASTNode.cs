using MagicAST.Core.AST.Visitors;

namespace MagicAST.Core.AST;

/// <summary>
/// Base class for all AST nodes in the MagicAST tree structure.
/// Implements composite pattern for tree traversal and visitor pattern for operations.
/// </summary>
public abstract class ASTNode
{
  /// <summary>
  /// Original oracle text that generated this node.
  /// Used for error reporting and regeneration.
  /// </summary>
  public string? SourceText { get; set; }

  /// <summary>
  /// Location in source file (line and column numbers).
  /// </summary>
  public SourceLocation? Location { get; set; }

  /// <summary>
  /// Extensibility point for custom metadata.
  /// Example: confidence scores, manual overrides, validation flags.
  /// </summary>
  public Dictionary<string, object> Metadata { get; init; } = new();

  /// <summary>
  /// Accept a visitor for traversal or transformation.
  /// Implements visitor pattern.
  /// </summary>
  /// <typeparam name="T">Return type of visitor.</typeparam>
  public abstract T Accept<T>(IASTVisitor<T> visitor);

  /// <summary>
  /// Get immediate children of this node.
  /// Used for tree traversal in visitors.
  /// </summary>
  public abstract IEnumerable<ASTNode> GetChildren();

  /// <summary>
  /// Get all descendants of this node (depth-first).
  /// </summary>
  public IEnumerable<ASTNode> GetDescendants()
  {
    foreach (var child in GetChildren())
    {
      yield return child;
      foreach (var descendant in child.GetDescendants())
      {
        yield return descendant;
      }
    }
  }

  /// <summary>
  /// Find nodes matching a predicate.
  /// </summary>
  public IEnumerable<ASTNode> Find(Func<ASTNode, bool> predicate)
  {
    if (predicate(this))
    {
      yield return this;
    }

    foreach (var descendant in GetDescendants())
    {
      if (predicate(descendant))
      {
        yield return descendant;
      }
    }
  }
}
