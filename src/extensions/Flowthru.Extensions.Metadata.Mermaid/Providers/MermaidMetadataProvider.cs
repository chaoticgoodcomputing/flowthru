using System.Text;
using Flowthru.Core.Flows;
using Flowthru.Core.Graph.Meta.Models;
using Flowthru.Core.Meta;
using Flowthru.Core.Meta.Providers;
using Microsoft.Extensions.Logging;

namespace Flowthru.Meta.Providers;

/// <summary>
/// Exports DAG metadata as Mermaid flowchart diagrams, and optionally exports
/// a post-run diagram colored by step execution outcomes.
/// </summary>
/// <remarks>
/// This provider creates Markdown files containing Mermaid flowchart diagrams
/// for immediate visualization in GitHub, VS Code, and other Mermaid-compatible viewers.
/// When post-run metadata is available, step nodes are colored by outcome: failed steps
/// are highlighted in red, steps that did not run are shown in grey, and successful steps
/// are colored on a green-to-amber heat map normalized to the slowest completed step.
/// </remarks>
[MetadataProviderBuilder(typeof(MermaidMetadataProviderBuilder))]
public class MermaidMetadataProvider : IMetadataProvider, IPostRunMetadataProvider
{
    private readonly MermaidFlowchartDirection _direction;
    private readonly string _activeStepColor;
    private readonly string _activeDataColor;
    private readonly string _failedStepColor;
    private readonly string _notRunStepColor;
    private readonly string _outputDirectory;
    private readonly string _dagFilenameTemplate;
    private readonly string _runFilenameTemplate;
    private readonly TimestampConfiguration _timestampConfig;
    private readonly ILogger? _logger;

    /// <summary>
    /// Flow direction for Mermaid flowcharts.
    /// </summary>
    public enum MermaidFlowchartDirection
    {
        /// <summary>Top to Bottom (default)</summary>
        TopToBottom,

        /// <summary>Left to Right</summary>
        LeftToRight,

        /// <summary>Bottom to Top</summary>
        BottomToTop,

        /// <summary>Right to Left</summary>
        RightToLeft,
    }

    /// <summary>
    /// Initializes a new Mermaid metadata provider.
    /// </summary>
    public MermaidMetadataProvider(
      string outputDirectory,
      string dagFilenameTemplate,
      string runFilenameTemplate,
      TimestampConfiguration timestampConfig,
      MermaidFlowchartDirection direction = MermaidFlowchartDirection.TopToBottom,
      string activeStepColor = "#2E7D32",
      string activeDataColor = "#2E7D32",
      string failedStepColor = "#C62828",
      string notRunStepColor = "#757575",
      ILogger? logger = null
    )
    {
        _outputDirectory = outputDirectory ?? throw new ArgumentNullException(nameof(outputDirectory));
        _dagFilenameTemplate =
          dagFilenameTemplate ?? throw new ArgumentNullException(nameof(dagFilenameTemplate));
        _runFilenameTemplate =
          runFilenameTemplate ?? throw new ArgumentNullException(nameof(runFilenameTemplate));
        _timestampConfig = timestampConfig ?? throw new ArgumentNullException(nameof(timestampConfig));
        _direction = direction;
        _activeStepColor = activeStepColor;
        _activeDataColor = activeDataColor;
        _failedStepColor = failedStepColor;
        _notRunStepColor = notRunStepColor;
        _logger = logger;
    }

    /// <inheritdoc />
    public string Name => "Mermaid";

    /// <inheritdoc />
    public void Consume(DagMetadata dag)
    {
        try
        {
            Directory.CreateDirectory(_outputDirectory);

            var timestamp = _timestampConfig.GenerateTimestamp();
            var filename = FilenameTemplateParser.Render(dag, _dagFilenameTemplate, timestamp) + ".md";
            var filePath = Path.Combine(_outputDirectory, filename);

            _logger?.LogInformation("Exporting Mermaid DAG diagram to {FilePath}", filePath);

            var mermaid = dag.ToMermaidDiagram(
              GetDirectionCode(_direction),
              _activeStepColor,
              _activeDataColor
            );

            AtomicWriteFile(filePath, mermaid);

            _logger?.LogInformation("Successfully exported Mermaid DAG diagram");
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(
              ex,
              "Failed to export Mermaid DAG diagram to {OutputDirectory}",
              _outputDirectory
            );
        }
    }

    /// <inheritdoc />
    public void Consume(RunMetadata run)
    {
        try
        {
            Directory.CreateDirectory(_outputDirectory);

            var timestamp = _timestampConfig.GenerateTimestamp();
            var filename =
              FilenameTemplateParser.Render(run.Dag, _runFilenameTemplate, timestamp) + ".md";
            var filePath = Path.Combine(_outputDirectory, filename);

            _logger?.LogInformation("Exporting Mermaid run diagram to {FilePath}", filePath);

            var mermaid = RenderRunDiagram(run);

            AtomicWriteFile(filePath, mermaid);

            _logger?.LogInformation(
              "Successfully exported Mermaid run diagram (success={Success})",
              run.Result.Success
            );
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(
              ex,
              "Failed to export Mermaid run diagram to {OutputDirectory}",
              _outputDirectory
            );
        }
    }

