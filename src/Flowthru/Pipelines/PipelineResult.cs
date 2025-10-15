namespace Flowthru.Pipelines;

/// <summary>
/// Represents the result of a pipeline execution.
/// </summary>
/// <remarks>
/// <para>
/// This class provides comprehensive execution information including success status,
/// timing, individual node results, and error details.
/// </para>
/// <para>
/// <strong>Usage Pattern:</strong>
/// </para>
/// <code>
/// var result = await pipeline.RunAsync();
/// 
/// if (result.Success)
/// {
///     Console.WriteLine($"Pipeline completed in {result.ExecutionTime.TotalSeconds:F2}s");
///     foreach (var nodeResult in result.NodeResults.Values)
///     {
///         Console.WriteLine($"  {nodeResult.NodeName}: {nodeResult.ExecutionTime.TotalSeconds:F2}s");
///     }
/// }
/// else
/// {
///     Console.WriteLine($"Pipeline failed: {result.Exception?.Message}");
/// }
/// </code>
/// </remarks>
public class PipelineResult
{
    /// <summary>
    /// Indicates whether the pipeline executed successfully.
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// Total execution time for the entire pipeline.
    /// </summary>
    public TimeSpan ExecutionTime { get; init; }

    /// <summary>
    /// Results for individual nodes, keyed by node name.
    /// </summary>
    /// <remarks>
    /// Dictionary keys are the node names as specified in the pipeline definition.
    /// Values contain execution details for each node.
    /// </remarks>
    public Dictionary<string, NodeResult> NodeResults { get; init; } = new();

    /// <summary>
    /// Exception that caused pipeline failure, if any.
    /// </summary>
    /// <remarks>
    /// Null if Success is true. Contains the first exception that caused
    /// pipeline execution to halt if Success is false.
    /// </remarks>
    public Exception? Exception { get; init; }

    /// <summary>
    /// Creates a successful pipeline result.
    /// </summary>
    public static PipelineResult CreateSuccess(
        TimeSpan executionTime,
        Dictionary<string, NodeResult> nodeResults)
    {
        return new PipelineResult
        {
            Success = true,
            ExecutionTime = executionTime,
            NodeResults = nodeResults
        };
    }

    /// <summary>
    /// Creates a failed pipeline result.
    /// </summary>
    public static PipelineResult CreateFailure(
        TimeSpan executionTime,
        Exception exception,
        Dictionary<string, NodeResult>? nodeResults = null)
    {
        return new PipelineResult
        {
            Success = false,
            ExecutionTime = executionTime,
            Exception = exception,
            NodeResults = nodeResults ?? new()
        };
    }
}

/// <summary>
/// Represents the execution result of a single pipeline node.
/// </summary>
public class NodeResult
{
    /// <summary>
    /// The name of the node that was executed.
    /// </summary>
    public required string NodeName { get; init; }

    /// <summary>
    /// Indicates whether the node executed successfully.
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// Execution time for this specific node.
    /// </summary>
    public TimeSpan ExecutionTime { get; init; }

    /// <summary>
    /// Exception that occurred during node execution, if any.
    /// </summary>
    /// <remarks>
    /// Null if Success is true. Contains the exception that caused
    /// the node to fail if Success is false.
    /// </remarks>
    public Exception? Exception { get; init; }

    /// <summary>
    /// Number of input items processed by this node.
    /// </summary>
    /// <remarks>
    /// For multi-input nodes, this represents the total count across
    /// all input catalog entries.
    /// </remarks>
    public int InputCount { get; init; }

    /// <summary>
    /// Number of output items produced by this node.
    /// </summary>
    /// <remarks>
    /// For multi-output nodes, this represents the total count across
    /// all output catalog entries.
    /// </remarks>
    public int OutputCount { get; init; }

    /// <summary>
    /// Creates a successful node result.
    /// </summary>
    public static NodeResult CreateSuccess(
        string nodeName,
        TimeSpan executionTime,
        int inputCount,
        int outputCount)
    {
        return new NodeResult
        {
            NodeName = nodeName,
            Success = true,
            ExecutionTime = executionTime,
            InputCount = inputCount,
            OutputCount = outputCount
        };
    }

    /// <summary>
    /// Creates a failed node result.
    /// </summary>
    public static NodeResult CreateFailure(
        string nodeName,
        TimeSpan executionTime,
        Exception exception,
        int inputCount = 0)
    {
        return new NodeResult
        {
            NodeName = nodeName,
            Success = false,
            ExecutionTime = executionTime,
            Exception = exception,
            InputCount = inputCount,
            OutputCount = 0
        };
    }
}
