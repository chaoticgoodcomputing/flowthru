namespace FlowthruIris.Data.Schemas;

/// <summary>
/// Processed Iris schema with validated, typed data and engineered features.
///
/// <para><strong>Type Safety Benefits</strong></para>
/// <para>
/// By converting to strongly-typed floats, the compiler ensures:
/// - ML.NET receives correctly typed numeric features
/// - No accidental string-to-number conversion errors at runtime
/// - IntelliSense shows exact data types
/// - Mathematical operations are type-safe
/// </para>
///
/// <para><strong>Feature Engineering</strong></para>
/// <para>
/// Adds derived features (ratios) that can improve model performance:
/// - PetalRatio: Captures petal shape (length/width relationship)
/// - SepalRatio: Captures sepal shape (length/width relationship)
/// </para>
///
/// <para><strong>Data Validation</strong></para>
/// <para>
/// Records reaching this schema have been validated during parsing:
/// - All numeric fields successfully converted from strings
/// - No null or missing values
/// - Species is one of the three known classes
/// </para>
/// </summary>
public record IrisSchema
{
  /// <summary>
  /// Sepal length in centimeters (validated float)
  /// </summary>
  public float SepalLength { get; init; }

  /// <summary>
  /// Sepal width in centimeters (validated float)
  /// </summary>
  public float SepalWidth { get; init; }

  /// <summary>
  /// Petal length in centimeters (validated float)
  /// </summary>
  public float PetalLength { get; init; }

  /// <summary>
  /// Petal width in centimeters (validated float)
  /// </summary>
  public float PetalWidth { get; init; }

  /// <summary>
  /// Iris species classification (validated)
  /// Values: "Iris-setosa", "Iris-versicolor", "Iris-virginica"
  /// </summary>
  public string Species { get; init; } = string.Empty;

  /// <summary>
  /// Engineered feature: Petal aspect ratio (length / width)
  /// Higher values indicate longer, narrower petals
  /// </summary>
  public float PetalRatio { get; init; }

  /// <summary>
  /// Engineered feature: Sepal aspect ratio (length / width)
  /// Higher values indicate longer, narrower sepals
  /// </summary>
  public float SepalRatio { get; init; }
}
