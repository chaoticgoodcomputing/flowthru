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
/// - Steps as rectangles with rounded corners
/// - Catalog items as cylindrical database shapes
/// - Flow subgraphs grouping nodes by their origin flow
/// - External data (no producer) shown with special styling
/// - Produced data (has producer) inside their producer's flow subgraph
/// </para>
/// </remarks>
public static class MermaidMetadataExtensions
{
  /// <summary>
  /// Generates a Mermaid flowchart representation of the DAG, wrapped in a code fence.
  /// </summary>
  /// <param name="dag">The DAG metadata to visualize</param>
  /// <param name="direction">Flow direction code (TB, LR, BT, RL). Defaults to TB (Top to Bottom).</param>
  /// <param name="activeStepColor">Hex color for active (sliced) steps. Defaults to #2E7D32.</param>
  /// <param name="activeItemColor">Hex color for active (sliced) catalog items. Defaults to #2E7D32.</param>
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
  public static string ToMermaidDiagram(
    this DagMetadata dag,
    string direction = "TB",
    string activeStepColor = "#2E7D32",
    string activeItemColor = "#2E7D32"
  )
  {
    var sb = new StringBuilder();

    // Start Mermaid code fence with flowchart and specified direction
    sb.AppendLine("```mermaid");
    sb.AppendLine($"flowchart {direction}");
    sb.AppendLine();

    // Classify catalog items into external and produced
    var externalItems = dag.CatalogItems.Where(e => string.IsNullOrEmpty(e.Producer)).ToList();

    var producedItems = dag.CatalogItems.Where(e => !string.IsNullOrEmpty(e.Producer)).ToList();

    // Define all external data inputs first (cylindrical database shape)
    if (externalItems.Any())
    {
      sb.AppendLine("    %% External Data Inputs");
      foreach (var item in externalItems)
      {
        sb.AppendLine($"    {SanitizeId(item.Key)}[(\"{EscapeLabel(item.Label)}\")]");
      }
      sb.AppendLine();
    }

    // Group steps by flow
    var flowGroups = dag.Steps.GroupBy(n => n.FlowName).OrderBy(g => g.Key);

    foreach (var flowGroup in flowGroups)
    {
      var flowName = flowGroup.Key;
      var flowSteps = flowGroup.OrderBy(n => n.Layer).ThenBy(n => n.Id).ToList();

      sb.AppendLine($"    subgraph {SanitizeId(flowName)}[\"{EscapeLabel(flowName)}\"]");

      // Find produced catalog items that belong to this flow
      var flowItems = producedItems.Where(e => flowSteps.Any(n => n.Id == e.Producer)).ToList();

      // Define steps (rectangles) with styling for steps in the slice
      foreach (var step in flowSteps)
      {
        var stepId = SanitizeId(step.Id);
        var stepLabel = EscapeLabel(step.Label);

        // Apply color fill to steps in the execution slice
        if (dag.SlicedStepIds != null && dag.SlicedStepIds.Contains(step.Id))
        {
          sb.AppendLine($"        {stepId}[\"{stepLabel}\"]");
          sb.AppendLine($"        style {stepId} fill:{activeStepColor}");
        }
        else
        {
          sb.AppendLine($"        {stepId}[\"{stepLabel}\"]");
        }
      }

      // Define catalog items produced by this flow (cylindrical database shape)
      foreach (var item in flowItems)
      {
        var itemId = SanitizeId(item.Key);
        var itemLabel = EscapeLabel(item.Label);

        sb.AppendLine($"        {itemId}[(\"{itemLabel}\")]");

        // Apply color fill to catalog items in the execution slice
        if (dag.SlicedCatalogItemIds != null && dag.SlicedCatalogItemIds.Contains(item.Key))
        {
          sb.AppendLine($"        style {itemId} fill:{activeItemColor}");
        }
      }

      sb.AppendLine();

      // Generate edges for this flow
      foreach (var step in flowSteps)
      {
        // Input edges - only include if the input is produced by this flow (not external!)
        foreach (var input in step.Inputs)
        {
          var inputItem = dag.CatalogItems.FirstOrDefault(e => e.Key == input);
          if (inputItem != null)
          {
            var isProducedByThisFlow = flowItems.Any(e => e.Key == input);

            // Only include edges from data produced within this flow
            if (isProducedByThisFlow)
            {
              sb.AppendLine($"        {SanitizeId(input)} --> {SanitizeId(step.Id)}");
            }
          }
        }

        // Output edges - node to its produced catalog items
        foreach (var output in step.Outputs)
        {
          var catalogItem = flowItems.FirstOrDefault(e => e.Key == output);
          if (catalogItem != null)
          {
            sb.AppendLine($"        {SanitizeId(step.Id)} --> {SanitizeId(output)}");
          }
        }
      }

      sb.AppendLine("    end");
      sb.AppendLine();
    }

    // Generate external data to step edges (outside subgraphs)
    sb.AppendLine("    %% External Data to Flow Edges");
    foreach (var item in externalItems)
    {
      foreach (var consumer in item.Consumers)
      {
        var consumerStep = dag.Steps.FirstOrDefault(n => n.Id == consumer);
        if (consumerStep != null)
        {
          sb.AppendLine($"    {SanitizeId(item.Key)} --> {SanitizeId(consumer)}");
        }
      }
    }
    sb.AppendLine();

    // Generate cross-flow edges (catalog items that connect different flows)
    var crossFlowEdges = new List<(string source, string target)>();

    foreach (var item in producedItems)
    {
      var producerStep = dag.Steps.FirstOrDefault(n => n.Id == item.Producer);
      if (producerStep == null)
      {
        continue;
      }

      foreach (var consumer in item.Consumers)
      {
        var consumerStep = dag.Steps.FirstOrDefault(n => n.Id == consumer);
        if (consumerStep != null && consumerStep.FlowName != producerStep.FlowName)
        {
          // This catalog item connects two different flows
          crossFlowEdges.Add((item.Key, consumer));
        }
      }
    }

    if (crossFlowEdges.Any())
    {
      sb.AppendLine("    %% Cross-Flow Data Flow");
      foreach (var (source, target) in crossFlowEdges.Distinct())
      {
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
  private static string SanitizeId(string id)
  {
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
  private static string EscapeLabel(string label)
  {
    // Escape special characters that might break Mermaid syntax
    return label.Replace("\"", "\\\"");
  }
}
