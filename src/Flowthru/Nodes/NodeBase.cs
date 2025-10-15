using System.ComponentModel;
using Microsoft.Extensions.Logging;

namespace Flowthru.Nodes;

/// <summary>
/// Abstract base class for all transformation nodes in a Flowthru pipeline.
/// </summary>
/// <typeparam name="TInput">
/// <para>
/// The <strong>individual item type</strong> for this node's input, NOT a collection type.
/// </para>
/// <para>
/// For simple nodes: Use the item type directly (e.g., CompanySchema, not IEnumerable&lt;CompanySchema&gt;).
/// The Transform method will receive IEnumerable&lt;TInput&gt;, but TInput refers to individual items.
/// </para>
/// <para>
/// For multi-input nodes: Use a schema type that bundles multiple input properties
/// (e.g., TrainModelInputs containing XTrain and YTrain properties).
/// </para>
/// <para>
/// <strong>Important:</strong> Catalog entries store IEnumerable&lt;TInput&gt;, but node definitions
/// use TInput to represent the item type. This semantic distinction aligns type parameters with
/// what developers think about (individual items) while maintaining collection-based storage.
/// </para>
/// </typeparam>
/// <typeparam name="TOutput">
/// <para>
/// The <strong>individual item type</strong> for this node's output, NOT a collection type.
/// </para>
/// <para>
/// For simple nodes: Use the item type directly (e.g., ProcessedData, not IEnumerable&lt;ProcessedData&gt;).
/// The Transform method returns IEnumerable&lt;TOutput&gt;, but TOutput refers to individual items.
/// </para>
/// <para>
/// For multi-output nodes: Use a schema type that bundles multiple output properties
/// (e.g., SplitDataOutputs containing Train and Test properties).
/// </para>
/// </typeparam>
/// <typeparam name="TParameters">
/// The parameters type for configuring this node. Use NoParams if no parameters are needed.
/// Parameters are injected via the Parameters property during pipeline configuration.
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
/// <strong>Three-Type-Parameter Pattern:</strong>
/// All nodes can specify three types: TInput, TOutput, and TParameters.
/// For nodes without parameters, use the two-parameter NodeBase&lt;TInput, TOutput&gt;
/// convenience base class which defaults TParameters to NoParams.
/// Example:
/// <code>
/// // Without parameters
/// public class SimpleNode : NodeBase&lt;Input, Output&gt; { }
/// 
/// // With parameters
/// public class ConfigurableNode : NodeBase&lt;Input, Output, MyParameters&gt;
/// {
///     // Parameters property is automatically available
///     protected override Task&lt;IEnumerable&lt;Output&gt;&gt; Transform(IEnumerable&lt;Input&gt; input)
///     {
///         var testSize = Parameters.TestSize; // Access parameters
///         // ...
///     }
/// }
/// </code>
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
/// <strong>Property Injection Pattern:</strong>
/// Dependencies and parameters are injected via properties, NOT constructor parameters.
/// This maintains the parameterless constructor requirement while still supporting DI.
/// Example:
/// <code>
/// public class MyNode : NodeBase&lt;Input, Output, MyParameters&gt;
/// {
///     // Logger injected automatically by pipeline
///     public ILogger? Logger { get; set; }
///     
///     // Parameters injected via configureNode callback
///     public MyParameters Parameters { get; set; } = new();
///     
///     // Custom service injection
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
public abstract class NodeBase<TInput, TOutput, TParameters>
  where TParameters : new()
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
  /// Parameters for configuring this node's behavior.
  /// Set via property injection during pipeline configuration using the configureNode callback.
  /// </summary>
  /// <remarks>
  /// <para>
  /// Parameters are initialized with a default instance. Override during pipeline configuration:
  /// </para>
  /// <code>
  /// pipeline.AddNode&lt;MyNode&gt;(
  ///     input: inputMap,
  ///     output: outputMap,
  ///     configure: node =&gt; node.Parameters = new MyParameters 
  ///     { 
  ///         TestSize = 0.3,
  ///         RandomSeed = 42 
  ///     }
  /// );
  /// </code>
  /// </remarks>
  public TParameters Parameters { get; set; } = new();

  /// <summary>
  /// Abstract transformation method that derived classes must implement.
  /// This is where the node's core logic resides.
  /// </summary>
  /// <param name="input">
  /// <para>
  /// Collection of input items to transform. Note that while the parameter type is
  /// IEnumerable&lt;TInput&gt;, the generic type parameter TInput represents the
  /// <strong>individual item type</strong>, not a collection.
  /// </para>
  /// <para>
  /// For simple nodes: Process each TInput item in the collection.
  /// Example: IEnumerable&lt;CompanyRawSchema&gt; where TInput = CompanyRawSchema.
  /// </para>
  /// <para>
  /// For multi-input nodes: This will be a singleton collection containing one schema
  /// instance with all input properties populated from multiple catalog entries.
  /// Example: IEnumerable&lt;TrainModelInputs&gt; with a single element.
  /// </para>
  /// </param>
  /// <returns>
  /// <para>
  /// Collection of transformed output items. The generic type parameter TOutput represents
  /// the <strong>individual output item type</strong>, not a collection.
  /// </para>
  /// <para>
  /// For simple nodes: Return a collection of TOutput items.
  /// Example: IEnumerable&lt;CompanySchema&gt; where TOutput = CompanySchema.
  /// </para>
  /// <para>
  /// For multi-output nodes: Return a singleton collection containing one schema
  /// instance with all output properties populated for distribution to multiple catalog entries.
  /// Example: new[] { splitOutputs } where splitOutputs contains Train and Test properties.
  /// </para>
  /// </returns>
  /// <remarks>
  /// <para>
  /// <strong>Type Parameter Semantics:</strong> TInput and TOutput represent item types,
  /// while the method signature uses IEnumerable&lt;TInput&gt; and IEnumerable&lt;TOutput&gt;
  /// for the actual data flow. This design keeps node definitions intuitive (thinking about
  /// individual items) while maintaining collection-based processing for efficiency.
  /// </para>
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
  /// Test helper method that exposes Transform() for unit testing.
  /// </summary>
  /// <param name="input">Input data for transformation</param>
  /// <returns>Transformed output data</returns>
  /// <remarks>
  /// <para>
  /// <strong>For Testing Only:</strong> This method is provided to enable unit testing
  /// of node transformation logic without requiring full pipeline execution.
  /// </para>
  /// <para>
  /// In production code, nodes should only be executed through the pipeline executor.
  /// This method bypasses pipeline orchestration, dependency resolution, and catalog I/O.
  /// </para>
  /// </remarks>
  [EditorBrowsable(EditorBrowsableState.Never)]
  public Task<IEnumerable<TOutput>> TestTransform(IEnumerable<TInput> input)
  {
    return Transform(input);
  }

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

