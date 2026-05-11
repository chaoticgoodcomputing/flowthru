namespace Flowthru.Flow;

/// <summary>
/// Whether to skip transform execution and only validate the graph
/// shape and reachability of inputs/outputs. Used for CI smoke
/// checks on a flow before doing expensive work.
/// </summary>
public enum DryRunOption
{
  /// <summary>Execute the flow normally (default).</summary>
  Off,

  /// <summary>
  /// Validate the graph and inputs but do not run any step's
  /// <c>Transform</c>. Outputs are not written.
  /// </summary>
  On,
}
