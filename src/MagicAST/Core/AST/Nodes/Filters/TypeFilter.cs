using MagicAST.Core.AST.Visitors;
using MagicAST.Core.CardTypes;

namespace MagicAST.Core.AST.Nodes.Filters;

/// <summary>
/// Filter by card type or subtype.
/// Example: "Creature", "Artifact", "Dog", "Zombie"
/// </summary>
public class TypeFilter : ObjectFilter
{
  /// <summary>
  /// Card types to match.
  /// </summary>
  public List<CardType> CardTypes { get; init; } = new();

  /// <summary>
  /// Subtypes to match.
  /// </summary>
  public List<string> Subtypes { get; init; } = new();

  public override T Accept<T>(IASTVisitor<T> visitor) => visitor.VisitTypeFilter(this);

  public override IEnumerable<ASTNode> GetChildren()
  {
    yield break; // No child nodes
  }
}