/// <summary>
/// Convenience base class for nodes that don't require parameters.
/// Equivalent to NodeBase&lt;TInput, TOutput, NoParams&gt;.
/// </summary>
/// <typeparam name="TInput">
/// <para>
/// The <strong>individual item type</strong> for this node's input, NOT a collection type.
/// </para>
/// <para>
/// For simple nodes: Use the item type directly (e.g., CompanySchema).
/// For multi-input nodes: Use a schema type (e.g., TrainModelInputs).
/// </para>
/// <para>
/// See NodeBase&lt;TInput, TOutput, TParameters&gt; for detailed type parameter semantics.
/// </para>
/// </typeparam>
/// <typeparam name="TOutput">
/// <para>
/// The <strong>individual item type</strong> for this node's output, NOT a collection type.
/// </para>
/// <para>
/// For simple nodes: Use the item type directly (e.g., ProcessedData).
/// For multi-output nodes: Use a schema type (e.g., SplitDataOutputs).
/// </para>
/// <para>
/// See NodeBase&lt;TInput, TOutput, TParameters&gt; for detailed type parameter semantics.
/// </para>
/// </typeparam>
/// <remarks>
/// <para>
/// This is the recommended base class for stateless transformation nodes that don't
/// need configuration parameters. For nodes that require parameters, use the
/// three-parameter version: NodeBase&lt;TInput, TOutput, TParameters&gt;.
/// </para>
/// <para>
/// <strong>Usage Example:</strong>
/// </para>
/// <code>
/// public class PreprocessCompaniesNode : NodeBase&lt;CompanyRawSchema, CompanySchema&gt;
/// {
///     protected override Task&lt;IEnumerable&lt;CompanySchema&gt;&gt; Transform(
///         IEnumerable&lt;CompanyRawSchema&gt; input)
///     {
///         var processed = input.Select(company =&gt; new CompanySchema
///         {
///             Id = company.Id,
///             CompanyRating = ParsePercentage(company.CompanyRating),
///             // ...
///         });
///         
///         return Task.FromResult(processed);
///     }
/// }
/// </code>
/// </remarks>
public abstract class NodeBase<TInput, TOutput> : NodeBase<TInput, TOutput, NoParams>
{
  // Inherits all functionality from three-parameter base class
  // Parameters property will be of type NoParams (empty marker)
}
