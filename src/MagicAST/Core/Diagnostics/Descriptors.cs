namespace MagicAST.Core.Diagnostics;

/// <summary>
/// Centralized registry of all diagnostic descriptors.
/// Provides discoverability and documentation.
/// </summary>
public static class Descriptors
{
  // ============================================================================
  // Parsing errors (MAST0xxx)
  // ============================================================================

  public static readonly DiagnosticDescriptor InvalidManaCost =
    new(
      id: "MAST0001",
      title: "Invalid mana cost",
      messageFormat: "Failed to parse mana cost: {0}",
      category: "Parsing",
      defaultSeverity: DiagnosticSeverity.Error
    );

  public static readonly DiagnosticDescriptor UnknownManaSymbol =
    new(
      id: "MAST0002",
      title: "Unknown mana symbol",
      messageFormat: "Unknown mana symbol: '{0}'",
      category: "Parsing",
      defaultSeverity: DiagnosticSeverity.Error
    );

  public static readonly DiagnosticDescriptor InvalidTypeLine =
    new(
      id: "MAST0003",
      title: "Invalid type line",
      messageFormat: "Failed to parse type line: {0}",
      category: "Parsing",
      defaultSeverity: DiagnosticSeverity.Error
    );

  public static readonly DiagnosticDescriptor InvalidPowerToughness =
    new(
      id: "MAST0004",
      title: "Invalid power/toughness",
      messageFormat: "Failed to parse power/toughness: {0}",
      category: "Parsing",
      defaultSeverity: DiagnosticSeverity.Warning
    );

  public static readonly DiagnosticDescriptor TokenizationFailed =
    new(
      id: "MAST0010",
      title: "Tokenization failed",
      messageFormat: "Could not tokenize oracle text: {0}",
      category: "Parsing",
      defaultSeverity: DiagnosticSeverity.Error,
      customTags: "Superpower"
    );

  public static readonly DiagnosticDescriptor UnexpectedToken =
    new(
      id: "MAST0011",
      title: "Unexpected token",
      messageFormat: "Unexpected {0}{1}",
      category: "Parsing",
      defaultSeverity: DiagnosticSeverity.Error,
      customTags: "Superpower"
    );

  // ============================================================================
  // Semantic errors (MAST1xxx)
  // ============================================================================

  public static readonly DiagnosticDescriptor UnresolvedPronoun =
    new(
      id: "MAST1001",
      title: "Unresolved pronoun",
      messageFormat: "Could not resolve pronoun '{0}' - no appropriate antecedent found",
      category: "Semantic",
      defaultSeverity: DiagnosticSeverity.Warning
    );

  public static readonly DiagnosticDescriptor UnboundVariable =
    new(
      id: "MAST1002",
      title: "Unbound variable",
      messageFormat: "Variable '{0}' is used but never defined in cost",
      category: "Semantic",
      defaultSeverity: DiagnosticSeverity.Error
    );

  public static readonly DiagnosticDescriptor InvalidTargetFilter =
    new(
      id: "MAST1003",
      title: "Invalid target filter",
      messageFormat: "Target filter '{0}' cannot apply to object type '{1}'",
      category: "Semantic",
      defaultSeverity: DiagnosticSeverity.Warning
    );

  // ============================================================================
  // Oracle text parsing (MAST2xxx)
  // ============================================================================

  public static readonly DiagnosticDescriptor OracleTextNotImplemented =
    new(
      id: "MAST2999",
      title: "Oracle text parsing not implemented",
      messageFormat: "Oracle text parsing not yet implemented - abilities list will be empty",
      category: "Parsing",
      defaultSeverity: DiagnosticSeverity.Error
    );

  public static readonly DiagnosticDescriptor UnknownAbilityPattern =
    new(
      id: "MAST2001",
      title: "Unknown ability pattern",
      messageFormat: "Could not parse ability: {0}",
      category: "Parsing",
      defaultSeverity: DiagnosticSeverity.Warning
    );

  public static readonly DiagnosticDescriptor UnsupportedKeyword =
    new(
      id: "MAST2002",
      title: "Unsupported keyword",
      messageFormat: "Keyword '{0}' is not yet supported",
      category: "Parsing",
      defaultSeverity: DiagnosticSeverity.Warning
    );

  public static readonly DiagnosticDescriptor ComplexAbilityApproximated =
    new(
      id: "MAST2003",
      title: "Complex ability approximated",
      messageFormat: "Ability '{0}' was simplified for AST representation",
      category: "Parsing",
      defaultSeverity: DiagnosticSeverity.Info
    );

  // ============================================================================
  // Validation warnings (MAST3xxx)
  // ============================================================================

  public static readonly DiagnosticDescriptor InvalidCardStructure =
    new(
      id: "MAST3001",
      title: "Invalid card structure",
      messageFormat: "{0}",
      category: "Validation",
      defaultSeverity: DiagnosticSeverity.Warning
    );

  public static readonly DiagnosticDescriptor MissingRequiredProperty =
    new(
      id: "MAST3002",
      title: "Missing required property",
      messageFormat: "Card is missing required property: {0}",
      category: "Validation",
      defaultSeverity: DiagnosticSeverity.Warning
    );
}
