using System.ComponentModel;
using Flowthru.Data.Catalog;
using Flowthru.Prelude;
using Flowthru.Step;

namespace Flowthru.Flow;

/// <summary>
/// Variadic-input <c>AddStep</c> overloads — the (N×1) row in the AddStep
/// matrix where N is runtime-determined and all inputs share a type. Pure
/// reduce / fan-in shape: collapse <em>N</em> homogeneous catalog items
/// into a single output.
/// </summary>
/// <remarks>
/// <para>
/// The fixed-arity AddStep overloads emitted by
/// <c>FlowBuilderGenerator</c> handle <em>heterogeneous-shape</em> joins
/// (one Customer record AND one Shuttle record AND one Review record);
/// arity is part of the step's identity. This overload handles the
/// distinct case of <em>homogeneous-shape</em> reduction (N records of
/// the same shape, concatenated or aggregated); arity is incidental —
/// the step's identity is the operation, not the count.
/// </para>
/// <para>
/// Engine-internally there is nothing special: <see cref="IStepNode.Inputs"/>
/// is already <c>IReadOnlyList&lt;IItem&gt;</c> and <see cref="Step{TIn, TOut}"/>
/// is arity-agnostic. The hand-written closures here just sequence each
/// input's <c>Load()</c> in order via repeated <c>Bind</c>, exactly the
/// way the source-generated typed overloads sequence their fixed-arity
/// inputs — only the loop is runtime instead of unrolled.
/// </para>
/// <para>
/// <strong>Phase 8 codeVersion.</strong> Each visible overload below
/// resolves <c>CodeVersion</c> automatically via
/// <see cref="StepMetadataResolver"/> — Flow developers do not pass it
/// by hand. A hidden, <see cref="EditorBrowsableAttribute"/>-decorated
/// twin overload accepts an explicit <c>codeVersion</c> argument for
/// power-user overrides (the same shape as the generated typed
/// matrix). Power users discover the hidden overloads by passing
/// <c>codeVersion:</c> as a named argument; overload resolution
/// distinguishes them by the presence of that parameter.
/// </para>
/// </remarks>
public sealed partial class FlowBuilder
{
  /// <summary>
  /// Add a synchronous reduce step: <em>N</em> typed inputs of the same
  /// shape collapse into one output via the user-supplied transform.
  /// </summary>
  /// <typeparam name="TIn">Per-input element type.</typeparam>
  /// <typeparam name="TOut">Output type.</typeparam>
  /// <param name="label">Unique step label within the flow.</param>
  /// <param name="transform">
  /// Reduction over the loaded inputs. Receives them in the order
  /// declared by <paramref name="inputs"/>.
  /// </param>
  /// <param name="inputs">
  /// Catalog items to reduce over. Order is preserved into the
  /// transform's <see cref="IEnumerable{T}"/> argument.
  /// </param>
  /// <param name="outputs">Catalog item receiving the reduction's result.</param>
  public FlowBuilder AddStep<TIn, TOut>(
    string label,
    Func<IEnumerable<TIn>, TOut> transform,
    IReadOnlyList<IItem<TIn>> inputs,
    IItem<TOut> outputs
  )
  {
    if (label is null) throw new ArgumentNullException(nameof(label));
    if (transform is null) throw new ArgumentNullException(nameof(transform));
    if (inputs is null) throw new ArgumentNullException(nameof(inputs));
    if (outputs is null) throw new ArgumentNullException(nameof(outputs));

    return AddVariadicStep(
      label,
      input => FlowIO.Lift(() => transform(input), source: "step:" + label),
      inputs,
      outputs,
      StepMetadataResolver.ResolveFromDelegate(transform)
    );
  }

  /// <summary>
  /// Add an asynchronous reduce step. Same shape as the synchronous
  /// overload; the transform returns a <see cref="Task{TOut}"/>.
  /// </summary>
  public FlowBuilder AddStep<TIn, TOut>(
    string label,
    Func<IEnumerable<TIn>, Task<TOut>> transform,
    IReadOnlyList<IItem<TIn>> inputs,
    IItem<TOut> outputs
  )
  {
    if (label is null) throw new ArgumentNullException(nameof(label));
    if (transform is null) throw new ArgumentNullException(nameof(transform));
    if (inputs is null) throw new ArgumentNullException(nameof(inputs));
    if (outputs is null) throw new ArgumentNullException(nameof(outputs));

    return AddVariadicStep(
      label,
      input => FlowIO.LiftAsync(_ => transform(input), source: "step:" + label),
      inputs,
      outputs,
      StepMetadataResolver.ResolveFromDelegate(transform)
    );
  }

  /// <summary>
  /// Add an asynchronous, cancellable reduce step.
  /// </summary>
  public FlowBuilder AddStep<TIn, TOut>(
    string label,
    Func<IEnumerable<TIn>, CancellationToken, Task<TOut>> transform,
    IReadOnlyList<IItem<TIn>> inputs,
    IItem<TOut> outputs
  )
  {
    if (label is null) throw new ArgumentNullException(nameof(label));
    if (transform is null) throw new ArgumentNullException(nameof(transform));
    if (inputs is null) throw new ArgumentNullException(nameof(inputs));
    if (outputs is null) throw new ArgumentNullException(nameof(outputs));

    return AddVariadicStep(
      label,
      input => FlowIO.LiftAsync(ct => transform(input, ct), source: "step:" + label),
      inputs,
      outputs,
      StepMetadataResolver.ResolveFromDelegate(transform)
    );
  }

