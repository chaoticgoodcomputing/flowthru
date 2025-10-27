using Flowthru.Nodes;
using FlowthruIris.Data.Schemas;
using Microsoft.Extensions.Logging;

namespace FlowthruIris.Pipelines.Analysis.Nodes;

/// <summary>
/// Processes raw Iris CSV data by converting strings to typed floats and engineering features.
/// 
/// <para><strong>Type Safety Pattern</strong></para>
/// <para>
/// This node demonstrates Flowthru's type safety by transforming:
/// - Input: IrisRawSchema (all strings from CSV)
/// - Output: IrisSchema (validated floats + engineered features)
/// </para>
/// <para>
/// The compiler enforces that this node can only be used where these exact types are expected.
/// </para>
/// 
/// <para><strong>Data Validation</strong></para>
/// <para>
/// - Parses string fields to floats with error handling
/// - Filters out invalid records (malformed numbers, missing values)
/// - Validates species names against known classes
/// - Logs warnings for skipped records
/// </para>
/// 
/// <para><strong>Feature Engineering</strong></para>
/// <para>
/// Calculates aspect ratios (length/width) for petals and sepals.
/// These derived features can improve model performance by capturing shape information.
/// </para>
/// </summary>
public class ProcessRawDataNode : NodeBase<IEnumerable<IrisRawSchema>, IEnumerable<IrisSchema>> {
  /// <summary>
  /// Known Iris species for validation
  /// </summary>
  private static readonly HashSet<string> ValidSpecies = new()
  {
        "Iris-setosa",
        "Iris-versicolor",
        "Iris-virginica"
    };

  protected override Task<IEnumerable<IrisSchema>> Transform(IEnumerable<IrisRawSchema> input) {
    var rawData = input.ToList();
    Logger?.LogInformation("Processing {Count} raw Iris records", rawData.Count);

    var processed = rawData
        .Select((raw, index) => TryParse(raw, index))
        .Where(result => result != null)
        .Cast<IrisSchema>()
        .ToList();

    var skipped = rawData.Count - processed.Count;
    if (skipped > 0) {
      Logger?.LogWarning("Skipped {Count} invalid records during processing", skipped);
    }

    Logger?.LogInformation("Successfully processed {Count} Iris records", processed.Count);

    return Task.FromResult<IEnumerable<IrisSchema>>(processed);
  }

  /// <summary>
  /// Attempts to parse a raw Iris record into a validated, typed schema.
  /// Returns null if parsing fails or validation fails.
  /// </summary>
  private IrisSchema? TryParse(IrisRawSchema raw, int rowIndex) {
    // Parse numeric fields
    if (!float.TryParse(raw.SepalLength, out var sepalLength)) {
      Logger?.LogWarning("Row {Index}: Invalid sepal_length '{Value}'", rowIndex, raw.SepalLength);
      return null;
    }

    if (!float.TryParse(raw.SepalWidth, out var sepalWidth)) {
      Logger?.LogWarning("Row {Index}: Invalid sepal_width '{Value}'", rowIndex, raw.SepalWidth);
      return null;
    }

    if (!float.TryParse(raw.PetalLength, out var petalLength)) {
      Logger?.LogWarning("Row {Index}: Invalid petal_length '{Value}'", rowIndex, raw.PetalLength);
      return null;
    }

    if (!float.TryParse(raw.PetalWidth, out var petalWidth)) {
      Logger?.LogWarning("Row {Index}: Invalid petal_width '{Value}'", rowIndex, raw.PetalWidth);
      return null;
    }

    // Validate species
    if (!ValidSpecies.Contains(raw.Species)) {
      Logger?.LogWarning("Row {Index}: Unknown species '{Species}'", rowIndex, raw.Species);
      return null;
    }

    // Engineer features: calculate aspect ratios
    // Use small epsilon to avoid division by zero
    const float epsilon = 0.001f;
    var petalRatio = petalLength / Math.Max(petalWidth, epsilon);
    var sepalRatio = sepalLength / Math.Max(sepalWidth, epsilon);

    return new IrisSchema {
      SepalLength = sepalLength,
      SepalWidth = sepalWidth,
      PetalLength = petalLength,
      PetalWidth = petalWidth,
      Species = raw.Species,
      PetalRatio = petalRatio,
      SepalRatio = sepalRatio
    };
  }
}
