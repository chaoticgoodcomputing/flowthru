namespace Flowthru.Diagnostics.Mermaid;

/// <summary>
/// Controls whether the Mermaid provider emits a per-flow companion
/// diagram (one Mermaid block per Flow, with neighboring Flows
/// collapsed to their boundary Items/Steps) alongside the merged DAG.
/// Per-flow output is always additive — the merged file is written
/// regardless of this setting.
/// </summary>
public enum PerFlowMode
{
  /// <summary>
  /// Emit the per-flow file only when the merged DAG contains more
  /// distinct flows than the auto threshold (default: 4 — i.e.
  /// pipelines with more than 3 flows). Small pipelines skip the
  /// extra file.
  /// </summary>
  Auto = 0,

  /// <summary>
  /// Always emit the per-flow file in addition to the merged file,
  /// regardless of flow count. A single-flow pipeline produces a
  /// per-flow file with one block and no collapsed neighbors.
  /// </summary>
  Enabled,

  /// <summary>
  /// Never emit the per-flow file; only the merged view is written.
  /// </summary>
  Disabled,
}
