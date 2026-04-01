using Flowthru.Data;
using Flowthru.Nodes;

namespace Flowthru.Pipelines;

/// <summary>
/// Fluent builder for constructing type-safe data pipelines with function-based nodes.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Function-Based Design (v0.5.0):</strong>
/// Nodes are pure transformation functions with compile-time type safety.
/// Both synchronous and asynchronous functions are supported:
/// - Sync: Func&lt;TInput, TOutput&gt;
/// - Async: Func&lt;TInput, Task&lt;TOutput&gt;&gt;
/// - Multi-input: Func&lt;(TIn1, TIn2, ...), TOutput&gt; or Task&lt;TOutput&gt;
/// - Multi-output: Func&lt;TInput, (TOut1, TOut2, ...)&gt; or Task&lt;(TOut1, TOut2, ...)&gt;
/// </para>
/// <para>
/// Use synchronous functions for pure data transformations. Use asynchronous functions
/// only when your node performs I/O operations (external APIs, databases, etc.).
/// </para>
/// <para>
/// The compiler infers all types from function signatures and validates catalog entry
/// types at pipeline construction time, catching type mismatches before execution.
/// </para>
/// <para>
/// <strong>Usage Patterns:</strong>
/// </para>
/// <code>
/// var pipeline = PipelineBuilder.CreatePipeline(builder =>
/// {
///     // Simple synchronous node
///     builder.AddNode(
///         name: "Preprocess",
///         transform: PreprocessNode.Create(),
///         input: catalog.RawData,
///         output: catalog.ProcessedData
///     );
///
///     // Multi-input node: tuple → single output
///     builder.AddNode(
///         name: "TrainModel",
///         transform: TrainModelNode.Create(),
///         input: (catalog.XTrain, catalog.YTrain),
///         output: catalog.Model
///     );
///
///     // Multi-output node: single input → tuple
///     builder.AddNode(
///         name: "SplitData",
///         transform: SplitDataNode.Create(),
///         input: catalog.Data,
///         output: (catalog.XTrain, catalog.XTest, catalog.YTrain, catalog.YTest)
///     );
///
///     // Asynchronous node (only when needed for I/O)
///     builder.AddNode(
///         name: "FetchExternalData",
///         transform: ExternalDataNode.Create(),
///         input: catalog.Config,
///         output: catalog.ExternalData
///     );
/// });
///
/// pipeline.Build();
/// await pipeline.ExecuteAsync();
/// </code>
/// </remarks>
public partial class PipelineBuilder
{
  /// <summary>
  /// Maximum number of inputs supported by generated AddNode overloads.
  /// </summary>
  internal const int MaxInputs = 8;

  /// <summary>
  /// Maximum number of outputs supported by generated AddNode overloads.
  /// </summary>
  internal const int MaxOutputs = 8;

  private readonly Pipeline _pipeline = new();

  /// <summary>
  /// Creates and configures a new pipeline using the builder pattern.
  /// </summary>
  /// <param name="configure">Action to configure the pipeline by adding nodes</param>
  /// <returns>Configured but not yet built pipeline</returns>
  public static Pipeline CreatePipeline(Action<PipelineBuilder> configure)
  {
    var builder = new PipelineBuilder();
    configure(builder);
    return builder._pipeline;
  }

  /// <summary>
  /// Adds a node with single input and single output (asynchronous transformation).
  /// All types are inferred from the transformation function signature.
  /// </summary>
  /// <typeparam name="TInput">Input type (inferred from transform)</typeparam>
  /// <typeparam name="TOutput">Output type (inferred from transform)</typeparam>
  /// <param name="label">Unique identifier for this node</param>
  /// <param name="transform">Asynchronous transformation function from input to output</param>
  /// <param name="input">Catalog entry providing input data</param>
  /// <param name="output">Catalog entry to store output data</param>
  /// <returns>This builder for method chaining</returns>
  public PipelineBuilder AddNode<TInput, TOutput>(
    string label,
    Func<TInput, Task<TOutput>> transform,
    ICatalogEntry<TInput> input,
    ICatalogEntry<TOutput> output,
    string description = ""
  )
  {
    var pipelineNode = new PipelineNode(
      label: label,
      description: description,
      node: transform,
      inputs: new List<ICatalogEntry> { input },
      outputs: new List<ICatalogEntry> { output }
    );

    _pipeline.AddNode(pipelineNode);
    return this;
  }

