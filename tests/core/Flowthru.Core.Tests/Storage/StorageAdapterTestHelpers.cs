using Flowthru.Data.Storage;
using Flowthru.Prelude;

namespace Flowthru.Core.Tests.Storage;

/// <summary>
/// Shared helpers for per-adapter storage tests. Centralizes the
/// <see cref="EffResult{A}.Success"/> unwrap and validation-error
/// formatting that every adapter-level test needs, so individual test
/// classes stay focused on the assertions specific to their adapter.
/// </summary>
internal static class StorageAdapterTestHelpers
{
  /// <summary>
  /// Runs an effect and asserts it succeeded, returning the unwrapped value.
  /// Failure cases include the <see cref="RuntimeError"/> in the NUnit
  /// message so a regression points at the underlying cause.
  /// </summary>
  public static async Task<T> UnwrapSuccess<T>(FlowIO<T> effect)
  {
    var result = await effect.Run();
    Assert.That(result, Is.InstanceOf<EffResult<T>.Success>(),
      $"Effect failed: {(result as EffResult<T>.Failure)?.Error}");
    return ((EffResult<T>.Success)result).Value;
  }

  /// <summary>
  /// Formats the first <see cref="ValidationError"/> in a result for use in
  /// NUnit failure messages. Returns <c>"&lt;no errors&gt;"</c> when the
  /// result is empty.
  /// </summary>
  public static string FormatFirstError(ValidationResult result)
  {
    var first = result.Errors.FirstOrDefault();
    return first is null
      ? "<no errors>"
      : $"[{first.ErrorType}] {first.CatalogKey}: {first.Message}";
  }
}
