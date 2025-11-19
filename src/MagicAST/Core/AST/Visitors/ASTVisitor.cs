using MagicAST.Core.AST.Nodes;
using MagicAST.Core.AST.Nodes.Abilities;
using MagicAST.Core.AST.Nodes.Costs;
using MagicAST.Core.AST.Nodes.Effects;
using MagicAST.Core.AST.Nodes.Expressions;
using MagicAST.Core.AST.Nodes.Filters;
using MagicAST.Core.AST.Nodes.Targets;
using MagicAST.Core.AST.Nodes.Triggers;

namespace MagicAST.Core.AST.Visitors;

/// <summary>
/// Base visitor with default implementations.
/// Allows selective override of visit methods.
/// </summary>
public abstract class ASTVisitor<T> : IASTVisitor<T>
{
  /// <summary>
  /// Default behavior: visit all children and return default value.
  /// Override in derived classes for custom aggregation.
  /// </summary>
  protected virtual T DefaultVisit(ASTNode node)
  {
    foreach (var child in node.GetChildren())
    {
      child.Accept(this);
    }
    return default(T)!;
  }

  public virtual T VisitCard(CardNode node) => DefaultVisit(node);

  public virtual T VisitActivatedAbility(ActivatedAbilityNode node) => DefaultVisit(node);

  public virtual T VisitSpellAbility(SpellAbilityNode node) => DefaultVisit(node);

  public virtual T VisitKeywordAbility(KeywordAbilityNode node) => DefaultVisit(node);

  public virtual T VisitTriggeredAbility(TriggeredAbilityNode node) => DefaultVisit(node);

  public virtual T VisitManaCost(ManaCostNode node) => DefaultVisit(node);

  public virtual T VisitTapCost(TapCostNode node) => DefaultVisit(node);

  public virtual T VisitAddManaEffect(AddManaEffect node) => DefaultVisit(node);

  public virtual T VisitDealDamageEffect(DealDamageEffect node) => DefaultVisit(node);

  public virtual T VisitPTModificationEffect(PTModificationEffect node) => DefaultVisit(node);

  public virtual T VisitDrawEffect(DrawEffect node) => DefaultVisit(node);

  public virtual T VisitStaticValue(StaticValue node) => DefaultVisit(node);

  public virtual T VisitVariableValue(VariableValue node) => DefaultVisit(node);

  public virtual T VisitCountExpression(CountExpression node) => DefaultVisit(node);

  public virtual T VisitTypeFilter(TypeFilter node) => DefaultVisit(node);

  public virtual T VisitControllerFilter(ControllerFilter node) => DefaultVisit(node);

  public virtual T VisitTokenFilter(TokenFilter node) => DefaultVisit(node);

  public virtual T VisitTargetSpec(TargetSpec node) => DefaultVisit(node);

  public virtual T VisitTriggerEvent(TriggerEvent node) => DefaultVisit(node);
}
