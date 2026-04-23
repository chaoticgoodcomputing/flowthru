using Flowthru.Core.Graph.Meta.Models;
using Flowthru.Core.Meta.Providers;

namespace Flowthru.Tests.Helpers;

/// <summary>
/// Test metadata provider that captures metadata for assertions rather than exporting.
/// Demonstrates the pull pattern complementary to the push pattern of IMetadataProvider.
/// </summary>
public sealed class CapturingMetadataProvider : IMetadataProvider
{
  /// <summary>
  /// Gets the most recently consumed DAG metadata, or null if no metadata has been captured yet.
  /// </summary>
  public DagMetadata? CapturedDag { get; private set; }

  /// <summary>
  /// Gets the metadata provider name.
  /// </summary>
  public string Name => "CapturingMetadataProvider";

  /// <summary>
  /// Captures the DAG metadata for later inspection.
  /// </summary>
  /// <param name="dag">The DAG metadata to capture.</param>
  public void Consume(DagMetadata dag)
  {
    CapturedDag = dag;
  }

  /// <summary>
  /// Resets the captured metadata to null, useful for test cleanup or multi-stage tests.
  /// </summary>
  public void Reset()
  {
    CapturedDag = null;
  }
}
