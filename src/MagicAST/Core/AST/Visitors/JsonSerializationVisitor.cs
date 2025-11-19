using System.Text.Json;
using System.Text.Json.Nodes;
using MagicAST.Core.AST.Nodes;
using MagicAST.Core.AST.Nodes.Abilities;
using MagicAST.Core.AST.Nodes.Costs;
using MagicAST.Core.AST.Nodes.Effects;
using MagicAST.Core.AST.Nodes.Expressions;
using MagicAST.Core.AST.Nodes.Filters;
using MagicAST.Core.AST.Nodes.Targets;
using MagicAST.Core.AST.Nodes.Triggers;
using MagicAST.Core.Keywords;

namespace MagicAST.Core.AST.Visitors;

/// <summary>
/// Visitor that serializes AST nodes to JSON format.
/// Produces structured JSON representation matching the MagicAST specification.
/// </summary>
public class JsonSerializationVisitor : ASTVisitor<JsonNode?>
{
  /// <summary>
  /// Serializes a CardNode to JSON.
  /// </summary>
  public override JsonNode? VisitCard(CardNode node)
  {
    var obj = new JsonObject { ["type"] = "Card", ["name"] = node.Name };

    if (node.ManaCost != null)
    {
      obj["manaCost"] = new JsonObject
      {
        ["costString"] = node.ManaCost.ToString(),
        ["convertedManaCost"] = node.ManaCost.ConvertedManaCost,
      };
    }

    obj["typeLine"] = new JsonObject
    {
      ["supertypes"] = new JsonArray(
        node.TypeLine.Supertypes.Select(s => JsonValue.Create(s.ToString())).ToArray()
      ),
      ["cardTypes"] = new JsonArray(
        node.TypeLine.CardTypes.Select(c => JsonValue.Create(c.ToString())).ToArray()
      ),
      ["subtypes"] = new JsonArray(
        node.TypeLine.Subtypes.Select(s => JsonValue.Create(s)).ToArray()
      ),
    };

    if (node.PowerToughness != null)
    {
      obj["powerToughness"] = new JsonObject
      {
        ["power"] = node.PowerToughness.Power.ToString(),
        ["toughness"] = node.PowerToughness.Toughness.ToString(),
      };
    }

    var abilities = new JsonArray();
    foreach (var ability in node.Abilities)
    {
      var abilityJson = ability.Accept(this);
      if (abilityJson != null)
      {
        abilities.Add(abilityJson);
      }
    }
    obj["abilities"] = abilities;

    // Add diagnostics if present
    if (node.Diagnostics.Count > 0)
    {
      var diagnostics = new JsonArray();
      foreach (var diagnostic in node.Diagnostics)
      {
        var diagObj = new JsonObject
        {
          ["severity"] = diagnostic.Severity.ToString(),
          ["code"] = diagnostic.Code,
          ["message"] = diagnostic.Message,
        };

        if (diagnostic.SourceText != null)
        {
          diagObj["sourceText"] = diagnostic.SourceText;
        }

        diagnostics.Add(diagObj);
      }
      obj["diagnostics"] = diagnostics;
    }

    return obj;
  }

  public override JsonNode? VisitActivatedAbility(ActivatedAbilityNode node)
  {
    var obj = new JsonObject { ["type"] = "ActivatedAbility" };

    if (!string.IsNullOrEmpty(node.AbilityId))
    {
      obj["abilityId"] = node.AbilityId;
    }

    var costs = new JsonArray();
    foreach (var cost in node.Costs)
    {
      var costJson = cost.Accept(this);
      if (costJson != null)
      {
        costs.Add(costJson);
      }
    }
    obj["costs"] = costs;

    var effects = new JsonArray();
    foreach (var effect in node.Effects)
    {
      var effectJson = effect.Accept(this);
      if (effectJson != null)
      {
        effects.Add(effectJson);
      }
    }
    obj["effects"] = effects;

    return obj;
  }

  public override JsonNode? VisitSpellAbility(SpellAbilityNode node)
  {
    var obj = new JsonObject { ["type"] = "SpellAbility" };

    if (!string.IsNullOrEmpty(node.AbilityId))
    {
      obj["abilityId"] = node.AbilityId;
    }

    var targets = new JsonArray();
    foreach (var target in node.Targets)
    {
      var targetJson = target.Accept(this);
      if (targetJson != null)
      {
        targets.Add(targetJson);
      }
    }
    obj["targets"] = targets;

    var effects = new JsonArray();
    foreach (var effect in node.Effects)
    {
      var effectJson = effect.Accept(this);
      if (effectJson != null)
      {
        effects.Add(effectJson);
      }
    }
    obj["effects"] = effects;

    return obj;
  }

  public override JsonNode? VisitKeywordAbility(KeywordAbilityNode node)
  {
    var obj = new JsonObject { ["type"] = "KeywordAbility", ["keyword"] = node.Keyword.ToString() };

    if (!string.IsNullOrEmpty(node.ReminderText))
    {
      obj["reminderText"] = node.ReminderText;
    }

    return obj;
  }

