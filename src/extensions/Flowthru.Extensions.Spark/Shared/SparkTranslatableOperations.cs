// This file is shared between Flowthru.Extensions.Spark (runtime) and
// Flowthru.Extensions.Spark.SourceGenerators (analyzer) via a <Compile Link="..."/>.
// It must not reference types from either host project — only BCL types.

using System;
using System.Collections.Generic;

namespace Flowthru.Extensions.Spark.Shared;

/// <summary>
/// The authoritative whitelist of BCL operations that the <c>SparkExpressionVisitor</c>
/// can translate to Spark <c>Column</c> expressions.
/// </summary>
/// <remarks>
/// <para>
/// This class is the single source of truth for the Spark-translatable subset. Both the
/// runtime visitor and the <c>FSPARK1002</c> Roslyn analyzer consume it. When a new
/// translation is added to <c>SparkExpressionVisitor</c>:
/// </para>
/// <list type="number">
/// <item>Add the method or operator name to the appropriate set here.</item>
/// <item>Implement the switch arm in the visitor.</item>
/// </list>
/// <para>
/// A sync-validation test in <c>Flowthru.Tests.Spark</c> verifies that every entry in
/// these sets has a corresponding switch arm in the visitor, catching the reverse case.
/// </para>
/// </remarks>
public static class SparkTranslatableOperations
{
    /// <summary>
    /// <c>string</c> instance methods that have Spark <c>Column</c> translations.
    /// </summary>
    public static readonly IReadOnlyCollection<string> SupportedStringMethods = new HashSet<string>(
      StringComparer.Ordinal
    )
  {
    nameof(string.Replace),
    nameof(string.Contains),
    nameof(string.StartsWith),
    nameof(string.EndsWith),
    nameof(string.ToUpper),
    nameof(string.ToLower),
    nameof(string.Trim),
    nameof(string.TrimStart),
    nameof(string.TrimEnd),
    nameof(string.Substring),
  };

    /// <summary>
    /// <c>System.Math</c> static methods that have Spark <c>Column</c> translations.
    /// </summary>
    public static readonly IReadOnlyCollection<string> SupportedMathMethods = new HashSet<string>(
      StringComparer.Ordinal
    )
  {
    nameof(Math.Round),
    nameof(Math.Abs),
    nameof(Math.Floor),
    nameof(Math.Ceiling),
  };

    /// <summary>
    /// <c>System.DateTime</c> instance properties that have Spark date/time function translations.
    /// </summary>
    public static readonly IReadOnlyCollection<string> SupportedDateTimeProperties =
      new HashSet<string>(StringComparer.Ordinal)
      {
      nameof(DateTime.Year),
      nameof(DateTime.Month),
      nameof(DateTime.Day),
      nameof(DateTime.Hour),
      nameof(DateTime.Minute),
      nameof(DateTime.Second),
      };
}
