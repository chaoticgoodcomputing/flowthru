using MagicAST.Core.AST.Nodes.Expressions;
using MagicAST.Core.AST.Nodes.Filters;
using MagicAST.Core.AST.Visitors;

namespace MagicAST.Core.AST.Nodes.Effects;

/// <summary>
/// Represents an effect that modifies power and/or toughness.
/// Example: "All creatures get -1/-1 until end of turn", "Target creature gets +2/+2"
/// </summary>
public class PTModificationEffect : EffectNode
{
  /// <summary>
  /// Filter for which creatures are affected.
  /// Null means "all creatures".
  /// </summary>
  public ObjectFilter? AffectedCreatures { get; init; }

  /// <summary>
  /// Power modification (can be positive or negative).
  /// </summary>
  public required ValueExpression PowerModification { get; init; }

  /// <summary>
  /// Toughness modification (can be positive or negative).
  /// </summary>
  public required ValueExpression ToughnessModification { get; init; }

  /// <summary>
  /// Duration of the effect.
  /// </summary>
  public Duration Duration { get; init; } = Duration.UntilEndOfTurn;

  public override T Accept<T>(IASTVisitor<T> visitor) => visitor.VisitPTModificationEffect(this);

  public override IEnumerable<ASTNode> GetChildren()
  {
    if (AffectedCreatures != null)
    {
      yield return AffectedCreatures;
    }
    yield return PowerModification;
    yield return ToughnessModification;
  }
}

/// <summary>
/// Duration for effects.
/// </summary>
public enum Duration
{
  /// <summary>
  /// Effect lasts until end of current turn.
  /// </summary>
  UntilEndOfTurn,

  /// <summary>
  /// Permanent effect (or until card leaves battlefield).
  /// </summary>
  Permanent,

  /// <summary>
  /// Until end of combat.
  /// </summary>
  UntilEndOfCombat,
}
