using Flowthru.Core.Data.Validation;
using Flowthru.Core.Effects;
using Flowthru.Core.Graph;

namespace Flowthru.Tests.Helpers.Effects;

/// <summary>
/// Test-only <see cref="IEffect{T}"/> implementation backed by an in-memory recorder rather
/// than a real network call. Lets any test wire up an effect node into a flow to validate
/// engine-level behavior (untyped dispatch, bridge DIM dispatch through <c>INode&lt;T&gt;</c>)
/// without flaky network dependencies.
/// </summary>
/// <remarks>
/// <para>
/// Each call to <see cref="Execute"/> increments <see cref="InvocationCount"/> and appends
/// the configured <c>Result</c> to <see cref="Invocations"/>. Tests can pre-configure
/// the effect to fail (via constructor) or succeed with a specific value, then assert the
/// invocation history after running the flow.
/// </para>
/// <para>
/// The default <see cref="EffectTraits"/> declare the effect as having side effects and
/// being non-idempotent — matching a real webhook's typical shape. Override via the
/// <c>traits</c> constructor parameter to model retryable / idempotent endpoints.
/// </para>
/// </remarks>
public sealed class FakeWebhookEffect<T> : IEffect<T>
{
  private readonly Func<T> _result;
  private readonly Func<T, ValidationResult>? _validateResult;
  private readonly List<T> _invocations = new();
  private readonly List<T> _consumed = new();

  /// <summary>
  /// Creates a fake effect that returns <paramref name="result"/> from each
  /// <see cref="Execute"/> invocation. Optionally configure custom traits or a validation
  /// function used by <see cref="INode.Validate"/>.
  /// </summary>
  public FakeWebhookEffect(
    T result,
    string? label = null,
    EffectTraits? traits = null,
    Func<T, ValidationResult>? validateResult = null
  )
    : this(() => result, label, traits, validateResult) { }

  /// <summary>
  /// Creates a fake effect that produces a fresh result via <paramref name="resultFactory"/>
  /// on each invocation. Useful when the test needs distinct values per call (timestamps,
  /// counters, etc.).
  /// </summary>
  public FakeWebhookEffect(
    Func<T> resultFactory,
    string? label = null,
    EffectTraits? traits = null,
    Func<T, ValidationResult>? validateResult = null
  )
  {
    _result = resultFactory ?? throw new ArgumentNullException(nameof(resultFactory));
    Label = label ?? $"fake-webhook-{Guid.NewGuid():N}";
    EffectTraits =
      traits
      ?? new EffectTraits
      {
        CanInspect = true,
        IsIdempotent = false,
        HasSideEffects = true,
      };
    _validateResult = validateResult;
  }

  /// <summary>Number of times <see cref="Execute"/> has been invoked.</summary>
  public int InvocationCount => _invocations.Count;

  /// <summary>
  /// Ordered history of result values returned from <see cref="Execute"/>. Tests can assert
  /// on the count and content to verify the engine invoked the effect as expected.
  /// </summary>
  public IReadOnlyList<T> Invocations => _invocations.AsReadOnly();

  /// <summary>
  /// Ordered history of payloads sent to <see cref="Consume"/>. Tests can assert on these
  /// when the effect is wired as a step output (consumer) rather than producer.
  /// </summary>
  public IReadOnlyList<T> ConsumedPayloads => _consumed.AsReadOnly();

  /// <inheritdoc/>
  public string Label { get; }

  /// <inheritdoc/>
  public Type DataType => typeof(T);

  /// <inheritdoc/>
  public EffectTraits EffectTraits { get; }

  /// <inheritdoc/>
  public FlowIO<T> Execute() =>
    FlowIO.Lift(() =>
    {
      var value = _result();
      _invocations.Add(value);
      return value;
    });

  /// <inheritdoc/>
  public FlowIO<FlowUnit> Consume(T data) =>
    FlowIO.Lift(() =>
    {
      _consumed.Add(data);
      return FlowUnit.Default;
    });

  /// <summary>
  /// <see cref="INode.Validate"/> override. Returns the configured validation function's
  /// output, or trivially-successful when no function was provided. Lets tests exercise
  /// pre-flight validation paths without needing a real reachability probe.
  /// </summary>
  public FlowIO<ValidationResult> Validate()
  {
    if (_validateResult is null)
    {
      return FlowIO.Pure(ValidationResult.Success());
    }
    var sample = _result();
    return FlowIO.Pure(_validateResult(sample));
  }
}
