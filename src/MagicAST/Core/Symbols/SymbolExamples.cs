namespace MagicAST.Core.Symbols;

/// <summary>
/// Demonstrates usage of the Symbol parsing system.
/// </summary>
public static class SymbolExamples
{
  /// <summary>
  /// Example: Parse mana cost symbols.
  /// </summary>
  public static void ParseManaCost()
  {
    var manaCost = "{2}{W}{U}";
    var symbols = SymbolParser.ParseSymbols(manaCost);

    Console.WriteLine($"Mana cost: {manaCost}");
    Console.WriteLine("Symbols:");
    foreach (var symbol in symbols)
    {
      if (symbol is ManaSymbol mana)
      {
        Console.WriteLine($"  {symbol.SymbolText} - {symbol.EnglishDescription}");
        Console.WriteLine($"    Category: {symbol.Category}");
        Console.WriteLine($"    Mana Value: {mana.Amount}");
        Console.WriteLine($"    Is Hybrid: {mana.IsHybrid}");
        Console.WriteLine($"    Is Phyrexian: {mana.IsPhyrexian}");
      }
    }
    // Output:
    // Mana cost: {2}{W}{U}
    // Symbols:
    //   {2} - two generic mana
    //     Category: Mana
    //     Mana Value: 2
    //     Is Hybrid: False
    //     Is Phyrexian: False
    //   {W} - one white mana
    //     Category: Mana
    //     Mana Value: 1
    //     Is Hybrid: False
    //     Is Phyrexian: False
    //   {U} - one blue mana
    //     Category: Mana
    //     Mana Value: 1
    //     Is Hybrid: False
    //     Is Phyrexian: False
  }

  /// <summary>
  /// Example: Parse hybrid mana symbols.
  /// </summary>
  public static void ParseHybridMana()
  {
    var manaCost = "{2}{W/U}{B/G/P}";
    var symbols = SymbolParser.ParseSymbols(manaCost);

    Console.WriteLine($"Mana cost: {manaCost}");
    var totalCMC = symbols.OfType<ManaSymbol>().Sum(s => s.Amount);
    Console.WriteLine($"Total CMC: {totalCMC}");

    foreach (var symbol in symbols)
    {
      Console.WriteLine($"  {symbol.SymbolText}: {symbol.EnglishDescription}");
    }
    // Output:
    // Mana cost: {2}{W/U}{B/G/P}
    // Total CMC: 4
    //   {2}: two generic mana
    //   {W/U}: one white or blue mana
    //   {B/G/P}: one black mana, one green mana, or 2 life
  }

  /// <summary>
  /// Example: Parse oracle text with ability symbols.
  /// </summary>
  public static void ParseOracleText()
  {
    var oracleText =
      "{T}: Add {C}{C}{C}.\n{T}, Pay {2} and 3 life: Draw a card. ({E} is an energy counter)";
    var symbols = SymbolParser.ParseSymbols(oracleText);

    Console.WriteLine("Oracle text symbols:");
    foreach (var symbol in symbols)
    {
      Console.WriteLine($"  {symbol.SymbolText} ({symbol.Category}): {symbol.EnglishDescription}");
    }
    // Output:
    // Oracle text symbols:
    //   {T} (TapUntap): tap this permanent
    //   {C} (Mana): one colorless mana
    //   {C} (Mana): one colorless mana
    //   {C} (Mana): one colorless mana
    //   {T} (TapUntap): tap this permanent
    //   {2} (Mana): two generic mana
    //   {E} (Counter): an energy counter
  }

  /// <summary>
  /// Example: Replace symbols with English text.
  /// </summary>
  public static void ReplaceSymbolsWithEnglish()
  {
    var oracleText = "{T}: Add {W}{U}. You gain 2 life.";
    var english = SymbolParser.ReplaceSymbolsWithEnglish(oracleText);

    Console.WriteLine($"Original: {oracleText}");
    Console.WriteLine($"English:  {english}");
    // Output:
    // Original: {T}: Add {W}{U}. You gain 2 life.
    // English:  tap this permanent: Add one white manaone blue mana. You gain 2 life.
  }

  /// <summary>
  /// Example: Filter symbols by category.
  /// </summary>
  public static void FilterSymbolsByCategory()
  {
    var text = "{2}{W/U}, {T}, Sacrifice a creature: Draw a card. You get {E}{E}.";
    var symbols = SymbolParser.ParseSymbols(text);

    var manaSymbols = symbols.Where(s => s.Category == SymbolCategory.Mana).ToList();
    var abilitySymbols = symbols.Where(s => s.Category == SymbolCategory.TapUntap).ToList();
    var counterSymbols = symbols.Where(s => s.Category == SymbolCategory.Counter).ToList();

    Console.WriteLine($"Mana symbols: {string.Join(", ", manaSymbols.Select(s => s.SymbolText))}");
    Console.WriteLine(
      $"Ability symbols: {string.Join(", ", abilitySymbols.Select(s => s.SymbolText))}"
    );
    Console.WriteLine(
      $"Counter symbols: {string.Join(", ", counterSymbols.Select(s => s.SymbolText))}"
    );
    // Output:
    // Mana symbols: {2}, {W/U}
    // Ability symbols: {T}
    // Counter symbols: {E}, {E}
  }

  /// <summary>
  /// Example: Calculate total mana value from symbols.
  /// </summary>
  public static void CalculateManaValue()
  {
    var manaCostExamples = new[] { "{1}{W}", "{X}{R}{R}", "{2/W}{2/U}{2/B}", "{B/G/P}{B/G/P}" };

    foreach (var manaCost in manaCostExamples)
    {
      var symbols = SymbolParser.ParseSymbols(manaCost);
      var manaSymbols = symbols.OfType<ManaSymbol>().ToList();
      var cmc = manaSymbols.Sum(s => s.Amount);
      var hasX = manaSymbols.Any(s => s.SymbolType == ManaSymbolType.X);

      Console.WriteLine($"{manaCost} => CMC: {cmc}{(hasX ? " (plus X)" : "")}");
    }
    // Output:
    // {1}{W} => CMC: 2
    // {X}{R}{R} => CMC: 2 (plus X)
    // {2/W}{2/U}{2/B} => CMC: 6
    // {B/G/P}{B/G/P} => CMC: 2
  }
}
