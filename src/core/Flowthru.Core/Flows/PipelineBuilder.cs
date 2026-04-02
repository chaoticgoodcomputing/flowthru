using Flowthru.Data;
using Flowthru.Steps;

namespace Flowthru.Flows;

/// <summary>
/// Fluent builder for constructing type-safe flows with function-based steps.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Function-Based Design (v0.5.0):</strong>
/// Steps are pure transformation functions with compile-time type safety.
/// Both synchronous and asynchronous functions are supported:
/// - Sync: Func&lt;TInput, TOutput&gt;
/// - Async: Func&lt;TInput, Task&lt;TOutput&gt;&gt;
/// - Multi-input: Func&lt;(TIn1, TIn2, ...), TOutput&gt; or Task&lt;TOutput&gt;
/// - Multi-output: Func&lt;TInput, (TOut1, TOut2, ...)&gt; or Task&lt;(TOut1, TOut2, ...)&gt;
/// </para>
/// <para>
/// Use synchronous functions for pure data transformations. Use asynchronous functions
/// only when your step performs I/O operations (external APIs, databases, etc.).
/// </para>
/// <para>
/// The compiler infers all types from function signatures and validates catalog entry
/// types at flow construction time, catching type mismatches before execution.
/// </para>
/// <para>
/// <strong>Usage Patterns:</strong>
/// </para>
/// <code>
/// var flow = FlowBuilder.CreateFlow(builder =>
/// {
///     // Simple synchronous step
///     builder.AddStep(
///         name: "Preprocess",
///         transform: PreprocessStep.Create(),
///         input: catalog.RawData,
///         output: catalog.ProcessedData
///     );
///
///     // Multi-input step: tuple → single output
///     builder.AddStep(
///         name: "TrainModel",
///         transform: TrainModelStep.Create(),
///         input: (catalog.XTrain, catalog.YTrain),
///         output: catalog.Model
///     );
///
///     // Multi-output step: single input → tuple
///     builder.AddStep(
///         name: "SplitData",
///         transform: SplitDataStep.Create(),
///         input: catalog.Data,
///         output: (catalog.XTrain, catalog.XTest, catalog.YTrain, catalog.YTest)
///     );
///
///     // Asynchronous step (only when needed for I/O)
///     builder.AddStep(
///         name: "FetchExternalData",
///         transform: ExternalDataStep.Create(),
///         input: catalog.Config,
///         output: catalog.ExternalData
///     );
/// });
///
/// flow.Build();
/// await flow.ExecuteAsync();
/// </code>
/// </remarks>
public partial class FlowBuilder
{
  /// <summary>
  /// Maximum number of inputs supported by generated AddStep overloads.
  /// </summary>
  internal const int MaxInputs = 8;

  /// <summary>
  /// Maximum number of outputs supported by generated AddStep overloads.
  /// </summary>
  internal const int MaxOutputs = 8;

  private readonly Flow _flow = new();

  /// <summary>
  /// Creates and configures a new flow using the builder pattern.
  /// </summary>
  /// <param name="configure">Action to configure the flow by adding steps</param>
  /// <returns>Configured but not yet built flow</returns>
  public static Flow CreateFlow(Action<FlowBuilder> configure)
  {
    var builder = new FlowBuilder();
    configure(builder);
    return builder._flow;
  }

  /// <summary>
  /// Adds a step with single input and single output (asynchronous transformation).
  /// All types are inferred from the transformation function signature.
  /// </summary>
  /// <typeparam name="TInput">Input type (inferred from transform)</typeparam>
  /// <typeparam name="TOutput">Output type (inferred from transform)</typeparam>
  /// <param name="label">Unique identifier for this step</param>
  /// <param name="transform">Asynchronous transformation function from input to output</param>
  /// <param name="input">Catalog entry providing input data</param>
  /// <param name="output">Catalog entry to store output data</param>
  /// <param name="description">Optional description for this step</param>
  /// <returns>This builder for method chaining</returns>
  public FlowBuilder AddStep<TInput, TOutput>(
    string label,
    Func<TInput, Task<TOutput>> transform,
    IItem<TInput> input,
    IItem<TOutput> output,
    string description = ""
  )
  {
    var flowStep = new FlowStep(
      label: label,
      description: description,
      step: transform,
      inputs: new List<IItem> { input },
      outputs: new List<IItem> { output }
    );

    _flow.AddStep(flowStep);
    return this;
  }

