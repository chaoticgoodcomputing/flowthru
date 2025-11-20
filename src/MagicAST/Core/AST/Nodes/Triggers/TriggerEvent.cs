using MagicAST.Core.AST.Nodes.Filters;
using MagicAST.Core.AST.Visitors;

namespace MagicAST.Core.AST.Nodes.Triggers;

/// <summary>
/// Represents a trigger event for triggered abilities.
/// Example: "Whenever a nontoken creature you control dies", "At the beginning of your upkeep"
/// </summary>
public class TriggerEvent : ASTNode
{
  /// <summary>
  /// The type of event that triggers.
  /// </summary>
  public required EventType Type { get; init; }

  /// <summary>
  /// Optional filter for what objects trigger this.
  /// Example: "nontoken creature you control"
  /// </summary>
  public ObjectFilter? Filter { get; init; }

  public override T Accept<T>(IASTVisitor<T> visitor) => visitor.VisitTriggerEvent(this);

  public override IEnumerable<ASTNode> GetChildren()
  {
    if (Filter != null)
    {
      yield return Filter;
    }
  }
}

/// <summary>
/// Types of trigger events.
/// </summary>
public enum EventType
{
  /// <summary>
  /// When a permanent enters the battlefield.
  /// </summary>
  Enters,

  /// <summary>
  /// When a permanent leaves the battlefield.
  /// </summary>
  Leaves,

  /// <summary>
  /// When a creature dies (goes to graveyard from battlefield).
  /// </summary>
  Dies,

  /// <summary>
  /// When a creature attacks.
  /// </summary>
  Attacks,

  /// <summary>
  /// When a creature blocks.
  /// </summary>
  Blocks,

  /// <summary>
  /// At the beginning of a phase/step.
  /// </summary>
  PhaseBegin,

  /// <summary>
  /// At the end of a phase/step.
  /// </summary>
  PhaseEnd,

  /// <summary>
  /// When a spell is cast.
  /// </summary>
  SpellCast,

  /// <summary>
  /// When a permanent becomes tapped.
  /// </summary>
  BecomesTapped,

  /// <summary>
  /// When a permanent becomes untapped.
  /// </summary>
  BecomesUntapped,

  /// <summary>
  /// When damage is dealt.
  /// </summary>
  DamageDealt,
}
