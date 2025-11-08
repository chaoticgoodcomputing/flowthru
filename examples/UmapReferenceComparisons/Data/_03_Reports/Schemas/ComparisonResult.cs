using Flowthru.Abstractions;

namespace UmapReferenceComparisons.Data._03_Reports.Schemas;

/// <summary>
/// Comparison result for UMAP reference validation.
/// </summary>
/// <remarks>
/// Contains metrics comparing C# UMAP output against Python reference output.
/// Initial implementation validates only count and schema compatibility.
/// </remarks>
public record ComparisonResult : IFlatSchema, IBinarySerializable, IStructuredSerializable
{
  /// <summary>
  /// Name of the dataset being compared (e.g., "iris", "digits", "mnist").
  /// </summary>
  [SerializedLabel("dataset")]
  public string Dataset { get; init; } = null!;

  /// <summary>
  /// Number of samples in Python reference output.
  /// </summary>
  [SerializedLabel("python_sample_count")]
  public int PythonSampleCount { get; init; }

  /// <summary>
  /// Number of samples in C# UMAP output.
  /// </summary>
  [SerializedLabel("csharp_sample_count")]
  public int CSharpSampleCount { get; init; }

  /// <summary>
  /// Number of dimensions in Python reference output.
  /// </summary>
  [SerializedLabel("python_dimension_count")]
  public int PythonDimensionCount { get; init; }

  /// <summary>
  /// Number of dimensions in C# UMAP output.
  /// </summary>
  [SerializedLabel("csharp_dimension_count")]
  public int CSharpDimensionCount { get; init; }

  /// <summary>
  /// Whether sample counts match.
  /// </summary>
  [SerializedLabel("counts_match")]
  public bool CountsMatch { get; init; }

  /// <summary>
  /// Whether dimension counts match.
  /// </summary>
  [SerializedLabel("dimensions_match")]
  public bool DimensionsMatch { get; init; }

  /// <summary>
  /// Whether the comparison passed (counts and dimensions match).
  /// </summary>
  [SerializedLabel("validation_passed")]
  public bool ValidationPassed { get; init; }

  /// <summary>
  /// Descriptive message about the comparison result.
  /// </summary>
  [SerializedLabel("message")]
  public string Message { get; init; } = null!;
}
