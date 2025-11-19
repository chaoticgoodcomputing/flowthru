using MagicAST.Core.AST.Visitors;

namespace MagicAST.Core.AST.Nodes.Filters;

/// <summary>
/// Filter by controller (from Forge .YouCtrl, .OppCtrl pattern).
/// Example: "you control", "opponent controls"
/// </summary>
public class ControllerFilter : ObjectFilter
{
  /// <summary>
  /// The controller type to filter by.
  /// </summary>
  public required Controller ControllerType { get; init; }

  public override T Accept<T>(IASTVisitor<T> visitor) => visitor.VisitControllerFilter(this);

  public override IEnumerable<ASTNode> GetChildren()
  {
    yield break; // No child nodes
  }
}

/// <summary>
/// Controller type for filtering.
/// </summary>
public enum Controller
{
  /// <summary>
  /// You control.
  /// </summary>
  You,

  /// <summary>
  /// Opponent controls.
  /// </summary>
  Opponent,

  /// <summary>
  /// Any player controls.
  /// </summary>
  Any,
}
