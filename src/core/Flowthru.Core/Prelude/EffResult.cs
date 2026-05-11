namespace Flowthru.Prelude;

/// <summary>
/// Result of running a <see cref="FlowIO{A}"/>. Closed sum: either the
/// effect produced a value (<see cref="Success"/>) or it failed with a
/// <see cref="RuntimeError"/> (<see cref="Failure"/>).
/// </summary>
/// <remarks>
/// Failures are values, not exceptions. Consumers pattern-match on the
/// closed sum to distinguish success from each runtime-error variant.
/// </remarks>
public abstract record EffResult<A>
{
  private EffResult() { }

  public sealed record Success(A Value) : EffResult<A>;

  public sealed record Failure(RuntimeError Error) : EffResult<A>;

  /// <summary>True if this is a <see cref="Success"/>.</summary>
  public bool IsSuccess => this is Success;

  /// <summary>True if this is a <see cref="Failure"/>.</summary>
  public bool IsFailure => this is Failure;

  /// <summary>
  /// Terminal pattern match. Use this to consume an EffResult at the host
  /// boundary where you must collapse the sum into a single result type.
  /// </summary>
  public TResult Match<TResult>(
    Func<A, TResult> onSuccess,
    Func<RuntimeError, TResult> onFailure
  ) =>
    this switch
    {
      Success s => onSuccess(s.Value),
      Failure f => onFailure(f.Error),
      _ => throw new InvalidOperationException("Unreachable: EffResult is a closed sum"),
    };
}