  /// <summary>
  /// Adds a node with single input and single output (asynchronous transformation with cancellation support).
  /// All types are inferred from the transformation function signature.
  /// </summary>
  /// <typeparam name="TInput">Input type (inferred from transform)</typeparam>
  /// <typeparam name="TOutput">Output type (inferred from transform)</typeparam>
  /// <param name="label">Unique identifier for this node</param>
  /// <param name="transform">Asynchronous transformation function from input to output with cancellation token</param>
  /// <param name="input">Catalog entry providing input data</param>
  /// <param name="output">Catalog entry to store output data</param>
  /// <returns>This builder for method chaining</returns>
  public PipelineBuilder AddNode<TInput, TOutput>(
    string label,
    Func<TInput, CancellationToken, Task<TOutput>> transform,
    ICatalogEntry<TInput> input,
    ICatalogEntry<TOutput> output,
    string description = ""
  )
  {
    var pipelineNode = new PipelineNode(
      label: label,
      description: description,
      node: transform,
      inputs: new List<ICatalogEntry> { input },
      outputs: new List<ICatalogEntry> { output }
    );

    _pipeline.AddNode(pipelineNode);
    return this;
  }

  /// <summary>
  /// Adds a node with single input and single output (synchronous transformation).
  /// All types are inferred from the transformation function signature.
  /// </summary>
  /// <typeparam name="TInput">Input type (inferred from transform)</typeparam>
  /// <typeparam name="TOutput">Output type (inferred from transform)</typeparam>
  /// <param name="label">Unique identifier for this node</param>
  /// <param name="transform">Synchronous transformation function from input to output</param>
  /// <param name="input">Catalog entry providing input data</param>
  /// <param name="output">Catalog entry to store output data</param>
  /// <returns>This builder for method chaining</returns>
  public PipelineBuilder AddNode<TInput, TOutput>(
    string label,
    Func<TInput, TOutput> transform,
    ICatalogEntry<TInput> input,
    ICatalogEntry<TOutput> output,
    string description = ""
  )
  {
    var pipelineNode = new PipelineNode(
      label: label,
      description: description,
      node: transform,
      inputs: new List<ICatalogEntry> { input },
      outputs: new List<ICatalogEntry> { output }
    );

    _pipeline.AddNode(pipelineNode);
    return this;
  }

  // Additional overloads (2-8 inputs, 1-8 outputs) are auto-generated via source generator.
  // See: Flowthru.SourceGenerators/PipelineBuilderGenerator.cs

  /// <summary>
  /// Adds a homogeneous fan-in node: N catalog entries of the same element type collapse
  /// into a single node whose transform receives all N loaded collections as a typed list.
  /// </summary>
  /// <typeparam name="TIn">Element type of each input catalog entry</typeparam>
  /// <typeparam name="TOut">Output type produced by the transform</typeparam>
  /// <param name="label">Unique identifier for this node</param>
  /// <param name="inputs">Variable-length list of same-typed input entries</param>
  /// <param name="output">Catalog entry to store the merged result</param>
  /// <param name="node">Transform function receiving all N loaded values as a typed read-only list</param>
  /// <param name="description">Optional human-readable description</param>
  /// <returns>This builder for method chaining</returns>
  /// <remarks>
  /// Use this when the number of inputs is not known at compile time — for example,
  /// aggregating per-partition catalogs constructed in a loop. The function receives
  /// <c>IReadOnlyList&lt;TIn&gt;</c> where each element corresponds to one input entry
  /// in declaration order. An empty inputs list is allowed but produces an empty list argument.
  /// </remarks>
  public PipelineBuilder AddNode<TIn, TOut>(
    string label,
    IReadOnlyList<ICatalogEntry<TIn>> inputs,
    ICatalogEntry<TOut> output,
    Func<IReadOnlyList<TIn>, TOut> node,
    string description = ""
  )
  {
    if (inputs == null)
      throw new ArgumentNullException(nameof(inputs));
    if (output == null)
      throw new ArgumentNullException(nameof(output));
    if (node == null)
      throw new ArgumentNullException(nameof(node));

    var capturedNode = node;
    // Wrap into Func<object[], TOut>. The executor uses the parameter type as a signal:
    // when it is object[], it passes the loaded inputs array directly rather than building
    // a ValueTuple. inputParameter is declared as object in the executor so DynamicInvoke
    // receives new object[]{ inputParameter } — no array-spreading occurs.
    Func<object[], TOut> wrappedNode = rawInputs =>
      capturedNode(rawInputs.Cast<TIn>().ToList().AsReadOnly());

    var pipelineNode = new PipelineNode(
      label: label,
      description: description,
      node: wrappedNode,
      inputs: inputs.Cast<ICatalogEntry>().ToList(),
      outputs: new List<ICatalogEntry> { output }
    );

    _pipeline.AddNode(pipelineNode);
    return this;
  }
}
