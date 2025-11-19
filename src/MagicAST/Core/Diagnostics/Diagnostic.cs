using System.Collections.Immutable;

namespace MagicAST.Core.Diagnostics;

/// <summary>
/// Represents an actual diagnostic instance.
/// Combines a descriptor with location and arguments.
/// </summary>
public sealed class Diagnostic
{
  /// <summary>
  /// The diagnostic descriptor (rule definition).
  /// </summary>
  public DiagnosticDescriptor Descriptor { get; }

  /// <summary>
  /// Severity (can override descriptor's default).
  /// </summary>
  public DiagnosticSeverity Severity { get; }

  /// <summary>
  /// Location in source where diagnostic occurred.
  /// </summary>
  public Location Location { get; }

  /// <summary>
  /// Additional locations relevant to this diagnostic.
  /// </summary>
  public ImmutableArray<Location> AdditionalLocations { get; }

  /// <summary>
  /// Format arguments for the message.
  /// </summary>
  private readonly object?[] _messageArgs;

  /// <summary>
  /// Custom properties for extensibility.
  /// </summary>
  public ImmutableDictionary<string, string?> Properties { get; }

  private Diagnostic(
    DiagnosticDescriptor descriptor,
    Location location,
    DiagnosticSeverity? severity,
    ImmutableArray<Location>? additionalLocations,
    ImmutableDictionary<string, string?>? properties,
    object?[] messageArgs
  )
  {
    Descriptor = descriptor ?? throw new ArgumentNullException(nameof(descriptor));
    Location = location ?? Diagnostics.Location.None;
    Severity = severity ?? descriptor.DefaultSeverity;
    AdditionalLocations = additionalLocations ?? ImmutableArray<Location>.Empty;
    Properties = properties ?? ImmutableDictionary<string, string?>.Empty;
    _messageArgs = messageArgs ?? Array.Empty<object?>();
  }

  /// <summary>
  /// Gets the formatted message.
  /// </summary>
  public string GetMessage()
  {
    if (_messageArgs.Length == 0)
    {
      return Descriptor.MessageFormat;
    }

    return string.Format(Descriptor.MessageFormat, _messageArgs);
  }

  /// <summary>
  /// Diagnostic ID (from descriptor).
  /// </summary>
  public string Id => Descriptor.Id;

  /// <summary>
  /// Creates a diagnostic instance.
  /// </summary>
  public static Diagnostic Create(
    DiagnosticDescriptor descriptor,
    Location? location,
    params object?[] messageArgs
  )
  {
    return new Diagnostic(
      descriptor,
      location ?? Diagnostics.Location.None,
      null,
      null,
      null,
      messageArgs
    );
  }

  /// <summary>
  /// Creates a diagnostic with custom severity.
  /// </summary>
  public static Diagnostic Create(
    DiagnosticDescriptor descriptor,
    Location? location,
    DiagnosticSeverity severity,
    params object?[] messageArgs
  )
  {
    return new Diagnostic(
      descriptor,
      location ?? Diagnostics.Location.None,
      severity,
      null,
      null,
      messageArgs
    );
  }

  /// <summary>
  /// Creates a diagnostic with additional locations and properties.
  /// </summary>
  public static Diagnostic Create(
    DiagnosticDescriptor descriptor,
    Location? location,
    DiagnosticSeverity? severity,
    ImmutableArray<Location>? additionalLocations,
    ImmutableDictionary<string, string?>? properties,
    params object?[] messageArgs
  )
  {
    return new Diagnostic(
      descriptor,
      location ?? Diagnostics.Location.None,
      severity,
      additionalLocations,
      properties,
      messageArgs
    );
  }

  public override string ToString()
  {
    var severityText = Severity.ToString().ToLowerInvariant();
    var location = Location.Kind != LocationKind.None ? $"{Location}: " : "";
    return $"{location}{severityText} {Id}: {GetMessage()}";
  }
}
