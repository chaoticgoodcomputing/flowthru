using UmapReferenceComparisons.Data._02_ModelOutputs.Schemas;
using UmapReferenceComparisons.Data._03_Reports.Schemas;

namespace UmapReferenceComparisons.Pipelines.IrisComparison.Nodes;

/// <summary>
/// Compares C# UMAP output against Python reference output.
/// </summary>
/// <remarks>
/// Validates that the C# UMAP implementation produces outputs with:
/// - Same number of samples as Python reference
/// - Same number of dimensions as Python reference
/// - Compatible schema structure
/// </remarks>
public static class CompareOutputsNode
{
  public static Func<
    (IEnumerable<UmapEmbedding2D> pythonOutput, IEnumerable<UmapEmbedding2D> csharpOutput),
    Task<ComparisonResult>
  > Create(string datasetName)
  {
    return async (input) =>
    {
      var (pythonOutput, csharpOutput) = input;

      var pythonList = pythonOutput.ToList();
      var csharpList = csharpOutput.ToList();

      Console.WriteLine($"\n=== UMAP Output Comparison for {datasetName} ===");
      Console.WriteLine($"Python reference samples: {pythonList.Count}");
      Console.WriteLine($"C# UMAP samples: {csharpList.Count}");

      // Validate sample counts match
      var countsMatch = pythonList.Count == csharpList.Count;
      Console.WriteLine($"Sample counts match: {countsMatch}");

      // Validate dimensions (both should be 2D)
      var pythonDimensions = 2; // Known from schema
      var csharpDimensions = 2; // Known from schema
      var dimensionsMatch = pythonDimensions == csharpDimensions;
      Console.WriteLine($"Dimension counts match: {dimensionsMatch}");

      var validationPassed = countsMatch && dimensionsMatch;

      string message;
      if (validationPassed)
      {
        message =
          $"✓ Validation passed for {datasetName}: "
          + $"Both outputs have {pythonList.Count} samples with {pythonDimensions} dimensions";
      }
      else
      {
        var errors = new List<string>();
        if (!countsMatch)
        {
          errors.Add($"Sample count mismatch: Python={pythonList.Count}, C#={csharpList.Count}");
        }
        if (!dimensionsMatch)
        {
          errors.Add($"Dimension mismatch: Python={pythonDimensions}, C#={csharpDimensions}");
        }
        message = $"✗ Validation failed for {datasetName}: {string.Join("; ", errors)}";
      }

      Console.WriteLine($"\n{message}\n");

      var result = new ComparisonResult
      {
        Dataset = datasetName,
        PythonSampleCount = pythonList.Count,
        CSharpSampleCount = csharpList.Count,
        PythonDimensionCount = pythonDimensions,
        CSharpDimensionCount = csharpDimensions,
        CountsMatch = countsMatch,
        DimensionsMatch = dimensionsMatch,
        ValidationPassed = validationPassed,
        Message = message,
      };

      return await Task.FromResult(result);
    };
  }
}
