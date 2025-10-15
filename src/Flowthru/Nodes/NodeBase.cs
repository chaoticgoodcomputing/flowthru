using Microsoft.Extensions.Logging;

namespace Flowthru.Nodes;

/// <summary>
/// Abstract base class for all transformation nodes in a Flowthru pipeline.
/// </summary>
/// <typeparam name="TInput">
/// The input type for this node. Can be:
/// - A single data type for simple nodes (e.g., IEnumerable&lt;CompanySchema&gt;)
/// - A schema type with multiple properties for multi-input nodes (e.g., TrainModelInputs)
/// </typeparam>
/// <typeparam name="TOutput">
/// The output type for this node. Can be:
/// - A single data type for simple nodes (e.g., IEnumerable&lt;ProcessedData&gt;)
/// - A schema type with multiple properties for multi-output nodes (e.g., SplitDataOutputs)
/// </typeparam>
/// <remarks>
/// <para>
/// <strong>Design Pattern:</strong> Template Method Pattern - defines the skeleton of node
/// execution while allowing subclasses to implement specific transformation logic.
/// </para>
/// <para>
/// <strong>Single Abstract Class Strategy:</strong>
/// Instead of having multiple node base classes for different input/output arities
/// (Node&lt;TIn, TOut&gt;, Node&lt;TIn1, TIn2, TOut&gt;, etc.), Flowthru uses a SINGLE
/// abstract node class. Multi-input/output scenarios are handled via input/output schemas
/// combined with CatalogMap for property-to-catalog mapping.
/// </para>
/// <para>
/// <strong>Constructor Requirements:</strong>
/// All node implementations MUST have a parameterless constructor. This is enforced by
/// the `new()` generic constraint in PipelineBuilder. Parameterless constructors enable:
/// - Type-based instantiation via Activator.CreateInstance&lt;T&gt;()
/// - Compiled expression factories (see NodeFactory)
/// - Distributed/parallel execution scenarios (future)
/// </para>
/// <para>
/// <strong>Dependency Injection Pattern:</strong>
/// Dependencies are injected via properties, NOT constructor parameters. This maintains
/// the parameterless constructor requirement while still supporting DI.
/// Example:
/// <code>
/// public class MyNode : NodeBase&lt;Input, Output&gt;
/// {
///     public ILogger&lt;MyNode&gt;? Logger { get; set; }
///     public IMyService? MyService { get; set; }
/// }
/// </code>
/// Dependencies are injected during pipeline configuration via the configure action.
/// </para>
/// <para>
/// <strong>Logging Integration:</strong>
/// All nodes have an optional Logger property for Serilog integration. The ExecuteAsync
/// method wraps Transform() with logging for observability.
/// </para>
/// </remarks>
public abstract class NodeBase<TInput, TOutput>
{
    /// <summary>
    /// Optional logger for this node.
    /// Injected via property during pipeline configuration.
    /// </summary>
    /// <remarks>
    /// Uses Microsoft.Extensions.Logging abstractions for compatibility with Serilog
    /// and other logging providers.
    /// </remarks>
    public ILogger? Logger { get; set; }

    /// <summary>
    /// Abstract transformation method that derived classes must implement.
    /// This is where the node's core logic resides.
    /// </summary>
    /// <param name="input">
    /// Input data for transformation. For multi-input nodes, this will be an enumerable
    /// containing a single schema instance with all input properties populated.
    /// </param>
    /// <returns>
    /// Transformed output data. For multi-output nodes, return an enumerable containing
    /// a single schema instance with all output properties populated.
    /// </returns>
    /// <remarks>
    /// <para>
    /// <strong>Important:</strong> Implementations should be pure functions where possible.
    /// Avoid side effects except for necessary I/O operations.
    /// </para>
    /// <para>
    /// <strong>Async Pattern:</strong> Even if your transformation is synchronous, return
    /// Task.FromResult() to maintain the async interface for consistency.
    /// </para>
    /// </remarks>
    protected abstract Task<IEnumerable<TOutput>> Transform(IEnumerable<TInput> input);

    /// <summary>
    /// Internal execution method called by the pipeline executor.
    /// Wraps Transform() with logging for observability.
    /// </summary>
    /// <param name="input">Input data for transformation</param>
    /// <returns>Transformed output data</returns>
    /// <remarks>
    /// This method is internal and should not be called directly by user code.
    /// The pipeline executor invokes this method during pipeline execution.
    /// </remarks>
    internal async Task<IEnumerable<TOutput>> ExecuteAsync(IEnumerable<TInput> input)
    {
        var nodeName = GetType().Name;

        Logger?.LogInformation("Starting transformation for node {NodeName}", nodeName);

        try
        {
            var result = await Transform(input);
            var count = result?.Count() ?? 0;

            Logger?.LogInformation(
                "Completed transformation for node {NodeName}, produced {OutputCount} outputs",
                nodeName,
                count);

            return result ?? Enumerable.Empty<TOutput>();
        }
        catch (Exception ex)
        {
            Logger?.LogError(ex,
                "Error during transformation in node {NodeName}: {ErrorMessage}",
                nodeName,
                ex.Message);

            throw; // Re-throw to allow pipeline executor to handle
        }
    }
}
