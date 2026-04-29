using Flowthru.Core.Data.Storage;
using Flowthru.Core.Data.Validation;

namespace Flowthru.Tests.Kits.Storage;

/// <summary>
/// Reusable assertions for exercising any <see cref="IStorageAdapter{T}"/> implementation
/// against the standard storage operations (Inspect Shallow / Deep / Target, Save / Load,
/// Exists). Test authors construct an adapter in the desired scenario state, then call the
/// assertion that matches the expected outcome.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Design.</strong> Each assertion runs the relevant <see cref="Effects.FlowIO{A}"/>
/// effect, unwraps it, and asserts via NUnit. Tests that exercise the same scenario across
/// multiple adapters use the same assertion call; only the adapter construction differs.
/// </para>
/// <para>
/// <strong>Reuse target.</strong> This harness is the underlying mechanism that the
/// <c>StorageAdapterConformance</c> base in this same project delegates to. Core's adapter
/// tests call into these assertions directly without going through the conformance base.
/// </para>
/// </remarks>
public static class StorageAdapterAssertions
{
  // ─────────────────────────────────────────────────────────────────────────
  // Inspect Shallow
  // ─────────────────────────────────────────────────────────────────────────

  /// <summary>
  /// Asserts that <see cref="IStorageAdapter{T}.InspectShallow"/> returns a successful
  /// validation result. Use when the adapter is constructed against well-formed,
  /// accessible data.
  /// </summary>
  public static async Task InspectShallowSucceeds<T>(IStorageAdapter<T> adapter, int sampleSize = 10)
  {
    var result = await adapter.InspectShallow(sampleSize).Run();
    Assert.That(
      result.IsValid,
      Is.True,
      $"Expected InspectShallow to succeed but got {result.ErrorCount} error(s). "
        + $"First error: {FormatFirstError(result)}"
    );
  }

  /// <summary>
  /// Asserts that <see cref="IStorageAdapter{T}.InspectShallow"/> reports a validation
  /// failure of the expected type. Use for missing-data or schema-mismatch scenarios.
  /// </summary>
  public static async Task InspectShallowFails<T>(
    IStorageAdapter<T> adapter,
    ValidationErrorType expectedErrorType,
    int sampleSize = 10
  )
  {
    var result = await adapter.InspectShallow(sampleSize).Run();
    Assert.That(result.IsValid, Is.False, "Expected InspectShallow to fail but it succeeded.");
    Assert.That(
      result.Errors,
      Has.Some.Matches<ValidationError>(e => e.ErrorType == expectedErrorType),
      $"Expected at least one error with type {expectedErrorType} but got: "
        + string.Join(", ", result.Errors.Select(e => e.ErrorType))
    );
  }

  // ─────────────────────────────────────────────────────────────────────────
  // Inspect Deep
  // ─────────────────────────────────────────────────────────────────────────

  /// <summary>
  /// Asserts that <see cref="IStorageAdapter{T}.InspectDeep"/> returns a successful
  /// validation result. Use when the adapter is constructed against well-formed data.
  /// </summary>
  public static async Task InspectDeepSucceeds<T>(IStorageAdapter<T> adapter)
  {
    var result = await adapter.InspectDeep().Run();
    Assert.That(
      result.IsValid,
      Is.True,
      $"Expected InspectDeep to succeed but got {result.ErrorCount} error(s). "
        + $"First error: {FormatFirstError(result)}"
    );
  }

  /// <summary>
  /// Asserts that <see cref="IStorageAdapter{T}.InspectDeep"/> reports a validation failure
  /// of the expected type. Use for corrupted-data or row-deserialization-error scenarios.
  /// </summary>
  public static async Task InspectDeepFails<T>(
    IStorageAdapter<T> adapter,
    ValidationErrorType expectedErrorType
  )
  {
    var result = await adapter.InspectDeep().Run();
    Assert.That(result.IsValid, Is.False, "Expected InspectDeep to fail but it succeeded.");
    Assert.That(
      result.Errors,
      Has.Some.Matches<ValidationError>(e => e.ErrorType == expectedErrorType),
      $"Expected at least one error with type {expectedErrorType} but got: "
        + string.Join(", ", result.Errors.Select(e => e.ErrorType))
    );
  }

