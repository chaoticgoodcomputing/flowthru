namespace Flowthru.Step.DuckDb.Internal;

/// <summary>
/// Compares the result schema DuckDB describes for a transform's SQL
/// against the output item's declared schema, before anything is
/// written. Name matching is case-insensitive (DuckDB folds unquoted
/// identifiers); type matching follows <see cref="DuckDbTypeMap"/>.
/// </summary>
internal static class DuckDbSchemaVerifier
{
  /// <summary>
  /// Returns <c>null</c> when the described result satisfies the
  /// declared schema; otherwise a human-readable detail enumerating
  /// every missing, extra, and type-incompatible column (accumulated,
  /// not first-failure, so one run surfaces the whole disagreement).
  /// </summary>
  public static string? Verify(
    IReadOnlyList<DuckDbExpectedColumn> expected,
    IReadOnlyList<(string Name, string DuckDbType)> actual
  )
  {
    var problems = new List<string>();

    var actualByName = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    foreach (var (name, duckDbType) in actual)
    {
      if (!actualByName.TryAdd(name, duckDbType))
      {
        problems.Add(
          $"result produces column '{name}' more than once — alias each occurrence uniquely"
        );
      }
    }

    foreach (var column in expected)
    {
      if (!actualByName.TryGetValue(column.Name, out var duckDbType))
      {
        problems.Add(
          $"declared column '{column.Name}' ({column.ClrType.Name}) is missing from the result"
        );
        continue;
      }

      if (!DuckDbTypeMap.IsCompatible(column.ClrType, duckDbType))
      {
        problems.Add(
          $"column '{column.Name}' is {duckDbType} in the result but the declared schema "
          + $"expects {column.ClrType.Name} "
          + $"(accepts {DuckDbTypeMap.DescribeAccepted(column.ClrType)}) — "
          + "add an explicit CAST in the transform SQL"
        );
      }
    }

    var expectedNames = new HashSet<string>(
      expected.Select(c => c.Name), StringComparer.OrdinalIgnoreCase);
    foreach (var (name, duckDbType) in actual)
    {
      if (!expectedNames.Contains(name))
      {
        problems.Add(
          $"result produces column '{name}' ({duckDbType}) that the declared schema "
          + "doesn't have — drop it from the SELECT list or add it to the schema"
        );
      }
    }

    return problems.Count == 0 ? null : string.Join("; ", problems);
  }
}
