using Flowthru.Meta.Models;
using Microsoft.Extensions.Logging;

namespace Flowthru.Meta.Providers;

/// <summary>
/// Interface for metadata export providers.
/// </summary>
/// <remarks>
/// Metadata providers handle exporting DAG metadata to different formats
/// (JSON, Mermaid, GraphML, etc.) with provider-specific configuration.
/// </remarks>
public interface IMetadataProvider {
  /// <summary>
  /// Gets the unique name of this provider.
  /// </summary>
  string Name { get; }

  /// <summary>
  /// Exports DAG metadata using this provider.
  /// </summary>
  /// <param name="dag">The DAG metadata to export</param>
  /// <param name="outputDirectory">Directory to write output files to</param>
  /// <param name="logger">Optional logger for diagnostic messages</param>
  /// <returns>True if export succeeded, false otherwise</returns>
  bool Export(DagMetadata dag, string outputDirectory, ILogger? logger = null);
}
