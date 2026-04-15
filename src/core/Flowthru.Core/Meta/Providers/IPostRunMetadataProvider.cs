using Flowthru.Core.Graph.Meta.Models;

namespace Flowthru.Core.Meta.Providers;

/// <summary>
/// Optional interface for metadata providers that also want to receive post-run execution data.
/// </summary>
/// <remarks>
/// <para>
/// Implement this interface alongside <see cref="IMetadataProvider"/> to opt into the post-run
/// metadata lifecycle. The infrastructure checks for this interface after each real pipeline
/// execution and calls <see cref="Consume"/> with a composite of the pre-run DAG snapshot
/// and the completed <see cref="Flowthru.Core.Flows.FlowResult"/>.
/// </para>
/// <para>
/// Post-run providers are <strong>not</strong> invoked during dry runs.
/// </para>
/// <para>
/// Errors thrown from <see cref="Consume"/> are logged and suppressed — they will never
/// fail the pipeline execution.
/// </para>
/// <para>
/// <strong>Example — coloring a Mermaid diagram by step duration:</strong>
/// </para>
/// <code>
/// public class TimingMermaidProvider : IMetadataProvider, IPostRunMetadataProvider
/// {
///     public string Name => "TimingMermaid";
///
///     // Pre-run: export a plain structural diagram
///     public void Consume(DagMetadata dag) { ... }
///
///     // Post-run: export a diagram color-coded by actual execution time
///     public void Consume(RunMetadata run)
///     {
///         foreach (var step in run.Dag.Steps)
///         {
///             if (run.Result.StepResults.TryGetValue(step.Id, out var stepResult))
///             {
///                 // use stepResult.ExecutionTime to drive node styling
///             }
///         }
///     }
/// }
/// </code>
/// </remarks>
public interface IPostRunMetadataProvider
{
    /// <summary>
    /// Consumes composite post-run metadata combining the DAG snapshot and execution results.
    /// </summary>
    /// <param name="run">
    /// The combined run metadata, containing both the pre-run DAG structure and
    /// the execution outcome for all steps.
    /// </param>
    void Consume(RunMetadata run);
}
