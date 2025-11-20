using MagicAST.Core.AST.Nodes;
using MagicAST.Core.AST.Nodes.Abilities;
using MagicAST.Core.AST.Nodes.Costs;
using MagicAST.Core.AST.Nodes.Effects;
using MagicAST.Core.AST.Nodes.Expressions;
using MagicAST.Core.AST.Nodes.Filters;
using MagicAST.Core.AST.Nodes.Targets;
using MagicAST.Core.AST.Nodes.Triggers;
using MagicAST.DTOs;

namespace MagicAST.Core.AST.Visitors;

/// <summary>
/// Visitor that converts AST nodes directly to AstNodeDto for serialization.
/// Produces DTOs that can be serialized with System.Text.Json without custom converters.
/// </summary>
public class DtoSerializationVisitor : ASTVisitor<AstNodeDto>
{
  public override AstNodeDto VisitCard(CardNode node)
  {
    var properties = new Dictionary<string, object?> { ["name"] = node.Name };

    // Add mana cost if present
    if (node.ManaCost != null)
    {
      properties["manaCostString"] = node.ManaCost.ToString();
      properties["convertedManaCost"] = node.ManaCost.ConvertedManaCost;
    }

    // Add type line
    properties["supertypes"] = node.TypeLine.Supertypes.Select(s => s.ToString()).ToList();
    properties["cardTypes"] = node.TypeLine.CardTypes.Select(c => c.ToString()).ToList();
    properties["subtypes"] = node.TypeLine.Subtypes.ToList();

    // Add power/toughness if present
    if (node.PowerToughness != null)
    {
      properties["power"] = node.PowerToughness.Power.ToString();
      properties["toughness"] = node.PowerToughness.Toughness.ToString();
    }

    // Recursively convert child abilities
    var children = node.Abilities.Select(ability => ability.Accept(this)).ToList();

    return new AstNodeDto
    {
      NodeType = "Card",
      Properties = properties,
      Children = children,
    };
  }

  public override AstNodeDto VisitActivatedAbility(ActivatedAbilityNode node)
  {
    var properties = new Dictionary<string, object?>();

    if (!string.IsNullOrEmpty(node.AbilityId))
    {
      properties["abilityId"] = node.AbilityId;
    }

    var children = new List<AstNodeDto>();

    // Add costs as children
    children.AddRange(node.Costs.Select(cost => cost.Accept(this)));

    // Add effects as children
    children.AddRange(node.Effects.Select(effect => effect.Accept(this)));

    return new AstNodeDto
    {
      NodeType = "ActivatedAbility",
      Properties = properties,
      Children = children,
    };
  }

  public override AstNodeDto VisitSpellAbility(SpellAbilityNode node)
  {
    var properties = new Dictionary<string, object?>();

    if (!string.IsNullOrEmpty(node.AbilityId))
    {
      properties["abilityId"] = node.AbilityId;
    }

    var children = new List<AstNodeDto>();

    // Add targets as children
    children.AddRange(node.Targets.Select(target => target.Accept(this)));

    // Add effects as children
    children.AddRange(node.Effects.Select(effect => effect.Accept(this)));

    return new AstNodeDto
    {
      NodeType = "SpellAbility",
      Properties = properties,
      Children = children,
    };
  }

  public override AstNodeDto VisitKeywordAbility(KeywordAbilityNode node)
  {
    var properties = new Dictionary<string, object?> { ["keyword"] = node.Keyword.ToString() };

    if (!string.IsNullOrEmpty(node.ReminderText))
    {
      properties["reminderText"] = node.ReminderText;
    }

    return new AstNodeDto
    {
      NodeType = "KeywordAbility",
      Properties = properties,
      Children = new(),
    };
  }

  public override AstNodeDto VisitTriggeredAbility(TriggeredAbilityNode node)
  {
    var properties = new Dictionary<string, object?>();

    if (!string.IsNullOrEmpty(node.AbilityId))
    {
      properties["abilityId"] = node.AbilityId;
    }

    var children = new List<AstNodeDto>();

    // Add trigger as child
    children.Add(node.Trigger.Accept(this));

    // Add effects as children
    children.AddRange(node.Effects.Select(effect => effect.Accept(this)));

    return new AstNodeDto
    {
      NodeType = "TriggeredAbility",
      Properties = properties,
      Children = children,
    };
  }

  // Cost nodes
  public override AstNodeDto VisitManaCost(ManaCostNode node)
  {
    return new AstNodeDto
    {
      NodeType = "ManaCost",
      Properties = new Dictionary<string, object?> { ["cost"] = node.Cost.ToString() },
      Children = new(),
    };
  }

  public override AstNodeDto VisitTapCost(TapCostNode node)
  {
    return new AstNodeDto
    {
      NodeType = "TapCost",
      Properties = new(),
      Children = new(),
    };
  }

  // Effect nodes
  public override AstNodeDto VisitAddManaEffect(AddManaEffect node)
  {
    return new AstNodeDto
    {
      NodeType = "AddMana",
      Properties = new Dictionary<string, object?> { ["mana"] = node.ManaToAdd.ToString() },
      Children = new(),
    };
  }

