using Flowthru.Data;
using Flowthru.Nodes;

namespace Flowthru.Pipelines;

/// <summary>
/// Fluent builder for constructing type-safe data pipelines with function-based nodes.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Function-Based Design (v0.5.0):</strong>
/// Nodes are pure transformation functions with compile-time type safety:
/// - Simple: Func&lt;TInput, Task&lt;TOutput&gt;&gt;
/// - Multi-input: Func&lt;(TIn1, TIn2, ...), Task&lt;TOutput&gt;&gt;
/// - Multi-output: Func&lt;TInput, Task&lt;(TOut1, TOut2, ...)&gt;&gt;
/// - Multi-input-output: Func&lt;(TIn1, TIn2), Task&lt;(TOut1, TOut2)&gt;&gt;
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
///     // Simple node: single input → single output
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
/// });
/// 
/// pipeline.Build();
/// await pipeline.ExecuteAsync();
/// </code>
/// </remarks>
public class PipelineBuilder {
  private readonly Pipeline _pipeline = new();

  /// <summary>
  /// Creates and configures a new pipeline using the builder pattern.
  /// </summary>
  /// <param name="configure">Action to configure the pipeline by adding nodes</param>
  /// <returns>Configured but not yet built pipeline</returns>
  public static Pipeline CreatePipeline(Action<PipelineBuilder> configure) {
    var builder = new PipelineBuilder();
    configure(builder);
    return builder._pipeline;
  }

  /// <summary>
  /// Adds a node with single input and single output.
  /// All types are inferred from the transformation function signature.
  /// </summary>
  /// <typeparam name="TInput">Input type (inferred from transform)</typeparam>
  /// <typeparam name="TOutput">Output type (inferred from transform)</typeparam>
  /// <param name="name">Unique identifier for this node</param>
  /// <param name="transform">Transformation function from input to output</param>
  /// <param name="input">Catalog entry providing input data</param>
  /// <param name="output">Catalog entry to store output data</param>
  /// <returns>This builder for method chaining</returns>
  public PipelineBuilder AddNode<TInput, TOutput>(
    string name,
    Func<TInput, Task<TOutput>> transform,
    ICatalogEntry<TInput> input,
    ICatalogEntry<TOutput> output
  ) {
    var pipelineNode = new PipelineNode(
      name: name,
      transformFunction: transform,
      inputs: new List<ICatalogEntry> { input },
      outputs: new List<ICatalogEntry> { output }
    );

    _pipeline.AddNode(pipelineNode);
    return this;
  }

  /// <summary>
  /// Adds a node with two inputs and single output.
  /// </summary>
  public PipelineBuilder AddNode<TIn1, TIn2, TOut>(
    string name,
    Func<(TIn1, TIn2), Task<TOut>> transform,
    (ICatalogEntry<TIn1>, ICatalogEntry<TIn2>) input,
    ICatalogEntry<TOut> output
  ) {
    var (input1, input2) = input;

    var pipelineNode = new PipelineNode(
      name: name,
      transformFunction: transform,
      inputs: new List<ICatalogEntry> { input1, input2 },
      outputs: new List<ICatalogEntry> { output }
    );

    _pipeline.AddNode(pipelineNode);
    return this;
  }

  /// <summary>
  /// Adds a node with three inputs and single output.
  /// </summary>
  public PipelineBuilder AddNode<TIn1, TIn2, TIn3, TOut>(
    string name,
    Func<(TIn1, TIn2, TIn3), Task<TOut>> transform,
    (ICatalogEntry<TIn1>, ICatalogEntry<TIn2>, ICatalogEntry<TIn3>) input,
    ICatalogEntry<TOut> output
  ) {
    var (input1, input2, input3) = input;

    var pipelineNode = new PipelineNode(
      name: name,
      transformFunction: transform,
      inputs: new List<ICatalogEntry> { input1, input2, input3 },
      outputs: new List<ICatalogEntry> { output }
    );

    _pipeline.AddNode(pipelineNode);
    return this;
  }

