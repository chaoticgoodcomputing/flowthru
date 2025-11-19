using MagicAST.Core.AST.Visitors;
using MagicAST.Core.Keywords;

namespace MagicAST.Core.AST.Nodes.Abilities;

/// <summary>
/// Represents a keyword ability (e.g., Flying, Vigilance, Haste).
/// Keywords may have reminder text.
/// </summary>
public class KeywordAbilityNode : AbilityNode
{
  /// <summary>
  /// The keyword being granted.
  /// </summary>
  public required Keyword Keyword { get; init; }

  /// <summary>
  /// Optional reminder text for the keyword.
  /// </summary>
  public string? ReminderText { get; init; }

  public override T Accept<T>(IASTVisitor<T> visitor) => visitor.VisitKeywordAbility(this);

  public override IEnumerable<ASTNode> GetChildren()
  {
    yield break; // Keywords have no child nodes
  }
}
