namespace Flowthru.Meta;

/// <summary>
/// Configuration for Flowthru metadata collection and export.
/// </summary>
/// <remarks>
/// <para>
/// This configuration controls whether and how pipeline metadata is collected
/// and persisted. Metadata includes DAG structure (nodes, catalog entries, edges)
/// that can be consumed by Flowthru.Viz for visualization.
/// </para>
/// <para>
/// <strong>Usage:</strong>
/// </para>
/// <code>
/// builder.IncludeMetadata(metadata => {
///     metadata
///         .WithOutputDirectory("Data/Metadata")
///         .EnableAutoExport();
/// });
/// </code>
/// </remarks>
public class FlowthruMetadataConfiguration {
  /// <summary>
  /// Directory where metadata JSON files will be written.
  /// </summary>
  /// <remarks>
  /// Default: "Data/Metadata"
  /// </remarks>
  public string OutputDirectory { get; private set; } = "Data/Metadata";

  /// <summary>
  /// Whether to automatically export DAG metadata after Pipeline.Build().
  /// </summary>
  /// <remarks>
  /// Default: true
  /// When enabled, DAG JSON files are automatically created after each pipeline build.
  /// </remarks>
  public bool AutoExportDag { get; private set; } = true;

  /// <summary>
  /// Whether to export Mermaid diagram files (.md) alongside JSON files.
  /// </summary>
  /// <remarks>
  /// Default: true
  /// When enabled, a Markdown file with an embedded Mermaid diagram is created
  /// alongside each JSON file for immediate visualization.
  /// </remarks>
  public bool ExportMermaid { get; private set; } = true;

  /// <summary>
  /// Sets the output directory for metadata files.
  /// </summary>
  /// <param name="directory">Directory path (absolute or relative to working directory)</param>
  /// <returns>This configuration for fluent chaining</returns>
  public FlowthruMetadataConfiguration WithOutputDirectory(string directory) {
    if (string.IsNullOrWhiteSpace(directory)) {
      throw new ArgumentException("Output directory cannot be null or empty", nameof(directory));
    }

    OutputDirectory = directory;
    return this;
  }

  /// <summary>
  /// Enables automatic DAG export after pipeline builds.
  /// </summary>
  /// <returns>This configuration for fluent chaining</returns>
  public FlowthruMetadataConfiguration EnableAutoExport() {
    AutoExportDag = true;
    return this;
  }

  /// <summary>
  /// Disables automatic DAG export.
  /// </summary>
  /// <remarks>
  /// Use this when you want manual control over metadata export via Pipeline.ExportDag().
  /// </remarks>
  /// <returns>This configuration for fluent chaining</returns>
  public FlowthruMetadataConfiguration DisableAutoExport() {
    AutoExportDag = false;
    return this;
  }

  /// <summary>
  /// Enables Mermaid diagram export.
  /// </summary>
  /// <returns>This configuration for fluent chaining</returns>
  public FlowthruMetadataConfiguration EnableMermaid() {
    ExportMermaid = true;
    return this;
  }

  /// <summary>
  /// Disables Mermaid diagram export.
  /// </summary>
  /// <remarks>
  /// Use this when you only want JSON output without Mermaid diagrams.
  /// </remarks>
  /// <returns>This configuration for fluent chaining</returns>
  public FlowthruMetadataConfiguration DisableMermaid() {
    ExportMermaid = false;
    return this;
  }
}
