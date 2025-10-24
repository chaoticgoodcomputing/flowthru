using System.Text;
using Flowthru.Meta.Models;

namespace Flowthru.Meta;

/// <summary>
/// Extension methods for generating Mermaid diagram representations of DAG metadata.
/// </summary>
/// <remarks>
/// <para>
/// Mermaid diagrams provide immediate visualization in Markdown-compatible tools
/// (GitHub, VS Code, etc.) without requiring a separate web application.
/// </para>
/// <para>
/// The generated diagram uses Mermaid flowchart syntax with:
/// - Nodes as rectangles with rounded corners
/// - Catalog entries as cylindrical database shapes
/// - Pipeline subgraphs grouping nodes by their origin pipeline
/// - External data (no producer) shown with special styling
/// - Produced data (has producer) inside their producer's pipeline subgraph
/// </para>
/// </remarks>
public static class MermaidMetadataExtensions {
  /// <summary>
  /// Generates a Mermaid flowchart representation of the DAG, wrapped in a code fence.
  /// </summary>
  /// <param name="dag">The DAG metadata to visualize</param>
  /// <returns>Complete Markdown document with Mermaid code fence</returns>
  /// <remarks>
  /// <para>
  /// The output is a valid Markdown document that can be saved as a .md file
  /// and rendered by any Mermaid-compatible viewer.
  /// </para>
  /// <para>
  /// <strong>Example output:</strong>
  /// </para>
  /// <code>
  /// ```mermaid
  /// flowchart TB
  ///     RawCompanies[("Raw Companies")]
  ///     
  ///     subgraph DataProcessing["DataProcessing"]
  ///         PreprocessCompanies["Preprocess Companies"]
  ///         CleanedCompanies[("Cleaned Companies")]
  ///         RawCompanies --> PreprocessCompanies
  ///         PreprocessCompanies --> CleanedCompanies
  ///     end
  /// ```
  /// </code>
  /// </remarks>
  public static string ToMermaidDiagram(this DagMetadata dag) {
    var sb = new StringBuilder();

    // Start Mermaid code fence with flowchart (TB = Top to Bottom)
    sb.AppendLine("```mermaid");
    sb.AppendLine("flowchart TB");
    sb.AppendLine();

    // Classify catalog entries into external and produced
    var externalEntries = dag.CatalogEntries
      .Where(e => string.IsNullOrEmpty(e.Producer))
      .ToList();

    var producedEntries = dag.CatalogEntries
      .Where(e => !string.IsNullOrEmpty(e.Producer))
      .ToList();

    // Define all external data inputs first (cylindrical database shape)
    if (externalEntries.Any()) {
      sb.AppendLine("    %% External Data Inputs");
      foreach (var entry in externalEntries) {
        sb.AppendLine($"    {SanitizeId(entry.Key)}[(\"{EscapeLabel(entry.Label)}\")]");
      }
      sb.AppendLine();
    }

    // Group nodes by pipeline
    var pipelineGroups = dag.Nodes
      .GroupBy(n => n.PipelineName)
      .OrderBy(g => g.Key);

    foreach (var pipelineGroup in pipelineGroups) {
      var pipelineName = pipelineGroup.Key;
      var pipelineNodes = pipelineGroup.OrderBy(n => n.Layer).ThenBy(n => n.Id).ToList();

      sb.AppendLine($"    subgraph {SanitizeId(pipelineName)}[\"{EscapeLabel(pipelineName)}\"]");

      // Find produced catalog entries that belong to this pipeline
      var pipelineCatalogEntries = producedEntries
        .Where(e => pipelineNodes.Any(n => n.Id == e.Producer))
        .ToList();

      // Define nodes (rectangles)
      foreach (var node in pipelineNodes) {
        sb.AppendLine($"        {SanitizeId(node.Id)}[\"{EscapeLabel(node.Label)}\"]");
      }

      // Define catalog entries produced by this pipeline (cylindrical database shape)
      foreach (var entry in pipelineCatalogEntries) {
        sb.AppendLine($"        {SanitizeId(entry.Key)}[(\"{EscapeLabel(entry.Label)}\")]");
      }

      sb.AppendLine();

      // Generate edges for this pipeline
      foreach (var node in pipelineNodes) {
        // Input edges - only include if the input is produced by this pipeline (not external!)
        foreach (var input in node.Inputs) {
          var inputEntry = dag.CatalogEntries.FirstOrDefault(e => e.Key == input);
          if (inputEntry != null) {
            var isProducedByThisPipeline = pipelineCatalogEntries.Any(e => e.Key == input);

            // Only include edges from data produced within this pipeline
            if (isProducedByThisPipeline) {
              sb.AppendLine($"        {SanitizeId(input)} --> {SanitizeId(node.Id)}");
            }
          }
        }

        // Output edges - node to its produced catalog entries
        foreach (var output in node.Outputs) {
          var catalogEntry = pipelineCatalogEntries.FirstOrDefault(e => e.Key == output);
          if (catalogEntry != null) {
            sb.AppendLine($"        {SanitizeId(node.Id)} --> {SanitizeId(output)}");
          }
        }
      }

      sb.AppendLine("    end");
      sb.AppendLine();
    }

    // Generate external data to node edges (outside subgraphs)
    sb.AppendLine("    %% External Data to Pipeline Edges");
    foreach (var entry in externalEntries) {
      foreach (var consumer in entry.Consumers) {
        var consumerNode = dag.Nodes.FirstOrDefault(n => n.Id == consumer);
        if (consumerNode != null) {
          sb.AppendLine($"    {SanitizeId(entry.Key)} --> {SanitizeId(consumer)}");
        }
      }
    }
    sb.AppendLine();

    // Generate cross-pipeline edges (catalog entries that connect different pipelines)
    var crossPipelineEdges = new List<(string source, string target)>();

    foreach (var entry in producedEntries) {
      var producerNode = dag.Nodes.FirstOrDefault(n => n.Id == entry.Producer);
      if (producerNode == null) {
        continue;
      }

      foreach (var consumer in entry.Consumers) {
        var consumerNode = dag.Nodes.FirstOrDefault(n => n.Id == consumer);
        if (consumerNode != null && consumerNode.PipelineName != producerNode.PipelineName) {
          // This catalog entry connects two different pipelines
          crossPipelineEdges.Add((entry.Key, consumer));
        }
      }
    }

    if (crossPipelineEdges.Any()) {
      sb.AppendLine("    %% Cross-Pipeline Data Flow");
      foreach (var (source, target) in crossPipelineEdges.Distinct()) {
        sb.AppendLine($"    {SanitizeId(source)} -.-> {SanitizeId(target)}");
      }
    }

    // Close Mermaid code fence
    sb.AppendLine("```");

    return sb.ToString();
  }

  /// <summary>
  /// Sanitizes an identifier for use in Mermaid diagrams.
  /// </summary>
  /// <param name="id">The identifier to sanitize</param>
  /// <returns>Sanitized identifier safe for Mermaid</returns>
  /// <remarks>
  /// Mermaid has specific requirements for identifiers. This method ensures
  /// the ID is compatible by replacing problematic characters.
  /// </remarks>
  private static string SanitizeId(string id) {
    // Replace spaces and special characters with underscores
    return id.Replace(" ", "_")
      .Replace("-", "_")
      .Replace(".", "_")
      .Replace("(", "_")
      .Replace(")", "_")
      .Replace("[", "_")
      .Replace("]", "_");
  }

  /// <summary>
  /// Escapes a label for safe use in Mermaid diagrams.
  /// </summary>
  /// <param name="label">The label to escape</param>
  /// <returns>Escaped label safe for Mermaid</returns>
  private static string EscapeLabel(string label) {
    // Escape special characters that might break Mermaid syntax
    return label.Replace("\"", "\\\"");
  }
}