    /// <summary>
    /// Renders the DAG colored by actual step execution outcomes.
    /// </summary>
    /// <remarks>
    /// Step coloring rules:
    /// - Failed: <see cref="_failedStepColor"/> (default red)
    /// - Did not run (no result): <see cref="_notRunStepColor"/> (default grey)
    /// - Succeeded: green-to-amber heat map, normalized against the slowest successful step
    /// </remarks>
    private string RenderRunDiagram(RunMetadata run)
    {
        var dag = run.Dag;
        var stepResults = run.Result.StepResults;
        var direction = GetDirectionCode(_direction);

        // Pre-compute heat map colors for successful steps
        var successDurations = stepResults
          .Values.Where(r => r.Success)
          .Select(r => r.ExecutionTime.TotalMilliseconds)
          .ToList();

        var maxDurationMs = successDurations.Count > 0 ? successDurations.Max() : 1.0;

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
                sb.AppendLine(
                  $"    {MermaidMetadataExtensions.SanitizeId(item.Key)}[(\"{MermaidMetadataExtensions.EscapeLabel(item.Label)}\")]"
                );
            }
            sb.AppendLine();
        }

        var flowGroups = dag.Steps.GroupBy(n => n.FlowName).OrderBy(g => g.Key);

        foreach (var flowGroup in flowGroups)
        {
            var flowName = flowGroup.Key;
            var flowSteps = flowGroup.OrderBy(n => n.Layer).ThenBy(n => n.Id).ToList();

            sb.AppendLine(
              $"    subgraph {MermaidMetadataExtensions.SanitizeId(flowName)}[\"{MermaidMetadataExtensions.EscapeLabel(flowName)}\"]"
            );

            var flowItems = producedItems.Where(e => flowSteps.Any(n => n.Id == e.Producer)).ToList();

            foreach (var step in flowSteps)
            {
                var stepId = MermaidMetadataExtensions.SanitizeId(step.Id);
                var stepLabel = MermaidMetadataExtensions.EscapeLabel(step.Label);

                sb.AppendLine($"        {stepId}[\"{stepLabel}\"]");

                var color = GetStepColor(step, stepResults, maxDurationMs);
                sb.AppendLine($"        style {stepId} fill:{color}");
            }

            foreach (var item in flowItems)
            {
                var itemId = MermaidMetadataExtensions.SanitizeId(item.Key);
                var itemLabel = MermaidMetadataExtensions.EscapeLabel(item.Label);
                sb.AppendLine($"        {itemId}[(\"{itemLabel}\")]");
            }

            sb.AppendLine();

            foreach (var step in flowSteps)
            {
                foreach (var input in step.Inputs)
                {
                    var inputItem = dag.CatalogItems.FirstOrDefault(e => e.Key == input);
                    if (inputItem != null && flowItems.Any(e => e.Key == input))
                    {
                        sb.AppendLine(
                          $"        {MermaidMetadataExtensions.SanitizeId(input)} --> {MermaidMetadataExtensions.SanitizeId(step.Id)}"
                        );
                    }
                }

                foreach (var output in step.Outputs)
                {
                    if (flowItems.Any(e => e.Key == output))
                    {
                        sb.AppendLine(
                          $"        {MermaidMetadataExtensions.SanitizeId(step.Id)} --> {MermaidMetadataExtensions.SanitizeId(output)}"
                        );
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
                if (dag.Steps.Any(n => n.Id == consumer))
                {
                    sb.AppendLine(
                      $"    {MermaidMetadataExtensions.SanitizeId(item.Key)} --> {MermaidMetadataExtensions.SanitizeId(consumer)}"
                    );
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
                sb.AppendLine(
                  $"    {MermaidMetadataExtensions.SanitizeId(source)} -.-> {MermaidMetadataExtensions.SanitizeId(target)}"
                );
            }
        }

        sb.AppendLine("```");

        return sb.ToString();
    }

    /// <summary>
    /// Resolves the fill color for a step node based on its execution result.
    /// </summary>
    /// <remarks>
    /// Successful steps are colored on a green-to-amber heat map where the fastest step
    /// is pure green (#2E7D32) and the slowest is amber (#F57F17), normalized across
    /// all successful steps in the run.
    /// </remarks>
    private string GetStepColor(
      StepMetadata step,
      Dictionary<string, StepResult> stepResults,
      double maxDurationMs
    )
    {
        if (!stepResults.TryGetValue(step.Id, out var result))
        {
            return _notRunStepColor;
        }

        if (!result.Success)
        {
            return _failedStepColor;
        }

        // Normalize 0.0 (fastest) → 1.0 (slowest), then interpolate green → amber
        var normalized =
          maxDurationMs > 0 ? result.ExecutionTime.TotalMilliseconds / maxDurationMs : 0.0;

        return InterpolateGreenToAmber(normalized);
    }

    /// <summary>
    /// Interpolates between green (#2E7D32) and amber (#F57F17) based on a 0–1 ratio.
    /// </summary>
    private static string InterpolateGreenToAmber(double t)
    {
        t = Math.Clamp(t, 0.0, 1.0);

        // Green: R=0x2E G=0x7D B=0x32
        // Amber: R=0xF5 G=0x7F B=0x17
        var r = (int)(0x2E + (0xF5 - 0x2E) * t);
        var g = (int)(0x7D + (0x7F - 0x7D) * t);
        var b = (int)(0x32 + (0x17 - 0x32) * t);

        return $"#{r:X2}{g:X2}{b:X2}";
    }

    private static string GetDirectionCode(MermaidFlowchartDirection direction)
    {
        return direction switch
        {
            MermaidFlowchartDirection.TopToBottom => "TB",
            MermaidFlowchartDirection.LeftToRight => "LR",
            MermaidFlowchartDirection.BottomToTop => "BT",
            MermaidFlowchartDirection.RightToLeft => "RL",
            _ => "TB",
        };
    }

    private static void AtomicWriteFile(string filePath, string content)
    {
        var tempPath = filePath + ".tmp";

        try
        {
            File.WriteAllText(tempPath, content);

            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }

            File.Move(tempPath, filePath);
        }
        finally
        {
            if (File.Exists(tempPath))
            {
                try
                {
                    File.Delete(tempPath);
                }
                catch
                {
                    // Ignore cleanup errors
                }
            }
        }
    }
}