  public override JsonNode? VisitTriggeredAbility(TriggeredAbilityNode node)
  {
    var obj = new JsonObject { ["type"] = "TriggeredAbility" };

    if (!string.IsNullOrEmpty(node.AbilityId))
    {
      obj["abilityId"] = node.AbilityId;
    }

    var triggerJson = node.Trigger.Accept(this);
    if (triggerJson != null)
    {
      obj["trigger"] = triggerJson;
    }

    var effects = new JsonArray();
    foreach (var effect in node.Effects)
    {
      var effectJson = effect.Accept(this);
      if (effectJson != null)
      {
        effects.Add(effectJson);
      }
    }
    obj["effects"] = effects;

    return obj;
  }

  public override JsonNode? VisitManaCost(ManaCostNode node)
  {
    return new JsonObject { ["type"] = "ManaCost", ["cost"] = node.Cost.ToString() };
  }

  public override JsonNode? VisitTapCost(TapCostNode node)
  {
    return new JsonObject { ["type"] = "TapCost" };
  }

  public override JsonNode? VisitAddManaEffect(AddManaEffect node)
  {
    return new JsonObject { ["type"] = "AddMana", ["mana"] = node.ManaToAdd.ToString() };
  }

  public override JsonNode? VisitDealDamageEffect(DealDamageEffect node)
  {
    var obj = new JsonObject
    {
      ["type"] = "DealDamage",
      ["source"] = node.Source.ToString(),
      ["target"] = node.Target.ToString(),
    };

    var amountJson = node.Amount.Accept(this);
    if (amountJson != null)
    {
      obj["amount"] = amountJson;
    }

    return obj;
  }

  public override JsonNode? VisitPTModificationEffect(PTModificationEffect node)
  {
    var obj = new JsonObject
    {
      ["type"] = "PTModification",
      ["duration"] = node.Duration.ToString(),
    };

    if (node.AffectedCreatures != null)
    {
      var filterJson = node.AffectedCreatures.Accept(this);
      if (filterJson != null)
      {
        obj["affectedCreatures"] = filterJson;
      }
    }

    var powerJson = node.PowerModification.Accept(this);
    if (powerJson != null)
    {
      obj["powerModification"] = powerJson;
    }

    var toughnessJson = node.ToughnessModification.Accept(this);
    if (toughnessJson != null)
    {
      obj["toughnessModification"] = toughnessJson;
    }

    return obj;
  }

  public override JsonNode? VisitDrawEffect(DrawEffect node)
  {
    var obj = new JsonObject { ["type"] = "Draw", ["player"] = node.Player.ToString() };

    var countJson = node.NumberOfCards.Accept(this);
    if (countJson != null)
    {
      obj["numberOfCards"] = countJson;
    }

    return obj;
  }

  public override JsonNode? VisitStaticValue(StaticValue node)
  {
    return new JsonObject { ["type"] = "StaticValue", ["value"] = node.Value };
  }

  public override JsonNode? VisitVariableValue(VariableValue node)
  {
    return new JsonObject
    {
      ["type"] = "VariableValue",
      ["variable"] = node.VariableName,
      ["isNegative"] = node.IsNegative,
    };
  }

  public override JsonNode? VisitCountExpression(CountExpression node)
  {
    var obj = new JsonObject
    {
      ["type"] = "CountExpression",
      ["countType"] = node.Type.ToString(),
      ["multiplier"] = node.Multiplier,
    };

    if (node.Filter != null)
    {
      var filterJson = node.Filter.Accept(this);
      if (filterJson != null)
      {
        obj["filter"] = filterJson;
      }
    }

    return obj;
  }

  public override JsonNode? VisitTypeFilter(TypeFilter node)
  {
    return new JsonObject
    {
      ["type"] = "TypeFilter",
      ["cardTypes"] = new JsonArray(
        node.CardTypes.Select(t => JsonValue.Create(t.ToString())).ToArray()
      ),
      ["subtypes"] = new JsonArray(node.Subtypes.Select(s => JsonValue.Create(s)).ToArray()),
    };
  }

  public override JsonNode? VisitControllerFilter(ControllerFilter node)
  {
    return new JsonObject
    {
      ["type"] = "ControllerFilter",
      ["controller"] = node.ControllerType.ToString(),
    };
  }

  public override JsonNode? VisitTokenFilter(TokenFilter node)
  {
    return new JsonObject { ["type"] = "TokenFilter", ["tokenType"] = node.TokenType.ToString() };
  }

  public override JsonNode? VisitTargetSpec(TargetSpec node)
  {
    var obj = new JsonObject { ["type"] = "TargetSpec", ["targetType"] = node.Type.ToString() };

    if (node.Filter != null)
    {
      var filterJson = node.Filter.Accept(this);
      if (filterJson != null)
      {
        obj["filter"] = filterJson;
      }
    }

    return obj;
  }

  public override JsonNode? VisitTriggerEvent(TriggerEvent node)
  {
    var obj = new JsonObject { ["type"] = "TriggerEvent", ["eventType"] = node.Type.ToString() };

    if (node.Filter != null)
    {
      var filterJson = node.Filter.Accept(this);
      if (filterJson != null)
      {
        obj["filter"] = filterJson;
      }
    }

    return obj;
  }

  /// <summary>
  /// Serializes a CardNode to a formatted JSON string.
  /// </summary>
  public static string ToJson(CardNode card, bool indented = true)
  {
    var visitor = new JsonSerializationVisitor();
    var json = card.Accept(visitor);

    var options = new JsonSerializerOptions { WriteIndented = indented };

    return json?.ToJsonString(options) ?? "{}";
  }
}