  // ── Advanced: explicit codeVersion overrides (hidden from IntelliSense) ─

  /// <summary>
  /// Advanced — synchronous reduce step with an explicit <c>codeVersion</c>
  /// override. Hidden from IntelliSense; surface this only when forcing a
  /// non-source-derived cache identity (e.g., breaking the cache on a
  /// refactor the trivia stripper considers cosmetic, or pinning identity
  /// across builds for a class whose Roslyn-emitted source the source
  /// generator can't see).
  /// </summary>
  [EditorBrowsable(EditorBrowsableState.Never)]
  public FlowBuilder AddStep<TIn, TOut>(
    string label,
    Func<IEnumerable<TIn>, TOut> transform,
    IReadOnlyList<IItem<TIn>> inputs,
    IItem<TOut> outputs,
    string? codeVersion
  )
  {
    if (label is null) throw new ArgumentNullException(nameof(label));
    if (transform is null) throw new ArgumentNullException(nameof(transform));
    if (inputs is null) throw new ArgumentNullException(nameof(inputs));
    if (outputs is null) throw new ArgumentNullException(nameof(outputs));

    return AddVariadicStep(
      label,
      input => FlowIO.Lift(() => transform(input), source: "step:" + label),
      inputs,
      outputs,
      codeVersion
    );
  }

  /// <inheritdoc cref="AddStep{TIn, TOut}(string, Func{IEnumerable{TIn}, TOut}, IReadOnlyList{IItem{TIn}}, IItem{TOut}, string?)"/>
  [EditorBrowsable(EditorBrowsableState.Never)]
  public FlowBuilder AddStep<TIn, TOut>(
    string label,
    Func<IEnumerable<TIn>, Task<TOut>> transform,
    IReadOnlyList<IItem<TIn>> inputs,
    IItem<TOut> outputs,
    string? codeVersion
  )
  {
    if (label is null) throw new ArgumentNullException(nameof(label));
    if (transform is null) throw new ArgumentNullException(nameof(transform));
    if (inputs is null) throw new ArgumentNullException(nameof(inputs));
    if (outputs is null) throw new ArgumentNullException(nameof(outputs));

    return AddVariadicStep(
      label,
      input => FlowIO.LiftAsync(_ => transform(input), source: "step:" + label),
      inputs,
      outputs,
      codeVersion
    );
  }

  /// <inheritdoc cref="AddStep{TIn, TOut}(string, Func{IEnumerable{TIn}, TOut}, IReadOnlyList{IItem{TIn}}, IItem{TOut}, string?)"/>
  [EditorBrowsable(EditorBrowsableState.Never)]
  public FlowBuilder AddStep<TIn, TOut>(
    string label,
    Func<IEnumerable<TIn>, CancellationToken, Task<TOut>> transform,
    IReadOnlyList<IItem<TIn>> inputs,
    IItem<TOut> outputs,
    string? codeVersion
  )
  {
    if (label is null) throw new ArgumentNullException(nameof(label));
    if (transform is null) throw new ArgumentNullException(nameof(transform));
    if (inputs is null) throw new ArgumentNullException(nameof(inputs));
    if (outputs is null) throw new ArgumentNullException(nameof(outputs));

    return AddVariadicStep(
      label,
      input => FlowIO.LiftAsync(ct => transform(input, ct), source: "step:" + label),
      inputs,
      outputs,
      codeVersion
    );
  }

  // ── Shared body ─────────────────────────────────────────────────────

  private FlowBuilder AddVariadicStep<TIn, TOut>(
    string label,
    Func<IEnumerable<TIn>, FlowIO<TOut>> transformIO,
    IReadOnlyList<IItem<TIn>> inputs,
    IItem<TOut> outputs,
    string? codeVersion = null
  )
  {
    // Snapshot the input list so closures don't see later mutations.
    var inputsArr = new IItem<TIn>[inputs.Count];
    for (var i = 0; i < inputs.Count; i++)
    {
      var item = inputs[i] ?? throw new ArgumentException(
        $"Variadic AddStep '{label}': inputs[{i}] is null.", nameof(inputs));
      inputsArr[i] = item;
    }

    // loadInputs: sequence each item.Load() through Bind, accumulating
    // values in a list. The fold is the runtime-arity equivalent of the
    // unrolled `input1.Load().Bind(v1 => input2.Load().Bind(v2 => …))`
    // chain emitted by the typed AddStep generator.
    Func<FlowIO<IEnumerable<TIn>>> loadInputs = () =>
    {
      var acc = FlowIO.Pure<List<TIn>>(new List<TIn>(inputsArr.Length));
      foreach (var item in inputsArr)
      {
        var captured = item;
        acc = acc.Bind(list => captured.Load().Map<List<TIn>>(value =>
        {
          list.Add(value);
          return list;
        }));
      }
      return acc.Map<IEnumerable<TIn>>(list => list);
    };

    // saveOutputs: single-output, identical to the typed (N, 1) cell.
    var capturedOutput = outputs;
    Func<TOut, FlowIO<FlowUnit>> saveOutputs = output => capturedOutput.Save(output);

    return Add(new Step<IEnumerable<TIn>, TOut>(
      label: label,
      transform: transformIO,
      inputs: inputsArr.Cast<IItem>().ToArray(),
      outputs: new IItem[] { outputs },
      loadInputs: loadInputs,
      saveOutputs: saveOutputs,
      flowLabel: this.Label,
      codeVersion: codeVersion
    ));
  }
}
