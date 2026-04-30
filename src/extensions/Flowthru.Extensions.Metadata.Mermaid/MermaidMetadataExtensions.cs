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
  /// <param name="showFullDag">
  /// When true (default), the full DAG is rendered with active nodes highlighted.
  /// When false and a slice is applied, only nodes in the active slice are rendered.
  /// Has no effect when no slice is applied.
  /// </param>
  /// <returns>Complete Markdown document with Mermaid code fence</returns>
  public static string ToMermaidDiagram(
    this DagMetadata dag,
    string direction = "TB",
    string activeStepColor = "#2E7D32",
    string activeItemColor = "#2E7D32",
    bool showFullDag = true
  )
  {
    var sb = new StringBuilder();

    // When showFullDag=false and a slice is applied, restrict nodes to the active subset
    var isSliced = !showFullDag && dag.SlicedStepIds != null;

    var steps = isSliced
      ? dag.Steps.Where(s => dag.SlicedStepIds!.Contains(s.Id)).ToList()
      : dag.Steps;

    var catalogItems = isSliced
      ? dag.CatalogItems.Where(i => dag.SlicedCatalogItemIds!.Contains(i.Key)).ToList()
      : dag.CatalogItems;

    sb.AppendLine("```mermaid");
    sb.AppendLine($"flowchart {direction}");
    sb.AppendLine();

    // An item is "external" if it has no producer, or if its producer is not among the
    // visible steps (producer is outside the slice when showFullDag=false).
    var visibleStepIds = steps.Select(s => s.Id).ToHashSet();
    var externalItems = catalogItems
      .Where(e => string.IsNullOrEmpty(e.Producer) || !visibleStepIds.Contains(e.Producer))
      .ToList();
    var producedItems = catalogItems
      .Where(e => !string.IsNullOrEmpty(e.Producer) && visibleStepIds.Contains(e.Producer))
      .ToList();

    if (externalItems.Any())
    {
      sb.AppendLine("    %% External Data Inputs");
      foreach (var item in externalItems)
      {
        sb.AppendLine($"    {SanitizeId(item.Key)}[(\"{EscapeLabel(item.Label)}\")]");
      }
      sb.AppendLine();
    }

    var flowGroups = steps.GroupBy(n => n.FlowName).OrderBy(g => g.Key);

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

        if (isSliced || (dag.SlicedStepIds != null && dag.SlicedStepIds.Contains(step.Id)))
        {
          sb.AppendLine($"        style {stepId} fill:{activeStepColor}");
        }
      }

      foreach (var item in flowItems)
      {
        var itemId = SanitizeId(item.Key);
        var itemLabel = EscapeLabel(item.Label);

        sb.AppendLine($"        {itemId}[(\"{itemLabel}\")]");

        if (
          isSliced
          || (dag.SlicedCatalogItemIds != null && dag.SlicedCatalogItemIds.Contains(item.Key))
        )
        {
          sb.AppendLine($"        style {itemId} fill:{activeItemColor}");
        }
      }

      sb.AppendLine();

      foreach (var step in flowSteps)
      {
        foreach (var input in step.Inputs)
        {
          var inputItem = catalogItems.FirstOrDefault(e => e.Key == input);
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
        var consumerStep = steps.FirstOrDefault(n => n.Id == consumer);
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
      var producerStep = steps.FirstOrDefault(n => n.Id == item.Producer);
      if (producerStep == null)
      {
        continue;
      }

      foreach (var consumer in item.Consumers)
      {
        var consumerStep = steps.FirstOrDefault(n => n.Id == consumer);
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

    AppendServiceNodes(sb, steps);

    sb.AppendLine("```");

    return sb.ToString();
  }

  /// <summary>
  /// Renders service-dependency nodes for any step that declares
  /// <see cref="StepMetadata.ServiceDependencies"/>. Each unique service type appears
  /// once; consuming steps connect via dashed <c>-.uses.-&gt;</c> edges.
  /// </summary>
  /// <remarks>
  /// Services are rendered outside flow subgraphs because they are typically
  /// process-level resources (DI singletons) shared across flow boundaries.
  /// A dedicated <c>service</c> classDef visually distinguishes them from data
  /// items (cylinders) and steps (rectangles).
  /// </remarks>
  internal static void AppendServiceNodes(StringBuilder sb, IReadOnlyList<StepMetadata> steps)
  {
    // Collect (stepId, serviceFullName) pairs across all visible steps; build a
    // unique service set keyed by full name.
    var pairs = steps
      .SelectMany(s => s.ServiceDependencies.Select(svc => (StepId: s.Id, ServiceName: svc)))
      .ToList();

    if (pairs.Count == 0)
    {
      return;
    }

    var uniqueServices = pairs.Select(p => p.ServiceName).Distinct().OrderBy(n => n).ToList();

    sb.AppendLine();
    sb.AppendLine("    %% Service Dependencies");
    foreach (var fullName in uniqueServices)
    {
      var nodeId = ServiceNodeId(fullName);
      var displayName = SimpleTypeName(fullName);
      sb.AppendLine($"    {nodeId}[\"{EscapeLabel(displayName)}\"]");
    }
    sb.AppendLine();

    foreach (var (stepId, serviceName) in pairs.Distinct().OrderBy(p => p.StepId).ThenBy(p => p.ServiceName))
    {
      sb.AppendLine($"    {SanitizeId(stepId)} -.uses.-> {ServiceNodeId(serviceName)}");
    }
    sb.AppendLine();

    sb.AppendLine("    classDef service fill:#FEF7E0,stroke:#A05A00,color:#5E4400");
    var classList = string.Join(",", uniqueServices.Select(ServiceNodeId));
    sb.AppendLine($"    class {classList} service");
  }

  /// <summary>
  /// Stable, collision-resistant node ID for a service type. Uses a <c>svc_</c> prefix
  /// over the sanitized full name so service nodes never collide with step or item IDs.
  /// </summary>
  internal static string ServiceNodeId(string serviceFullName) =>
    "svc_" + SanitizeId(serviceFullName);

  /// <summary>
  /// Extracts the simple (unqualified) name from a fully-qualified type name.
  /// Falls back to the input when no <c>.</c> is present.
  /// </summary>
  internal static string SimpleTypeName(string fullName)
  {
    var lastDot = fullName.LastIndexOf('.');
    return lastDot >= 0 && lastDot < fullName.Length - 1
      ? fullName.Substring(lastDot + 1)
      : fullName;
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
