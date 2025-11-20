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
/// Visitor interface for traversing and transforming AST nodes.
/// Type parameter T represents the return type of visit operations.
/// </summary>
public interface IASTVisitor<T>
{
  /// <summary>
  /// Visit a CardNode.
  /// </summary>
  T VisitCard(CardNode node);

  /// <summary>
  /// Visit an ActivatedAbilityNode.
  /// </summary>
  T VisitActivatedAbility(ActivatedAbilityNode node);

  /// <summary>
  /// Visit a SpellAbilityNode.
  /// </summary>
  T VisitSpellAbility(SpellAbilityNode node);

  /// <summary>
  /// Visit a KeywordAbilityNode.
  /// </summary>
  T VisitKeywordAbility(KeywordAbilityNode node);

  /// <summary>
  /// Visit a TriggeredAbilityNode.
  /// </summary>
  T VisitTriggeredAbility(TriggeredAbilityNode node);

  /// <summary>
  /// Visit a ManaCostNode.
  /// </summary>
  T VisitManaCost(ManaCostNode node);

  /// <summary>
  /// Visit a TapCostNode.
  /// </summary>
  T VisitTapCost(TapCostNode node);

  /// <summary>
  /// Visit an AddManaEffect.
  /// </summary>
  T VisitAddManaEffect(AddManaEffect node);

  /// <summary>
  /// Visit a DealDamageEffect.
  /// </summary>
  T VisitDealDamageEffect(DealDamageEffect node);

  /// <summary>
  /// Visit a PTModificationEffect.
  /// </summary>
  T VisitPTModificationEffect(PTModificationEffect node);

  /// <summary>
  /// Visit a DrawEffect.
  /// </summary>
  T VisitDrawEffect(DrawEffect node);

  /// <summary>
  /// Visit a GainLifeEffect.
  /// </summary>
  T VisitGainLifeEffect(GainLifeEffect node);

  /// <summary>
  /// Visit a LoseLifeEffect.
  /// </summary>
  T VisitLoseLifeEffect(LoseLifeEffect node);

  /// <summary>
  /// Visit a StaticValue.
  /// </summary>
  T VisitStaticValue(StaticValue node);

  /// <summary>
  /// Visit a VariableValue.
  /// </summary>
  T VisitVariableValue(VariableValue node);

  /// <summary>
  /// Visit a CountExpression.
  /// </summary>
  T VisitCountExpression(CountExpression node);

  /// <summary>
  /// Visit a TypeFilter.
  /// </summary>
  T VisitTypeFilter(TypeFilter node);

  /// <summary>
  /// Visit a ControllerFilter.
  /// </summary>
  T VisitControllerFilter(ControllerFilter node);

  /// <summary>
  /// Visit a TokenFilter.
  /// </summary>
  T VisitTokenFilter(TokenFilter node);

  /// <summary>
  /// Visit a TargetSpec.
  /// </summary>
  T VisitTargetSpec(TargetSpec node);

  /// <summary>
  /// Visit a TriggerEvent.
  /// </summary>
  T VisitTriggerEvent(TriggerEvent node);
}