  /// <summary>
  /// Adds a node with four inputs and single output.
  /// </summary>
  public PipelineBuilder AddNode<TIn1, TIn2, TIn3, TIn4, TOut>(
    string name,
    Func<(TIn1, TIn2, TIn3, TIn4), Task<TOut>> transform,
    (ICatalogEntry<TIn1>, ICatalogEntry<TIn2>, ICatalogEntry<TIn3>, ICatalogEntry<TIn4>) input,
    ICatalogEntry<TOut> output
  ) {
    var (input1, input2, input3, input4) = input;

    var pipelineNode = new PipelineNode(
      name: name,
      transformFunction: transform,
      inputs: new List<ICatalogEntry> { input1, input2, input3, input4 },
      outputs: new List<ICatalogEntry> { output }
    );

    _pipeline.AddNode(pipelineNode);
    return this;
  }

  /// <summary>
  /// Adds a node with single input and two outputs.
  /// </summary>
  public PipelineBuilder AddNode<TIn, TOut1, TOut2>(
    string name,
    Func<TIn, Task<(TOut1, TOut2)>> transform,
    ICatalogEntry<TIn> input,
    (ICatalogEntry<TOut1>, ICatalogEntry<TOut2>) output
  ) {
    var (output1, output2) = output;

    var pipelineNode = new PipelineNode(
      name: name,
      transformFunction: transform,
      inputs: new List<ICatalogEntry> { input },
      outputs: new List<ICatalogEntry> { output1, output2 }
    );

    _pipeline.AddNode(pipelineNode);
    return this;
  }

  /// <summary>
  /// Adds a node with single input and three outputs.
  /// </summary>
  public PipelineBuilder AddNode<TIn, TOut1, TOut2, TOut3>(
    string name,
    Func<TIn, Task<(TOut1, TOut2, TOut3)>> transform,
    ICatalogEntry<TIn> input,
    (ICatalogEntry<TOut1>, ICatalogEntry<TOut2>, ICatalogEntry<TOut3>) output
  ) {
    var (output1, output2, output3) = output;

    var pipelineNode = new PipelineNode(
      name: name,
      transformFunction: transform,
      inputs: new List<ICatalogEntry> { input },
      outputs: new List<ICatalogEntry> { output1, output2, output3 }
    );

    _pipeline.AddNode(pipelineNode);
    return this;
  }

  /// <summary>
  /// Adds a node with single input and four outputs.
  /// </summary>
  public PipelineBuilder AddNode<TIn, TOut1, TOut2, TOut3, TOut4>(
    string name,
    Func<TIn, Task<(TOut1, TOut2, TOut3, TOut4)>> transform,
    ICatalogEntry<TIn> input,
    (ICatalogEntry<TOut1>, ICatalogEntry<TOut2>, ICatalogEntry<TOut3>, ICatalogEntry<TOut4>) output
  ) {
    var (output1, output2, output3, output4) = output;

    var pipelineNode = new PipelineNode(
      name: name,
      transformFunction: transform,
      inputs: new List<ICatalogEntry> { input },
      outputs: new List<ICatalogEntry> { output1, output2, output3, output4 }
    );

    _pipeline.AddNode(pipelineNode);
    return this;
  }

  /// <summary>
  /// Adds a node with three inputs and two outputs.
  /// </summary>
  public PipelineBuilder AddNode<TIn1, TIn2, TIn3, TOut1, TOut2>(
    string name,
    Func<(TIn1, TIn2, TIn3), Task<(TOut1, TOut2)>> transform,
    (ICatalogEntry<TIn1>, ICatalogEntry<TIn2>, ICatalogEntry<TIn3>) input,
    (ICatalogEntry<TOut1>, ICatalogEntry<TOut2>) output
  ) {
    var (input1, input2, input3) = input;
    var (output1, output2) = output;

    var pipelineNode = new PipelineNode(
      name: name,
      transformFunction: transform,
      inputs: new List<ICatalogEntry> { input1, input2, input3 },
      outputs: new List<ICatalogEntry> { output1, output2 }
    );

    _pipeline.AddNode(pipelineNode);
    return this;
  }
}

