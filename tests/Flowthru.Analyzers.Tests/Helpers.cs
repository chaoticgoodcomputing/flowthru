using Flowthru.Extensions.Spark.Shared;

namespace Flowthru.Analyzers.Tests;

/// <summary>
/// Shared test helpers derived from <see cref="SparkTranslatableOperations"/> so that
/// expected diagnostic message arguments stay in sync with the analyzer automatically.
/// </summary>
internal static class Helpers
{
  public static readonly string SupportedStringList = string.Join(
    ", ",
    SparkTranslatableOperations.SupportedStringMethods
  );

  public static readonly string SupportedMathList = string.Join(
    ", ",
    SparkTranslatableOperations.SupportedMathMethods
  );
}
