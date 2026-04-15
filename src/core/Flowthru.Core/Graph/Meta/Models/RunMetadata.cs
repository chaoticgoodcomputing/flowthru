using System.Text.Json.Serialization;
using Flowthru.Core.Flows;

namespace Flowthru.Core.Graph.Meta.Models;

/// <summary>
/// Composite metadata representing a completed pipeline run.
/// </summary>
/// <remarks>
/// <para>
/// Combines the structural pre-run DAG snapshot with the post-execution results,
/// giving post-run metadata providers access to both the pipeline topology and
/// the observed execution outcomes in a single call scope.
/// </para>
/// <para>
/// This is the primary argument type for <see cref="Flowthru.Core.Meta.Providers.IPostRunMetadataProvider"/>.
/// </para>
/// <para>
/// <strong>Example use cases:</strong>
/// </para>
/// <list type="bullet">
/// <item>Coloring a Mermaid diagram by per-step execution duration</item>
/// <item>Exporting combined diagnostic JSON (DAG structure + per-step results) for bug reports</item>
/// <item>Persisting step timings for future scheduling optimization</item>
/// </list>
/// </remarks>
public class RunMetadata
{
    /// <summary>
    /// The structural DAG snapshot built during pre-flight, before any steps executed.
    /// </summary>
    [JsonPropertyName("dag")]
    public required DagMetadata Dag { get; init; }

    /// <summary>
    /// The outcome of the pipeline run, including per-step results and timing.
    /// </summary>
    [JsonPropertyName("result")]
    public required FlowResult Result { get; init; }
}
