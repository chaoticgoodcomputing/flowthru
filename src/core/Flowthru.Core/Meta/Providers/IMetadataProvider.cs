using Flowthru.Core.Graph.Meta.Models;

namespace Flowthru.Core.Meta.Providers;

/// <summary>
/// Interface for metadata consumers.
/// </summary>
/// <remarks>
/// <para>
/// Metadata providers receive DAG metadata after pipeline builds and can
/// process it in any way: write files, send to APIs, store in memory, etc.
/// </para>
/// <para>
/// <strong>Built-in Providers:</strong>
/// </para>
/// <list type="bullet">
/// <item>Flowthru.Extensions.Metadata.Json - Exports JSON files</item>
/// <item>Flowthru.Extensions.Metadata.Mermaid - Exports Mermaid diagrams</item>
/// </list>
/// <para>
/// <strong>Custom Provider Example:</strong>
/// </para>
/// <code>
/// public class DashboardMetadataProvider : IMetadataProvider
/// {
///   private readonly IDashboardClient _client;
///
///   public DashboardMetadataProvider(IDashboardClient client)
///   {
///     _client = client;
///   }
///
///   public string Name => "Dashboard";
///
///   public void Consume(DagMetadata dag)
///   {
///     _client.SendVisualization(dag);
///   }
/// }
/// </code>
/// </remarks>
public interface IMetadataProvider
{
    /// <summary>
    /// Gets the unique name of this provider.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Consumes DAG metadata.
    /// </summary>
    /// <param name="dag">The DAG metadata to consume</param>
    /// <remarks>
    /// This method is called after pipeline builds. Providers can process
    /// the metadata in any way: write files, send to APIs, store in memory, etc.
    ///
    /// Implementations should handle their own error recovery - exceptions thrown
    /// from this method will be logged but will not fail the pipeline execution.
    /// </remarks>
    void Consume(DagMetadata dag);
}
