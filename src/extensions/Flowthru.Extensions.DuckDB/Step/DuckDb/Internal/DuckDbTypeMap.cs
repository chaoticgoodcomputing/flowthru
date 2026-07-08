namespace Flowthru.Step.DuckDb.Internal;

/// <summary>
/// CLR-to-DuckDB type compatibility used when verifying a transform's
/// result schema against the output item's declared schema. Each CLR
/// type accepts the DuckDB types that round-trip losslessly into it
/// through a Parquet write + Flowthru load — exact matches plus safe
/// widenings (an <c>INTEGER</c> result loads into a <c>long</c>
/// property without loss, so it's accepted; the reverse is not).
/// </summary>
/// <remarks>
/// Deliberately strict beyond safe widening: a <c>BIGINT</c> result
/// against an <c>int</c> property, or a <c>HUGEINT</c> aggregate
/// against anything, is a mismatch — the fix is an explicit
/// <c>CAST</c> in the transform SQL, stated in the mismatch message,
/// not a silent narrowing that overflows two hours into a run.
/// </remarks>
internal static class DuckDbTypeMap
{
  /// <summary>
  /// DuckDB type names (upper-case, parameters stripped) each CLR type
  /// accepts. Parametric types (<c>DECIMAL(18,3)</c>) match on the name
  /// before the parenthesis; <c>TIMESTAMP</c> entries match every
  /// timestamp precision variant via the prefix rule in
  /// <see cref="IsCompatible"/>.
  /// </summary>
  private static readonly Dictionary<Type, string[]> Accepted = new()
  {
    [typeof(bool)] = ["BOOLEAN"],
    [typeof(sbyte)] = ["TINYINT"],
    [typeof(byte)] = ["UTINYINT"],
    [typeof(short)] = ["SMALLINT", "TINYINT", "UTINYINT"],
    [typeof(ushort)] = ["USMALLINT", "UTINYINT"],
    [typeof(int)] = ["INTEGER", "SMALLINT", "TINYINT", "USMALLINT", "UTINYINT"],
    [typeof(uint)] = ["UINTEGER", "USMALLINT", "UTINYINT"],
    [typeof(long)] =
      ["BIGINT", "INTEGER", "SMALLINT", "TINYINT", "UINTEGER", "USMALLINT", "UTINYINT"],
    [typeof(ulong)] = ["UBIGINT", "UINTEGER", "USMALLINT", "UTINYINT"],
    [typeof(float)] = ["FLOAT", "REAL"],
    [typeof(double)] = ["DOUBLE", "FLOAT", "REAL"],
    [typeof(decimal)] = ["DECIMAL"],
    [typeof(string)] = ["VARCHAR"],
    [typeof(DateTime)] = ["TIMESTAMP"],
    [typeof(DateTimeOffset)] = ["TIMESTAMP WITH TIME ZONE", "TIMESTAMP"],
    [typeof(DateOnly)] = ["DATE"],
    [typeof(TimeOnly)] = ["TIME"],
    [typeof(TimeSpan)] = ["INTERVAL"],
    [typeof(Guid)] = ["UUID", "BLOB", "VARCHAR"],
    [typeof(byte[])] = ["BLOB"],
  };

  /// <summary>
  /// True when a result column of DuckDB type
  /// <paramref name="duckDbType"/> satisfies a schema property of
  /// (non-nullable, enum-unwrapped) CLR type <paramref name="clrType"/>.
  /// </summary>
  public static bool IsCompatible(Type clrType, string duckDbType)
  {
    if (!Accepted.TryGetValue(clrType, out var accepted)) return false;

    var normalized = Normalize(duckDbType);
    foreach (var candidate in accepted)
    {
      if (normalized.Equals(candidate, StringComparison.Ordinal)) return true;
      // Parametric / precision variants: DECIMAL(18,3), TIMESTAMP_NS,
      // TIME WITH TIME ZONE, etc. match on the accepted name as prefix.
      if (normalized.StartsWith(candidate, StringComparison.Ordinal)) return true;
    }
    return false;
  }

  /// <summary>
  /// True when the verifier knows how to check <paramref name="clrType"/>
  /// at all. Unknown types are rejected at wire-up with guidance, rather
  /// than at runtime with a confusing mismatch.
  /// </summary>
  public static bool IsSupported(Type clrType) => Accepted.ContainsKey(clrType);

  /// <summary>
  /// The DuckDB types <paramref name="clrType"/> accepts, for mismatch
  /// messages ("expected BIGINT/INTEGER/...").
  /// </summary>
  public static string DescribeAccepted(Type clrType) =>
    Accepted.TryGetValue(clrType, out var accepted)
      ? string.Join("/", accepted)
      : "(unsupported)";

  private static string Normalize(string duckDbType) =>
    duckDbType.Trim().ToUpperInvariant();
}
