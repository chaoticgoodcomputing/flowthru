using System.Text;
using Flowthru.Core.Graph.Meta.Models;

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
/// - Produced data (has producer) inside their producer's Flow subgraph
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
    public static string ToMermaidDiagram(
      this DagMetadata dag,
      string direction = "TB",
      string activeStepColor = "#2E7D32",
      string activeItemColor = "#2E7D32"
    )
    {
        var sb = new StringBuilder();

        sb.AppendLine("```mermaid");
        sb.AppendLine($"flowchart {direction}");
        sb.AppendLine();

        var externalItems = dag.CatalogItems.Where(e => string.IsNullOrEmpty(e.Producer)).ToList();
        var producedItems = dag.CatalogItems.Where(e => !string.IsNullOrEmpty(e.Producer)).ToList();

        if (externalItems.Any())
        {
            sb.AppendLine("    %% External Data Inputs");
            foreach (var item in externalItems)
            {
                sb.AppendLine($"    {SanitizeId(item.Key)}[(\"{EscapeLabel(item.Label)}\")]");
            }
            sb.AppendLine();
        }

        var flowGroups = dag.Steps.GroupBy(n => n.FlowName).OrderBy(g => g.Key);

        foreach (var flowGroup in flowGroups)
        {
            var flowName = flowGroup.Key;
            var flowSteps = flowGroup.OrderBy(n => n.Layer).ThenBy(n => n.Id).ToList();

            sb.AppendLine($"    subgraph {SanitizeId(flowName)}[\"{EscapeLabel(flowName)}\"]");

            var flowItems = producedItems.Where(e => flowSteps.Any(n => n.Id == e.Producer)).ToList();

            foreach (var step in flowSteps)
            {
                var stepId = SanitizeId(step.Id);
                var stepLabel = EscapeLabel(step.Label);

                sb.AppendLine($"        {stepId}[\"{stepLabel}\"]");

                if (dag.SlicedStepIds != null && dag.SlicedStepIds.Contains(step.Id))
                {
                    sb.AppendLine($"        style {stepId} fill:{activeStepColor}");
                }
            }

            foreach (var item in flowItems)
            {
                var itemId = SanitizeId(item.Key);
                var itemLabel = EscapeLabel(item.Label);

                sb.AppendLine($"        {itemId}[(\"{itemLabel}\")]");

                if (dag.SlicedCatalogItemIds != null && dag.SlicedCatalogItemIds.Contains(item.Key))
                {
                    sb.AppendLine($"        style {itemId} fill:{activeItemColor}");
                }
            }

            sb.AppendLine();

            foreach (var step in flowSteps)
            {
                foreach (var input in step.Inputs)
                {
                    var inputItem = dag.CatalogItems.FirstOrDefault(e => e.Key == input);
                    if (inputItem != null)
                    {
                        var isProducedByThisFlow = flowItems.Any(e => e.Key == input);
                        if (isProducedByThisFlow)
                        {
                            sb.AppendLine($"        {SanitizeId(input)} --> {SanitizeId(step.Id)}");
                        }
                    }
                }

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

        sb.AppendLine("```");

        return sb.ToString();
    }

    /// <summary>
    /// Sanitizes an identifier for use in Mermaid diagrams.
    /// </summary>
    internal static string SanitizeId(string id)
    {
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
    internal static string EscapeLabel(string label)
    {
        return label.Replace("\"", "\\\"");
    }
}
