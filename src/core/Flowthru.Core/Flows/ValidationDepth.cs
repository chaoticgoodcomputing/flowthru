namespace Flowthru.Flows;

/// <summary>
/// Controls how deeply a dry run validates the pipeline before stopping.
/// </summary>
public enum ValidationDepth
{
  /// <summary>
  /// Validates graph structure only: no cycles, all node type contracts satisfied,
  /// all catalog entry dependencies wired, and all validation hooks run.
  /// No data source access.
  /// </summary>
  StructureOnly,

  /// <summary>
  /// Structure validation plus external data presence checks (default dry-run behaviour).
  /// </summary>
  Full,
}
