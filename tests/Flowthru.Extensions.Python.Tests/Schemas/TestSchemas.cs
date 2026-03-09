using Flowthru.Abstractions;

namespace Flowthru.Extensions.Python.Tests.Schemas;

// ─────────────────────────────────────────────────────────────────
// Phase 2: Scalar schemas
// ─────────────────────────────────────────────────────────────────

/// <summary>
/// Test schema for a simple model configuration input.
/// </summary>
public record ModelConfigSchema
{
  /// <summary>
  /// Learning rate parameter.
  /// </summary>
  public double LearningRate { get; init; }

  /// <summary>
  /// Number of iterations/epochs.
  /// </summary>
  public int Iterations { get; init; }

  /// <summary>
  /// Optional model name.
  /// </summary>
  public string? ModelName { get; init; }
}

/// <summary>
/// Test schema for model training results.
/// </summary>
public record ModelResultSchema
{
  /// <summary>
  /// Final accuracy metric.
  /// </summary>
  public double Accuracy { get; init; }

  /// <summary>
  /// Final loss value.
  /// </summary>
  public double Loss { get; init; }

  /// <summary>
  /// Training completed successfully.
  /// </summary>
  public bool Success { get; init; }

  /// <summary>
  /// Result message.
  /// </summary>
  public string? Message { get; init; }
}

/// <summary>
/// Test schema with SerializedLabel attributes for scalar marshalling.
/// Mimics report schemas that use snake_case serialization.
/// </summary>
public record MetricsReportSchema
{
  /// <summary>
  /// Overall accuracy metric (0.0 to 1.0).
  /// </summary>
  [SerializedLabel("accuracy")]
  public double Accuracy { get; init; }

  /// <summary>
  /// Number of correct predictions.
  /// </summary>
  [SerializedLabel("correct_predictions")]
  public int CorrectPredictions { get; init; }

  /// <summary>
  /// Total number of samples.
  /// </summary>
  [SerializedLabel("total_samples")]
  public int TotalSamples { get; init; }
}

// ─────────────────────────────────────────────────────────────────
// Phase 5: Array marshalling schemas
// ─────────────────────────────────────────────────────────────────

/// <summary>
/// Test schema for ML models with array properties (e.g., LinearRegression).
/// </summary>
public record ModelWithArraysSchema
{
  /// <summary>
  /// Regression coefficients for each feature.
  /// </summary>
  public double[] Coefficients { get; init; } = Array.Empty<double>();

  /// <summary>
  /// Intercept term (bias) of the regression model.
  /// </summary>
  public double Intercept { get; init; }

  /// <summary>
  /// Names of features corresponding to each coefficient.
  /// </summary>
  public string[] FeatureNames { get; init; } = Array.Empty<string>();
}

// ─────────────────────────────────────────────────────────────────
// Phase 3: Tabular schemas
// ─────────────────────────────────────────────────────────────────

/// <summary>
/// Simple flat schema for Arrow marshalling tests.
/// </summary>
[FlowthruSchema]
public partial record SimpleRowSchema
{
  /// <summary>
  /// Row identifier.
  /// </summary>
  [SerializedLabel("id")]
  public required int Id { get; init; }

  /// <summary>
  /// Name field.
  /// </summary>
  [SerializedLabel("name")]
  public required string Name { get; init; }

  /// <summary>
  /// Numeric value.
  /// </summary>
  [SerializedLabel("value")]
  public required double Value { get; init; }
}

/// <summary>
/// Schema with extended type support for Arrow marshalling.
/// </summary>
[FlowthruSchema]
public partial record ExtendedTypesSchema
{
  /// <summary>
  /// Unique identifier (Guid stored as string in Arrow).
  /// </summary>
  [SerializedLabel("id")]
  public required Guid Id { get; init; }

  /// <summary>
  /// Creation timestamp.
  /// </summary>
  [SerializedLabel("created_at")]
  public required DateTime CreatedAt { get; init; }

  /// <summary>
  /// Optional modification timestamp.
  /// </summary>
  [SerializedLabel("modified_at")]
  public DateTimeOffset? ModifiedAt { get; init; }

  /// <summary>
  /// Optional duration value.
  /// </summary>
  [SerializedLabel("duration")]
  public TimeSpan? Duration { get; init; }

  /// <summary>
  /// Optional name field.
  /// </summary>
  [SerializedLabel("name")]
  public string? Name { get; init; }
}
