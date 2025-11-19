using System.Collections.Immutable;

namespace MagicAST.Core.Diagnostics;

/// <summary>
/// Severity levels for diagnostics.
/// </summary>
public enum DiagnosticSeverity
{
  /// <summary>
  /// Hidden diagnostic (for internal use).
  /// </summary>
  Hidden = 0,

  /// <summary>
  /// Informational message about parse decisions.
  /// </summary>
  Info = 1,

  /// <summary>
  /// Warning about parse approximation or limitation.
  /// AST is valid but may not fully represent the oracle text.
  /// </summary>
  Warning = 2,

  /// <summary>
  /// Error indicating parse failure.
  /// Part of the oracle text could not be represented in the AST.
  /// </summary>
  Error = 3,
}

/// <summary>
/// Immutable descriptor for a diagnostic rule.
/// Defines the template; instances are created via Diagnostic.Create().
/// </summary>
public sealed class DiagnosticDescriptor
{
  /// <summary>
  /// Unique identifier (e.g., "MAST0001", "MAST1042").
  /// Format: MAST{category_digit}{number}
  /// - 0xxx: Parsing errors
  /// - 1xxx: Semantic errors
  /// - 2xxx: Oracle text parsing
  /// - 3xxx: Validation warnings
  /// </summary>
  public string Id { get; }

  /// <summary>
  /// Short title for the diagnostic.
  /// </summary>
  public string Title { get; }

  /// <summary>
  /// Message format with placeholders (e.g., "Unknown mana symbol: {0}").
  /// </summary>
  public string MessageFormat { get; }

  /// <summary>
  /// Category (e.g., "Parsing", "Semantic", "Validation").
  /// </summary>
  public string Category { get; }

  /// <summary>
  /// Default severity level.
  /// </summary>
  public DiagnosticSeverity DefaultSeverity { get; }

  /// <summary>
  /// Whether this diagnostic is enabled by default.
  /// </summary>
  public bool IsEnabledByDefault { get; }

  /// <summary>
  /// Optional tags for filtering/grouping.
  /// </summary>
  public ImmutableArray<string> CustomTags { get; }

  public DiagnosticDescriptor(
    string id,
    string title,
    string messageFormat,
    string category,
    DiagnosticSeverity defaultSeverity,
    bool isEnabledByDefault = true,
    params string[] customTags
  )
  {
    Id = id ?? throw new ArgumentNullException(nameof(id));
    Title = title ?? throw new ArgumentNullException(nameof(title));
    MessageFormat = messageFormat ?? throw new ArgumentNullException(nameof(messageFormat));
    Category = category ?? throw new ArgumentNullException(nameof(category));
    DefaultSeverity = defaultSeverity;
    IsEnabledByDefault = isEnabledByDefault;
    CustomTags = customTags?.ToImmutableArray() ?? ImmutableArray<string>.Empty;
  }
}
