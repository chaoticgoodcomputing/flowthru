namespace Flowthru.Core.Results;

/// <summary>
/// Marker exception that wraps any failure which escapes the normal
/// <see cref="Flows.FlowResult"/> contract — i.e., a runtime failure that
/// surfaced as a thrown exception rather than a structured step failure.
/// </summary>
/// <remarks>
/// <para>
/// Flowthru's design invariant (see <c>CONTRIBUTING.md</c>) is that a flow which
/// passes pre-flight checks should always complete successfully, with any
/// step failure captured in <see cref="Flows.FlowResult"/>. When something
/// throws past that boundary — for example, an internal cancellation cascade
/// that leaks out of the executor before partial step results can be
/// gathered — that's an unexpected escape of the contract and almost always
/// indicates a Flowthru framework bug.
/// </para>
/// <para>
/// Wrapping such a failure in <c>FlowExecutionEscapedException</c> lets the
/// runtime-error reporting pipeline classify it correctly as
/// <see cref="ErrorClassification.PossibleFrameworkBug"/> regardless of the
/// inner exception's type — including allowlisted types like
/// <see cref="OperationCanceledException"/> that would otherwise be mistaken
/// for environmental failures.
/// </para>
/// </remarks>
public sealed class FlowExecutionEscapedException : Exception
{
  /// <summary>
  /// Creates a new <see cref="FlowExecutionEscapedException"/> wrapping an
  /// underlying failure that escaped the FlowResult contract.
  /// </summary>
  /// <param name="message">Human-readable description of the escape.</param>
  /// <param name="innerException">The original failure being wrapped.</param>
  public FlowExecutionEscapedException(string message, Exception innerException)
    : base(message, innerException) { }
}