  public override AstNodeDto VisitDealDamageEffect(DealDamageEffect node)
  {
    var properties = new Dictionary<string, object?>
    {
      ["source"] = node.Source.ToString(),
      ["target"] = node.Target.ToString(),
    };

    var children = new List<AstNodeDto> { node.Amount.Accept(this) };

    return new AstNodeDto
    {
      NodeType = "DealDamage",
      Properties = properties,
      Children = children,
    };
  }

  public override AstNodeDto VisitPTModificationEffect(PTModificationEffect node)
  {
    var properties = new Dictionary<string, object?> { ["duration"] = node.Duration.ToString() };

    var children = new List<AstNodeDto>();

    if (node.AffectedCreatures != null)
    {
      children.Add(node.AffectedCreatures.Accept(this));
    }

    children.Add(node.PowerModification.Accept(this));
    children.Add(node.ToughnessModification.Accept(this));

    return new AstNodeDto
    {
      NodeType = "PTModification",
      Properties = properties,
      Children = children,
    };
  }

  public override AstNodeDto VisitDrawEffect(DrawEffect node)
  {
    var properties = new Dictionary<string, object?> { ["player"] = node.Player.ToString() };

    var children = new List<AstNodeDto> { node.NumberOfCards.Accept(this) };

    return new AstNodeDto
    {
      NodeType = "Draw",
      Properties = properties,
      Children = children,
    };
  }

  public override AstNodeDto VisitGainLifeEffect(GainLifeEffect node)
  {
    var properties = new Dictionary<string, object?> { ["target"] = node.Target.ToString() };

    var children = new List<AstNodeDto> { node.Amount.Accept(this) };

    return new AstNodeDto
    {
      NodeType = "GainLife",
      Properties = properties,
      Children = children,
    };
  }

  public override AstNodeDto VisitLoseLifeEffect(LoseLifeEffect node)
  {
    var properties = new Dictionary<string, object?> { ["target"] = node.Target.ToString() };

    var children = new List<AstNodeDto> { node.Amount.Accept(this) };

    return new AstNodeDto
    {
      NodeType = "LoseLife",
      Properties = properties,
      Children = children,
    };
  }

  // Expression nodes
  public override AstNodeDto VisitStaticValue(StaticValue node)
  {
    return new AstNodeDto
    {
      NodeType = "StaticValue",
      Properties = new Dictionary<string, object?> { ["value"] = node.Value },
      Children = new(),
    };
  }

  public override AstNodeDto VisitVariableValue(VariableValue node)
  {
    return new AstNodeDto
    {
      NodeType = "VariableValue",
      Properties = new Dictionary<string, object?>
      {
        ["variable"] = node.VariableName,
        ["isNegative"] = node.IsNegative,
      },
      Children = new(),
    };
  }

  public override AstNodeDto VisitCountExpression(CountExpression node)
  {
    var properties = new Dictionary<string, object?>
    {
      ["countType"] = node.Type.ToString(),
      ["multiplier"] = node.Multiplier,
    };

    var children = new List<AstNodeDto>();

    if (node.Filter != null)
    {
      children.Add(node.Filter.Accept(this));
    }

    return new AstNodeDto
    {
      NodeType = "CountExpression",
      Properties = properties,
      Children = children,
    };
  }

  // Filter nodes
  public override AstNodeDto VisitTypeFilter(TypeFilter node)
  {
    return new AstNodeDto
    {
      NodeType = "TypeFilter",
      Properties = new Dictionary<string, object?>
      {
        ["cardTypes"] = node.CardTypes.Select(t => t.ToString()).ToList(),
        ["subtypes"] = node.Subtypes.ToList(),
      },
      Children = new(),
    };
  }

  public override AstNodeDto VisitControllerFilter(ControllerFilter node)
  {
    return new AstNodeDto
    {
      NodeType = "ControllerFilter",
      Properties = new Dictionary<string, object?>
      {
        ["controller"] = node.ControllerType.ToString(),
      },
      Children = new(),
    };
  }

  public override AstNodeDto VisitTokenFilter(TokenFilter node)
  {
    return new AstNodeDto
    {
      NodeType = "TokenFilter",
      Properties = new Dictionary<string, object?> { ["tokenType"] = node.TokenType.ToString() },
      Children = new(),
    };
  }

  // Target and trigger nodes
  public override AstNodeDto VisitTargetSpec(TargetSpec node)
  {
    var properties = new Dictionary<string, object?> { ["targetType"] = node.Type.ToString() };

    var children = new List<AstNodeDto>();

    if (node.Filter != null)
    {
      children.Add(node.Filter.Accept(this));
    }

    return new AstNodeDto
    {
      NodeType = "TargetSpec",
      Properties = properties,
      Children = children,
    };
  }

  public override AstNodeDto VisitTriggerEvent(TriggerEvent node)
  {
    var properties = new Dictionary<string, object?> { ["eventType"] = node.Type.ToString() };

    var children = new List<AstNodeDto>();

    if (node.Filter != null)
    {
      children.Add(node.Filter.Accept(this));
    }

    return new AstNodeDto
    {
      NodeType = "TriggerEvent",
      Properties = properties,
      Children = children,
    };
  }
}
