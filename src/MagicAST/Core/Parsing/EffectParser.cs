using MagicAST.Core.AST.Nodes.Effects;
using MagicAST.Core.AST.Nodes.Expressions;
using MagicAST.Core.ManaSystem;
using Superpower;
using Superpower.Parsers;

namespace MagicAST.Core.Parsing;

/// <summary>
/// Parses effect expressions for activated abilities.
/// Phase 2: Handles Add mana, Draw, and PT modification effects.
/// Future phases: Deal damage, destroy, counter, etc.
/// </summary>
public static class EffectParser
{
  /// <summary>
  /// Case-insensitive string parser helper using Span.
  /// </summary>
  private static TextParser<string> Text(string text) =>
    Span.EqualToIgnoreCase(text).Select(span => span.ToStringValue());

  /// <summary>
  /// Parses "Add {C}", "Add {G}", etc.
  /// </summary>
  public static TextParser<AddManaEffect> AddSpecificMana =>
    from add in Text("Add")
    from ws in Character.WhiteSpace.AtLeastOnce()
    from manaString in Character
      .EqualTo('{')
      .IgnoreThen(Character.ExceptIn('}').AtLeastOnce())
      .Then(chars => Character.EqualTo('}').Value(new string(chars.ToArray())))
      .Many()
      .Select(parts => string.Concat(parts.Select(p => "{" + p + "}")))
    from period in Character.EqualTo('.').Optional()
    select new AddManaEffect { ManaToAdd = ParseManaValue(manaString) };

  /// <summary>
  /// Parses "Add one mana of any color"
  /// </summary>
  public static TextParser<AddManaEffect> AddAnyColorMana =>
    from add in Text("Add")
    from ws1 in Character.WhiteSpace.AtLeastOnce()
    from one in Text("one")
    from ws2 in Character.WhiteSpace.AtLeastOnce()
    from mana in Text("mana")
    from ws3 in Character.WhiteSpace.AtLeastOnce()
    from of in Text("of")
    from ws4 in Character.WhiteSpace.AtLeastOnce()
    from any in Text("any")
    from ws5 in Character.WhiteSpace.AtLeastOnce()
    from color in Text("color")
    from period in Character.EqualTo('.').Optional()
    select new AddManaEffect
    {
      ManaToAdd = new ManaValue { Colorless = 1 }, // Using colorless as placeholder for "any"
    };

  /// <summary>
  /// Parses any Add mana effect.
  /// </summary>
  public static TextParser<AddManaEffect> AddMana => AddAnyColorMana.Try().Or(AddSpecificMana);

  /// <summary>
  /// Helper to parse mana value from string like "{G}{G}" or "{C}".
  /// </summary>
  private static ManaValue ParseManaValue(string manaString)
  {
    // Simple parsing - count each symbol
    int white = 0,
      blue = 0,
      black = 0,
      red = 0,
      green = 0,
      colorless = 0;

    foreach (char c in manaString.Replace("{", "").Replace("}", ""))
    {
      switch (c)
      {
        case 'W':
        case 'w':
          white++;
          break;
        case 'U':
        case 'u':
          blue++;
          break;
        case 'B':
        case 'b':
          black++;
          break;
        case 'R':
        case 'r':
          red++;
          break;
        case 'G':
        case 'g':
          green++;
          break;
        case 'C':
        case 'c':
          colorless++;
          break;
        default:
          if (char.IsDigit(c))
          {
            colorless += int.Parse(c.ToString());
          }
          break;
      }
    }

    return new ManaValue
    {
      White = white,
      Blue = blue,
      Black = black,
      Red = red,
      Green = green,
      Colorless = colorless,
    };
  }

  /// <summary>
  /// Parses "Draw a card" or "Draw N cards"
  /// </summary>
  public static TextParser<DrawEffect> Draw =>
    from draw in Text("Draw")
    from ws1 in Character.WhiteSpace.AtLeastOnce()
    from amount in Text("a")
      .Select(_ => 1)
      .Try()
      .Or(Text("two").Select(_ => 2))
      .Try()
      .Or(Text("three").Select(_ => 3))
      .Try()
      .Or(Character.Digit.AtLeastOnce().Select(d => int.Parse(new string(d.ToArray()))))
    from ws2 in Character.WhiteSpace.AtLeastOnce()
    from card in Text("cards").Try().Or(Text("card")) // Try plural first with backtracking
    from period in Character.EqualTo('.').Optional()
    select new DrawEffect
    {
      NumberOfCards = new StaticValue { Value = amount },
      Player = DrawTarget.You,
    };

