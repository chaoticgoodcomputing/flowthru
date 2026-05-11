namespace Flowthru.Data.Storage;

/// <summary>
/// Depth at which an external input is inspected during pre-flight. Higher
/// levels increase work and increase the chance of catching subtle problems
/// before any pipeline logic runs.
/// </summary>
public enum InspectionLevel
{
  /// <summary>Skip inspection entirely.</summary>
  None = 0,

  /// <summary>
  /// Existence + format + headers + first N rows. Default for raw inputs;
  /// minimal overhead.
  /// </summary>
  Shallow = 1,

  /// <summary>
  /// Shallow checks plus every row deserializes successfully. Potentially
  /// significant overhead; opt-in by the catalog author for critical data.
  /// </summary>
  Deep = 2,

  /// <summary>
  /// Reachability check on a write target — "can this output land here?"
  /// Used for produced items rather than consumed ones.
  /// </summary>
  Target = 3,
}
