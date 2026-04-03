namespace Flowthru.Flows;

/// <summary>
/// Represents a dry-run configuration. Can be assigned from a <see cref="bool"/>
/// or a <see cref="ValidationDepth"/> value.
/// </summary>
/// <remarks>
/// Assigning <c>true</c> enables a full dry run (all pre-flight checks, no execution).
/// Assigning <c>false</c> disables dry-run mode entirely.
/// Assigning a <see cref="ValidationDepth"/> enables dry-run at the specified depth.
/// </remarks>
public readonly struct DryRunOption
{
  /// <summary>
  /// Whether dry-run mode is enabled.
  /// </summary>
  public bool Enabled { get; }

  /// <summary>
  /// The validation depth applied when dry-run is enabled.
  /// </summary>
  public ValidationDepth Depth { get; }

  private DryRunOption(bool enabled, ValidationDepth depth)
  {
    Enabled = enabled;
    Depth = depth;
  }

  /// <summary>
  /// Implicitly converts a <see cref="bool"/> to a <see cref="DryRunOption"/>.
  /// <c>true</c> enables full dry-run; <c>false</c> disables it.
  /// </summary>
  public static implicit operator DryRunOption(bool value) => new(value, ValidationDepth.Full);

  /// <summary>
  /// Implicitly converts a <see cref="ValidationDepth"/> to a <see cref="DryRunOption"/>,
  /// enabling dry-run at the specified depth.
  /// </summary>
  public static implicit operator DryRunOption(ValidationDepth depth) => new(true, depth);
}
