using Flowthru.Abstractions;

namespace UmapReferenceComparisons.Data._03_Reports.Schemas;

/// <summary>
/// Comparison result for UMAP reference validation.
/// </summary>
/// <remarks>
/// Contains metrics comparing C# UMAP output against Python reference output.
/// Validates structural similarity through k-NN graph comparison.
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
  /// Number of k-nearest neighbors used for skeletal similarity comparison.
  /// </summary>
  [SerializedLabel("k_neighbors")]
  public int KNeighbors { get; init; }

  /// <summary>
  /// Python UMAP neighborhood preservation: how well Python UMAP preserves original data structure.
  /// </summary>
  /// <remarks>
  /// Measures proportion of high-dimensional k-NN edges preserved in Python embedding.
  /// Score closer to 1.0 indicates Python UMAP is working correctly.
  /// </remarks>
  [SerializedLabel("python_neighborhood_preservation")]
  public double PythonNeighborhoodPreservation { get; init; }

  /// <summary>
  /// C# UMAP neighborhood preservation: how well C# UMAP preserves original data structure.
  /// </summary>
  /// <remarks>
  /// Measures proportion of high-dimensional k-NN edges preserved in C# embedding.
  /// Score closer to 1.0 indicates C# UMAP is working correctly.
  /// </remarks>
  [SerializedLabel("csharp_neighborhood_preservation")]
  public double CSharpNeighborhoodPreservation { get; init; }

  /// <summary>
  /// Mean C# neighborhood preservation across all trials.
  /// </summary>
  [SerializedLabel("csharp_mean_preservation")]
  public double CSharpMeanPreservation { get; init; }

  /// <summary>
  /// Standard deviation of C# neighborhood preservation across trials.
  /// </summary>
  [SerializedLabel("csharp_stddev_preservation")]
  public double CSharpStdDevPreservation { get; init; }

  /// <summary>
  /// Minimum C# neighborhood preservation across trials.
  /// </summary>
  [SerializedLabel("csharp_min_preservation")]
  public double CSharpMinPreservation { get; init; }

  /// <summary>
  /// Maximum C# neighborhood preservation across trials.
  /// </summary>
  [SerializedLabel("csharp_max_preservation")]
  public double CSharpMaxPreservation { get; init; }

  /// <summary>
  /// Absolute difference between Python and C# mean preservation scores.
  /// </summary>
  [SerializedLabel("preservation_difference")]
  public double PreservationDifference { get; init; }

  /// <summary>
  /// Statistical confidence that Python and C# produce similar preservation scores.
  /// </summary>
  /// <remarks>
  /// Higher values indicate greater confidence that both implementations
  /// are mathematically equivalent despite random initialization differences.
  /// </remarks>
  [SerializedLabel("statistical_confidence")]
  public double StatisticalConfidence { get; init; }

  /// <summary>
  /// Average runtime metrics across all C# UMAP trials.
  /// Dictionary mapping stage name to average milliseconds.
  /// </summary>
  [SerializedLabel("csharp_avg_timings")]
  public Dictionary<string, int> CSharpAvgTimings { get; init; } = new();

  /// <summary>
  /// Average total runtime across all C# UMAP trials, in milliseconds.
  /// </summary>
  [SerializedLabel("csharp_avg_total_time_ms")]
  public int CSharpAvgTotalTimeMs { get; init; }

  /// <summary>
  /// Whether the comparison passed (counts, dimensions match, and skeletal similarity above threshold).
  /// </summary>
  [SerializedLabel("validation_passed")]
  public bool ValidationPassed { get; init; }

  /// <summary>
  /// Descriptive message about the comparison result.
  /// </summary>
  [SerializedLabel("message")]
  public string Message { get; init; } = null!;
}
