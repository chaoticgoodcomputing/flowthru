using Flowthru.Core.Graph.Meta.Models;

namespace Flowthru.Core.Meta.Providers;


/// <summary>
/// Optional interface for metadata providers that also want to receive post-run execution data.
/// </summary>
/// <remarks>
/// <para>
/// Implement this interface alongside <see cref="IMetadataProvider"/> to opt into the post-run
/// metadata lifecycle. The infrastructure checks for this interface after each real pipeline
/// execution and calls <see cref="Consume(RunMetadata)"/> with a composite of the pre-run DAG snapshot
/// and the completed <see cref="Flowthru.Core.Flows.FlowResult"/>.
/// </para>
/// <para>
/// Post-run providers are <strong>not</strong> invoked during dry runs.
/// </para>
/// <para>
/// Errors thrown from <see cref="Consume(RunMetadata)"/> are logged and suppressed — they will never
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

  /// <summary>
  /// Service-aware overload of <see cref="Consume(RunMetadata)"/>. Receives the host's
  /// fully-built <see cref="IServiceProvider"/> alongside the run metadata, allowing
  /// providers to resolve live runtime state (catalog instances, registered options,
  /// etc.) for inspection.
  /// </summary>
  /// <param name="run">The combined run metadata.</param>
  /// <param name="services">The host's built service provider.</param>
  /// <remarks>
  /// <para>
  /// The default implementation forwards to the simple <see cref="Consume(RunMetadata)"/>
  /// overload — providers that don't need DI access are unaffected. Override this method
  /// to opt into service resolution; the engine prefers this overload when both are
  /// implemented.
  /// </para>
  /// <para>
  /// <strong>Cost discipline.</strong> Resolving live state can be expensive (counting
  /// rows, hitting external storage, etc.). Providers that walk the catalog should
  /// default to cheap operations (e.g. only counting items whose adapters implement
  /// <see cref="Flowthru.Core.Data.Storage.IHasEfficientCount"/>) rather than forcing
  /// materialization. The framework does not police this — the convention is the
  /// provider's responsibility.
  /// </para>
  /// </remarks>
  void Consume(RunMetadata run, IServiceProvider services) => Consume(run);
}
