using Flowthru.Prelude;
using Flowthru.Validation.PreFlight;

namespace Flowthru.Validation.Runtime;

/// <summary>
/// Opaque result a service inspector returns to Flowthru's pre-flight
/// pipeline. Constructed only via <see cref="Inspect.Pass"/>,
/// <see cref="Inspect.Fail(string, string?)"/>, or
/// <see cref="Inspect.FailIf"/>; the user never directly handles
/// <see cref="Validated{TError, TValue}"/> or
/// <see cref="PreFlightError"/> at the API surface.
/// </summary>
/// <remarks>
/// <para>
/// This is the "OOP at the API surface, FP inside" boundary for
/// service inspectors. Internally Core unwraps
/// <see cref="Internal"/> at the dispatcher boundary; users see only
/// <see cref="InspectionResult"/> and the <see cref="Inspect"/>
/// helpers.
/// </para>
/// </remarks>
public readonly record struct InspectionResult
{
  /// <summary>
  /// Internal accumulating validation value; consumed by Core's
  /// dispatcher pipeline. Not part of the public API surface even
  /// though the property itself is necessarily public for
  /// cross-assembly access.
  /// </summary>
  internal Validated<PreFlightError, FlowUnit> Internal { get; init; }
}

/// <summary>
/// Helpers for constructing an <see cref="InspectionResult"/>.
/// Service inspectors return one of these from their probe lambda /
/// method; the helpers hide the underlying
/// <see cref="Validated{TError, TValue}"/> + <see cref="PreFlightError"/>
/// machinery behind a small, declarative surface.
/// </summary>
public static class Inspect
{
  /// <summary>The probe succeeded — the service is reachable / configured.</summary>
  public static InspectionResult Pass() =>
    new() { Internal = Validated<PreFlightError, FlowUnit>.Pure(FlowUnit.Default) };

  /// <summary>
  /// The probe failed. <paramref name="message"/> is the human-readable
  /// description; <paramref name="source"/> overrides the default
  /// source label in the diagnostic (defaults to the service type's
  /// short name when omitted).
  /// </summary>
  public static InspectionResult Fail(string message, string? source = null) =>
    new()
    {
      Internal = Validated<PreFlightError, FlowUnit>.Fail(
        new PreFlightError.InspectionFailed(
          ItemId: source ?? "service",
          Detail: message
        )
      ),
    };

  /// <summary>
  /// Convenience: returns <see cref="Pass"/> when
  /// <paramref name="condition"/> is <see langword="false"/>; otherwise
  /// returns <see cref="Fail(string, string?)"/> with
  /// <paramref name="message"/>.
  /// </summary>
  public static InspectionResult FailIf(
    bool condition,
    string message,
    string? source = null
  ) => condition ? Fail(message, source) : Pass();
}
