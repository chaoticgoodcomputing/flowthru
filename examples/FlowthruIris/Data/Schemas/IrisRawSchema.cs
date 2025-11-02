using CsvHelper.Configuration.Attributes;

namespace FlowthruIris.Data.Schemas;

/// <summary>
/// Raw schema for Iris CSV data - all fields as strings for type-safe parsing.
///
/// <para><strong>Design Pattern: String-First Loading</strong></para>
/// <para>
/// By loading CSV data as strings first, we can:
/// - Handle malformed data gracefully (invalid numbers, missing values)
/// - Validate data before type conversion
/// - Log parsing errors with row context
/// - Demonstrate Flowthru's type safety at the schema transformation layer
/// </para>
///
/// <para><strong>CSV Mapping</strong></para>
/// <para>
/// Maps directly to iris.csv columns using CsvHelper attributes.
/// This schema represents data "as it appears" in the CSV file.
/// </para>
/// </summary>
public record IrisRawSchema
{
  /// <summary>
  /// Sepal length in centimeters (raw string from CSV)
  /// </summary>
  [Name("sepal_length")]
  public string SepalLength { get; init; } = string.Empty;

  /// <summary>
  /// Sepal width in centimeters (raw string from CSV)
  /// </summary>
  [Name("sepal_width")]
  public string SepalWidth { get; init; } = string.Empty;

  /// <summary>
  /// Petal length in centimeters (raw string from CSV)
  /// </summary>
  [Name("petal_length")]
  public string PetalLength { get; init; } = string.Empty;

  /// <summary>
  /// Petal width in centimeters (raw string from CSV)
  /// </summary>
  [Name("petal_width")]
  public string PetalWidth { get; init; } = string.Empty;

  /// <summary>
  /// Iris species classification
  /// Expected values: "Iris-setosa", "Iris-versicolor", "Iris-virginica"
  /// </summary>
  [Name("class")]
  public string Species { get; init; } = string.Empty;
}
