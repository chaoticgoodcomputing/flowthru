namespace MagicAST.Parsing;

using System.Diagnostics;
using MagicAST.AST;
using MagicAST.AST.Abilities;
using MagicAST.Diagnostics;
using MagicAST.Parsing.Parsers;
using MagicAST.Parsing.Tokens;

/// <summary>
/// Main orchestrator for parsing Magic: The Gathering oracle text.
/// Coordinates the tokenizer, clause splitter, classifier, and individual parsers.
/// </summary>
public sealed class OracleParser
{
  private readonly ClauseSplitter _splitter = new();
  private readonly AbilityClassifier _classifier = new();
  private readonly FallbackParser _fallbackParser = new();

  /// <summary>
  /// Parses oracle text into a structured CardOracle AST.
  /// </summary>
  /// <param name="oracleText">The oracle text to parse.</param>
  /// <returns>A ParseResult containing the AST and diagnostics.</returns>
  public ParseResult Parse(string? oracleText)
  {
    var stopwatch = Stopwatch.StartNew();
    var diagnostics = new List<Diagnostic>();

    // Handle null/empty oracle text
    if (string.IsNullOrWhiteSpace(oracleText))
    {
      stopwatch.Stop();
      return new ParseResult
      {
        Output = new CardOracle { RawText = oracleText ?? string.Empty, Abilities = [] },
        Status = ParseStatus.FullyParsed,
        Diagnostics = [],
        Metrics = new ParseMetrics
        {
          TotalAbilities = 0,
          ParsedAbilities = 0,
          FailedAbilities = 0,
          DurationMs = stopwatch.Elapsed.TotalMilliseconds,
        },
      };
    }

    // Split into clauses
    var clauses = _splitter.Split(oracleText);

    // Parse each clause
    var abilities = new List<Ability>();
    var parsedCount = 0;
    var failedCount = 0;

    foreach (var clause in clauses)
    {
      var (ability, clauseDiagnostics) = ParseClause(clause);
      abilities.Add(ability);
      diagnostics.AddRange(clauseDiagnostics);

      if (ability is UnparsedAbility)
      {
        failedCount++;
      }
      else
      {
        parsedCount++;
      }
    }

    stopwatch.Stop();

    // Determine overall status
    var status = DetermineStatus(parsedCount, failedCount);

    return new ParseResult
    {
      Output = new CardOracle { RawText = oracleText, Abilities = abilities },
      Status = status,
      Diagnostics = diagnostics,
      Metrics = new ParseMetrics
      {
        TotalAbilities = clauses.Count,
        ParsedAbilities = parsedCount,
        FailedAbilities = failedCount,
        DurationMs = stopwatch.Elapsed.TotalMilliseconds,
      },
    };
  }

  /// <summary>
  /// Parses a single clause into an ability.
  /// </summary>
  private (Ability Ability, IReadOnlyList<Diagnostic> Diagnostics) ParseClause(OracleClause clause)
  {
    // Classify the clause
    var classification = _classifier.Classify(clause);

    // Route to appropriate parser based on classification
    // For now, all routes go to fallback since we haven't implemented specific parsers
    var ability = classification.Kind switch
    {
      AbilityKind.Triggered => TryParseTriggeredAbility(clause, classification),
      AbilityKind.Activated => TryParseActivatedAbility(clause, classification),
      AbilityKind.Static => TryParseStaticAbility(clause, classification),
      AbilityKind.Modal => TryParseModalAbility(clause, classification),
      AbilityKind.Spell => TryParseSpellAbility(clause, classification),
      _ => _fallbackParser.Parse(clause, classification),
    };

    // Collect diagnostics from UnparsedAbility if present
    var diagnostics = ability is UnparsedAbility unparsed ? unparsed.Diagnostics : [];

    return (ability, diagnostics);
  }

  /// <summary>
  /// Attempts to parse a triggered ability.
  /// Currently delegates to fallback parser.
  /// </summary>
  private Ability TryParseTriggeredAbility(OracleClause clause, ClauseClassification classification)
  {
    // TODO: Implement TriggeredAbilityParser
    // For now, use fallback with informative message
    return _fallbackParser.Parse(
      clause,
      classification,
      "Triggered ability parser not yet implemented"
    );
  }

  /// <summary>
  /// Attempts to parse an activated ability.
  /// Currently delegates to fallback parser.
  /// </summary>
  private Ability TryParseActivatedAbility(OracleClause clause, ClauseClassification classification)
  {
    // TODO: Implement ActivatedAbilityParser
    // For now, use fallback with informative message
    return _fallbackParser.Parse(
      clause,
      classification,
      "Activated ability parser not yet implemented"
    );
  }

  /// <summary>
  /// Attempts to parse a static ability.
  /// Currently delegates to fallback parser.
  /// </summary>
  private Ability TryParseStaticAbility(OracleClause clause, ClauseClassification classification)
  {
    // TODO: Implement StaticAbilityParser
    // For now, use fallback with informative message
    return _fallbackParser.Parse(
      clause,
      classification,
      "Static ability parser not yet implemented"
    );
  }

  /// <summary>
  /// Attempts to parse a modal ability.
  /// Currently delegates to fallback parser.
  /// </summary>
  private Ability TryParseModalAbility(OracleClause clause, ClauseClassification classification)
  {
    // TODO: Implement ModalAbilityParser
    // For now, use fallback with informative message
    return _fallbackParser.Parse(
      clause,
      classification,
      "Modal ability parser not yet implemented"
    );
  }

  /// <summary>
  /// Attempts to parse a spell ability.
  /// Currently delegates to fallback parser.
  /// </summary>
  private Ability TryParseSpellAbility(OracleClause clause, ClauseClassification classification)
  {
    // TODO: Implement SpellAbilityParser
    // For now, use fallback with informative message
    return _fallbackParser.Parse(
      clause,
      classification,
      "Spell ability parser not yet implemented"
    );
  }

  /// <summary>
  /// Determines the overall parse status based on success/failure counts.
  /// </summary>
  private static ParseStatus DetermineStatus(int parsedCount, int failedCount)
  {
    if (failedCount == 0)
    {
      return ParseStatus.FullyParsed;
    }

    if (parsedCount == 0)
    {
      return ParseStatus.Failed;
    }

    return ParseStatus.Partial;
  }
}