  // ─────────────────────────────────────────────────────────────────────────
  // Inspect Target
  // ─────────────────────────────────────────────────────────────────────────

  /// <summary>
  /// Asserts that <see cref="IStorageAdapter{T}.InspectTarget"/> returns a successful
  /// validation result. Use when the destination is writable (or trivially valid for
  /// read-only / null adapters).
  /// </summary>
  public static async Task InspectTargetSucceeds<T>(IStorageAdapter<T> adapter)
  {
    var result = await adapter.InspectTarget().Run();
    Assert.That(
      result.IsValid,
      Is.True,
      $"Expected InspectTarget to succeed but got {result.ErrorCount} error(s). "
        + $"First error: {FormatFirstError(result)}"
    );
  }

  /// <summary>
  /// Asserts that <see cref="IStorageAdapter{T}.InspectTarget"/> reports a write-access
  /// failure. Use when the target is read-only, lacks permissions, or has an absent parent
  /// directory.
  /// </summary>
  public static async Task InspectTargetFails<T>(
    IStorageAdapter<T> adapter,
    ValidationErrorType expectedErrorType = ValidationErrorType.WriteAccessDenied
  )
  {
    var result = await adapter.InspectTarget().Run();
    Assert.That(result.IsValid, Is.False, "Expected InspectTarget to fail but it succeeded.");
    Assert.That(
      result.Errors,
      Has.Some.Matches<ValidationError>(e => e.ErrorType == expectedErrorType),
      $"Expected at least one error with type {expectedErrorType} but got: "
        + string.Join(", ", result.Errors.Select(e => e.ErrorType))
    );
  }

  // ─────────────────────────────────────────────────────────────────────────
  // Exists
  // ─────────────────────────────────────────────────────────────────────────

  /// <summary>
  /// Asserts that <see cref="IStorageAdapter{T}.Exists"/> returns the expected value.
  /// </summary>
  public static async Task ExistsReturns<T>(IStorageAdapter<T> adapter, bool expected)
  {
    var result = await adapter.Exists().Run();
    Assert.That(
      result,
      Is.EqualTo(expected),
      $"Expected Exists() to return {expected} but got {result}."
    );
  }

  // ─────────────────────────────────────────────────────────────────────────
  // Save / Load round-trip
  // ─────────────────────────────────────────────────────────────────────────

  /// <summary>
  /// Asserts that data saved through the adapter can be loaded back equivalently. Verifies
  /// the Save → Load happy path. The provided <paramref name="comparer"/> (or the type's
  /// default equality) determines what "equivalent" means.
  /// </summary>
  public static async Task SaveAndLoadRoundTrips<T>(
    IStorageAdapter<T> adapter,
    T data,
    IEqualityComparer<T>? comparer = null
  )
  {
    await adapter.Save(data).Run();
    var loaded = await adapter.Load().Run();

    var actualComparer = comparer ?? EqualityComparer<T>.Default;
    Assert.That(
      actualComparer.Equals(data, loaded),
      Is.True,
      $"Round-trip mismatch. Saved value did not equal loaded value.\n"
        + $"Expected: {data}\n"
        + $"Actual:   {loaded}"
    );
  }

  // ─────────────────────────────────────────────────────────────────────────
  // Helpers
  // ─────────────────────────────────────────────────────────────────────────

  private static string FormatFirstError(ValidationResult result)
  {
    var first = result.Errors.FirstOrDefault();
    if (first is null)
    {
      return "<no errors>";
    }
    return $"[{first.ErrorType}] {first.CatalogKey}: {first.Message}";
  }
}