  /// <summary>
  /// Parses "gets +N/+M until end of turn"
  /// Example: "This creature gets +1/+0 until end of turn"
  /// </summary>
  public static TextParser<PTModificationEffect> GetsPump =>
    from prefix in Text("This creature")
      .Try()
      .Or(Text("This permanent"))
      .Try()
      .Or(Parse.Return(string.Empty))
    from ws1 in Character.WhiteSpace.Many()
    from gets in Text("gets")
    from ws2 in Character.WhiteSpace.AtLeastOnce()
    from sign1 in Character.In('+', '-')
    from power in Character.Digit.AtLeastOnce().Select(d => int.Parse(new string(d.ToArray())))
    from slash in Character.EqualTo('/')
    from sign2 in Character.In('+', '-')
    from toughness in Character.Digit.AtLeastOnce().Select(d => int.Parse(new string(d.ToArray())))
    from ws3 in Character.WhiteSpace.AtLeastOnce()
    from until in Text("until")
    from ws4 in Character.WhiteSpace.AtLeastOnce()
    from end in Text("end")
    from ws5 in Character.WhiteSpace.AtLeastOnce()
    from of in Text("of")
    from ws6 in Character.WhiteSpace.AtLeastOnce()
    from turn in Text("turn")
    from period in Character.EqualTo('.').Optional()
    select new PTModificationEffect
    {
      PowerModification = new StaticValue { Value = sign1 == '+' ? power : -power },
      ToughnessModification = new StaticValue { Value = sign2 == '+' ? toughness : -toughness },
      Duration = Duration.UntilEndOfTurn,
    };

  /// <summary>
  /// Parses "you gain N life"
  /// </summary>
  public static TextParser<GainLifeEffect> GainLife =>
    from you in Text("you")
    from ws1 in Character.WhiteSpace.AtLeastOnce()
    from gain in Text("gain")
    from ws2 in Character.WhiteSpace.AtLeastOnce()
    from amount in Character.Digit.AtLeastOnce().Select(d => int.Parse(new string(d.ToArray())))
    from ws3 in Character.WhiteSpace.AtLeastOnce()
    from life in Text("life")
    from period in Character.EqualTo('.').Optional()
    select new GainLifeEffect
    {
      Amount = new StaticValue { Value = amount },
      Target = LifeTarget.You,
    };

  /// <summary>
  /// Parses "you lose N life", "target player loses N life", or "each opponent loses N life"
  /// </summary>
  public static TextParser<LoseLifeEffect> LoseLife =>
    from target in Text("each opponent")
      .Select(_ => LifeTarget.EachOpponent)
      .Try()
      .Or(Text("target player").Select(_ => LifeTarget.TargetPlayer))
      .Try()
      .Or(Text("you").Select(_ => LifeTarget.You))
    from ws1 in Character.WhiteSpace.AtLeastOnce()
    from lose in Text("loses").Try().Or(Text("lose"))
    from ws2 in Character.WhiteSpace.AtLeastOnce()
    from amount in Character.Digit.AtLeastOnce().Select(d => int.Parse(new string(d.ToArray())))
    from ws3 in Character.WhiteSpace.AtLeastOnce()
    from life in Text("life")
    from period in Character.EqualTo('.').Optional()
    select new LoseLifeEffect
    {
      Amount = new StaticValue { Value = amount },
      Target = target,
    };

  /// <summary>
  /// Parses any effect.
  /// Try all effect parsers with backtracking. GetsPump must use Try() because it has
  /// an optional prefix that can match empty string, causing it to consume input incorrectly.
  /// </summary>
  public static TextParser<EffectNode> AnyEffect =>
    AddMana
      .Select(e => (EffectNode)e)
      .Try()
      .Or(Draw.Select(e => (EffectNode)e))
      .Try()
      .Or(GainLife.Select(e => (EffectNode)e))
      .Try()
      .Or(LoseLife.Select(e => (EffectNode)e))
      .Try()
      .Or(GetsPump.Select(e => (EffectNode)e).Try());
}