  /// <summary>
  /// Adds a step with single input and single output (asynchronous transformation with cancellation support).
  /// All types are inferred from the transformation function signature.
  /// </summary>
  /// <typeparam name="TInput">Input type (inferred from transform)</typeparam>
  /// <typeparam name="TOutput">Output type (inferred from transform)</typeparam>
  /// <param name="label">Unique identifier for this step</param>
  /// <param name="transform">Asynchronous transformation function from input to output with cancellation token</param>
  /// <param name="input">Catalog entry providing input data</param>
  /// <param name="output">Catalog entry to store output data</param>
  /// <param name="description">Optional description for this step</param>
  /// <returns>This builder for method chaining</returns>
  public FlowBuilder AddStep<TInput, TOutput>(
    string label,
    Func<TInput, CancellationToken, Task<TOutput>> transform,
    IItem<TInput> input,
    IItem<TOutput> output,
    string description = ""
  )
  {
    var flowStep = new FlowStep(
      label: label,
      description: description,
      step: transform,
      inputs: new List<IItem> { input },
      outputs: new List<IItem> { output }
    );

    _flow.AddStep(flowStep);
    return this;
  }

  /// <summary>
  /// Adds a step with single input and single output (synchronous transformation).
  /// All types are inferred from the transformation function signature.
  /// </summary>
  /// <typeparam name="TInput">Input type (inferred from transform)</typeparam>
  /// <typeparam name="TOutput">Output type (inferred from transform)</typeparam>
  /// <param name="label">Unique identifier for this step</param>
  /// <param name="transform">Synchronous transformation function from input to output</param>
  /// <param name="input">Catalog entry providing input data</param>
  /// <param name="output">Catalog entry to store output data</param>
  /// <param name="description">Optional description for this step</param>
  /// <returns>This builder for method chaining</returns>
  public FlowBuilder AddStep<TInput, TOutput>(
    string label,
    Func<TInput, TOutput> transform,
    IItem<TInput> input,
    IItem<TOutput> output,
    string description = ""
  )
  {
    var flowStep = new FlowStep(
      label: label,
      description: description,
      step: transform,
      inputs: new List<IItem> { input },
      outputs: new List<IItem> { output }
    );

    _flow.AddStep(flowStep);
    return this;
  }

  // Additional overloads (2-8 inputs, 1-8 outputs) are auto-generated via source generator.
  // See: Flowthru.SourceGenerators/FlowBuilderGenerator.cs

  /// <summary>
  /// Adds a homogeneous fan-in step: N catalog entries of the same element type collapse
  /// into a single step whose transform receives all N loaded collections as a typed list.
  /// </summary>
  /// <typeparam name="TIn">Element type of each input catalog entry</typeparam>
  /// <typeparam name="TOut">Output type produced by the transform</typeparam>
  /// <param name="label">Unique identifier for this step</param>
  /// <param name="inputs">Variable-length list of same-typed input entries</param>
  /// <param name="output">Catalog entry to store the merged result</param>
  /// <param name="step">Transform function receiving all N loaded values as a typed read-only list</param>
  /// <param name="description">Optional human-readable description</param>
  /// <returns>This builder for method chaining</returns>
  /// <remarks>
  /// Use this when the number of inputs is not known at compile time — for example,
  /// aggregating per-partition catalogs constructed in a loop. The function receives
  /// <c>IReadOnlyList&lt;TIn&gt;</c> where each element corresponds to one input entry
  /// in declaration order. An empty inputs list is allowed but produces an empty list argument.
  /// </remarks>
  public FlowBuilder AddStep<TIn, TOut>(
    string label,
    IReadOnlyList<IItem<TIn>> inputs,
    IItem<TOut> output,
    Func<IReadOnlyList<TIn>, TOut> step,
    string description = ""
  )
  {
    if (inputs == null)
      throw new ArgumentNullException(nameof(inputs));
    if (output == null)
      throw new ArgumentNullException(nameof(output));
    if (step == null)
      throw new ArgumentNullException(nameof(step));

    var capturedStep = step;
    // Wrap into Func<object[], TOut>. The executor uses the parameter type as a signal:
    // when it is object[], it passes the loaded inputs array directly rather than building
    // a ValueTuple. inputParameter is declared as object in the executor so DynamicInvoke
    // receives new object[]{ inputParameter } — no array-spreading occurs.
    Func<object[], TOut> wrappedStep = rawInputs =>
      capturedStep(rawInputs.Cast<TIn>().ToList().AsReadOnly());

    var flowStep = new FlowStep(
      label: label,
      description: description,
      step: wrappedStep,
      inputs: inputs.Cast<IItem>().ToList(),
      outputs: new List<IItem> { output }
    );

    _flow.AddStep(flowStep);
    return this;
  }
}
