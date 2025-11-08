using UmapReferenceComparisons.Data._01_Raw.Schemas;
using UmapReferenceComparisons.Data._03_Reports.Schemas;

namespace UmapReferenceComparisons.Pipelines.IrisComparison.Nodes;

/// <summary>
/// Compares C# UMAP output against Python reference output.
/// </summary>
/// <remarks>
/// Validates UMAP implementation through multiple metrics:
/// - Sample and dimension count matching
/// - k-NN skeletal similarity (preservation of neighborhood structure)
///
/// Since Python and C# use different RNGs, exact numerical matching is impossible.
/// Instead, we validate that both embeddings preserve similar neighborhood relationships.
/// </remarks>
public static class CompareOutputsNode
{
  /// <summary>
  /// Configuration for comparison metrics.
  /// </summary>
  public record Params
  {
    /// <summary>
    /// Number of nearest neighbors to use for skeletal similarity comparison.
    /// Should match or be close to the n_neighbors parameter used in UMAP.
    /// </summary>
    public int KNeighbors { get; init; } = 15;

    /// <summary>
    /// Minimum skeletal similarity threshold (0.0 to 1.0) for validation to pass.
    /// </summary>
    public double MinimumSimilarity { get; init; } = 0.7;
  }

  public static Func<
    (
      IEnumerable<IrisInputRow> inputData,
      IEnumerable<UmapOutputRow> pythonOutput,
      IEnumerable<UmapOutputRow> csharpOutput
    ),
    Task<ComparisonResult>
  > Create(string datasetName, Params? options = null)
  {
    var config = options ?? new Params();

    return async (input) =>
    {
      var (inputData, pythonOutput, csharpOutput) = input;

      var inputList = inputData.ToList();
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

      // Compute k-NN skeletal similarity
      double skeletalSimilarity = 0.0;
      int totalEdges = 0;
      int preservedEdges = 0;

      if (countsMatch && dimensionsMatch && pythonList.Count > config.KNeighbors)
      {
        Console.WriteLine($"\nComputing k-NN skeletal similarity (k={config.KNeighbors})...");

        var (similarity, total, preserved) = ComputeSkeletalSimilarity(
          pythonList,
          csharpList,
          config.KNeighbors
        );

        skeletalSimilarity = similarity;
        totalEdges = total;
        preservedEdges = preserved;

        Console.WriteLine($"Skeletal similarity: {skeletalSimilarity:P2}");
        Console.WriteLine($"Preserved edges: {preservedEdges}/{totalEdges}");
      }

      // Determine if validation passed
      var validationPassed =
        countsMatch && dimensionsMatch && skeletalSimilarity >= config.MinimumSimilarity;

      // Build result message
      string message = BuildResultMessage(
        datasetName,
        countsMatch,
        dimensionsMatch,
        pythonList.Count,
        csharpList.Count,
        pythonDimensions,
        csharpDimensions,
        skeletalSimilarity,
        config.MinimumSimilarity,
        validationPassed
      );

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
        KNeighbors = config.KNeighbors,
        SkeletalSimilarity = skeletalSimilarity,
        TotalEdges = totalEdges,
        PreservedEdges = preservedEdges,
        ValidationPassed = validationPassed,
        Message = message,
      };

      return await Task.FromResult(result);
    };
  }

  /// <summary>
  /// Computes k-NN skeletal similarity between two embedding sets.
  /// </summary>
  /// <remarks>
  /// Builds k-NN graphs for both embeddings and measures the proportion of edges
  /// that are preserved between them. Higher similarity indicates better preservation
  /// of neighborhood structure.
  /// </remarks>
  /// <returns>Tuple of (similarity score, total edges, preserved edges).</returns>
  private static (double similarity, int totalEdges, int preservedEdges) ComputeSkeletalSimilarity(
    List<UmapOutputRow> pythonEmbeddings,
    List<UmapOutputRow> csharpEmbeddings,
    int k
  )
  {
    int n = pythonEmbeddings.Count;

    // Build k-NN graphs for both embeddings
    var pythonKnn = BuildKnnGraph(pythonEmbeddings, k);
    var csharpKnn = BuildKnnGraph(csharpEmbeddings, k);

    // Count preserved edges
    int preservedEdges = 0;
    int totalEdges = n * k;

    for (int i = 0; i < n; i++)
    {
      var pythonNeighbors = pythonKnn[i];
      var csharpNeighbors = csharpKnn[i];

      // Count how many neighbors are the same
      var intersection = pythonNeighbors.Intersect(csharpNeighbors).Count();
      preservedEdges += intersection;
    }

    double similarity = (double)preservedEdges / totalEdges;
    return (similarity, totalEdges, preservedEdges);
  }

  /// <summary>
  /// Builds a k-NN graph for a set of embeddings.
  /// </summary>
  /// <returns>Array where each index contains the k nearest neighbor indices.</returns>
  private static HashSet<int>[] BuildKnnGraph(List<UmapOutputRow> embeddings, int k)
  {
    int n = embeddings.Count;
    var knnGraph = new HashSet<int>[n];

    for (int i = 0; i < n; i++)
    {
      // Compute distances to all other points
      var distances = new List<(int index, double distance)>();

      for (int j = 0; j < n; j++)
      {
        if (i == j)
        {
          continue;
        }

        var dist = EuclideanDistance(embeddings[i], embeddings[j]);
        distances.Add((j, dist));
      }

      // Get k nearest neighbors
      var neighbors = distances.OrderBy(d => d.distance).Take(k).Select(d => d.index);

      knnGraph[i] = new HashSet<int>(neighbors);
    }

    return knnGraph;
  }

  /// <summary>
  /// Computes Euclidean distance between two 2D embeddings.
  /// </summary>
  private static double EuclideanDistance(UmapOutputRow a, UmapOutputRow b)
  {
    var dx = a.Component0 - b.Component0;
    var dy = a.Component1 - b.Component1;
    return Math.Sqrt(dx * dx + dy * dy);
  }

  /// <summary>
  /// Builds a descriptive message about the comparison result.
  /// </summary>
  private static string BuildResultMessage(
    string datasetName,
    bool countsMatch,
    bool dimensionsMatch,
    int pythonCount,
    int csharpCount,
    int pythonDim,
    int csharpDim,
    double skeletalSimilarity,
    double minSimilarity,
    bool validationPassed
  )
  {
    if (validationPassed)
    {
      return $"✓ Validation passed for {datasetName}: "
        + $"{pythonCount} samples, {pythonDim}D, "
        + $"skeletal similarity {skeletalSimilarity:P2} (threshold: {minSimilarity:P2})";
    }

    var errors = new List<string>();
    if (!countsMatch)
    {
      errors.Add($"Sample count mismatch: Python={pythonCount}, C#={csharpCount}");
    }
    if (!dimensionsMatch)
    {
      errors.Add($"Dimension mismatch: Python={pythonDim}, C#={csharpDim}");
    }
    if (skeletalSimilarity < minSimilarity)
    {
      errors.Add($"Skeletal similarity {skeletalSimilarity:P2} below threshold {minSimilarity:P2}");
    }

    return $"✗ Validation failed for {datasetName}: {string.Join("; ", errors)}";
  }
}
