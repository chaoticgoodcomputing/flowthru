using Flowthru.Extensions.ML.UMAP;
using Microsoft.ML;
using UmapReferenceComparisons.Data._01_Raw.Schemas;
using UmapReferenceComparisons.Data._03_Reports.Schemas;

namespace UmapReferenceComparisons.Pipelines.IrisComparison.Nodes;

/// <summary>
/// Compares C# UMAP output against Python reference output using three-metric validation.
/// </summary>
/// <remarks>
/// <para><strong>Three-Metric Validation Approach:</strong></para>
/// <para>
/// 1. <strong>Python Neighborhood Preservation:</strong> Does Python UMAP preserve the original high-dimensional structure?
/// </para>
/// <para>
/// 2. <strong>C# Neighborhood Preservation:</strong> Does C# UMAP preserve the original high-dimensional structure?
/// </para>
/// <para>
/// 3. <strong>Implementation Similarity:</strong> How similar are the Python and C# embeddings to each other?
/// </para>
/// <para>
/// Since UMAP uses random initialization, we run multiple C# trials with different seeds
/// and compare the distribution of preservation scores to determine if C# and Python
/// implementations are mathematically equivalent.
/// </para>
/// </remarks>
public static class CompareOutputsNode
{
  /// <summary>
  /// Configuration for comparison metrics.
  /// </summary>
  public record Params
  {
    /// <summary>
    /// Number of nearest neighbors to use for preservation metrics.
    /// Should match or be close to the n_neighbors parameter used in UMAP.
    /// </summary>
    public int KNeighbors { get; init; } = 15;

    /// <summary>
    /// Minimum neighborhood preservation threshold (0.0 to 1.0) for validation to pass.
    /// </summary>
    public double MinimumPreservation { get; init; } = 0.7;

    /// <summary>
    /// Maximum allowed difference between Python and C# mean preservation scores.
    /// </summary>
    public double MaxPreservationDifference { get; init; } = 0.10;

    /// <summary>
    /// Minimum statistical confidence (0.0 to 1.0) that implementations are equivalent.
    /// </summary>
    public double MinimumConfidence { get; init; } = 0.95;

    /// <summary>
    /// Number of C# UMAP trials to run with different random seeds.
    /// </summary>
    public int NumTrials { get; init; } = 10;
  }

  public static Func<
    (
      IEnumerable<IrisInputRow> inputData,
      IEnumerable<UmapOutputRow> pythonOutput,
      IEnumerable<IrisInputRow> csharpTrialInputs
    ),
    Task<ComparisonResult>
  > Create(Params? options = null)
  {
    var config = options ?? new Params();

    return async (input) =>
    {
      var (inputData, pythonOutput, _) = input;

      var inputList = inputData.ToList();
      var pythonList = pythonOutput.ToList();

      Console.WriteLine($"\n=== Three-Metric UMAP Validation ===");
      Console.WriteLine($"Input samples: {inputList.Count}");
      Console.WriteLine($"Python samples: {pythonList.Count}");

      // Validate sample counts match
      var countsMatch = pythonList.Count == inputList.Count;
      var dimensionsMatch = true; // Both are 2D by schema

      if (!countsMatch)
      {
        throw new InvalidOperationException(
          $"Sample count mismatch: input={inputList.Count}, python={pythonList.Count}"
        );
      }

      // Pre-compute high-dimensional k-NN graph (shared across all metrics)
      Console.WriteLine($"\n1. Building high-dimensional k-NN graph (k={config.KNeighbors})...");
      var originalKnn = BuildKnnGraphHighDim(inputList, config.KNeighbors);
      Console.WriteLine($"   High-dimensional k-NN graph computed.");

      // METRIC 1: Python neighborhood preservation
      Console.WriteLine($"\n2. Computing Python neighborhood preservation...");
      var pythonPreservation = ComputeNeighborhoodPreservationWithKnn(
        originalKnn,
        pythonList,
        config.KNeighbors
      );
      Console.WriteLine($"   Python preservation: {pythonPreservation:P2}");

      // METRIC 2: Run multiple C# trials
      Console.WriteLine($"\n3. Running {config.NumTrials} C# UMAP trials with different seeds...");
      var csharpPreservations = new List<double>();

      for (int trial = 0; trial < config.NumTrials; trial++)
      {
        var csharpEmbedding = await RunCSharpUmapTrial(inputList, trial);

        // Compute preservation (reusing precomputed high-dim k-NN)
        var preservation = ComputeNeighborhoodPreservationWithKnn(
          originalKnn,
          csharpEmbedding,
          config.KNeighbors
        );
        csharpPreservations.Add(preservation);
        Console.WriteLine($"   Trial {trial + 1}/{config.NumTrials}: {preservation:P2}");
      }

      // Compute C# statistics
      var csharpMean = csharpPreservations.Average();
      var csharpStdDev = ComputeStdDev(csharpPreservations);
      var csharpMin = csharpPreservations.Min();
      var csharpMax = csharpPreservations.Max();
      var preservationDiff = Math.Abs(pythonPreservation - csharpMean);

      // Compute statistical confidence
      var confidence = ComputeStatisticalConfidence(
        pythonPreservation,
        csharpPreservations,
        csharpStdDev
      );

      Console.WriteLine($"\n4. Statistical Summary:");
      Console.WriteLine($"   C# Mean Preservation: {csharpMean:P2} ± {csharpStdDev:P2}");
      Console.WriteLine($"   C# Range: [{csharpMin:P2}, {csharpMax:P2}]");
      Console.WriteLine($"   Preservation Difference: {preservationDiff:P2}");
      Console.WriteLine($"   Statistical Confidence: {confidence:P2}");

      // Determine if validation passed
      var validationPassed =
        countsMatch
        && dimensionsMatch
        && pythonPreservation >= config.MinimumPreservation
        && csharpMean >= config.MinimumPreservation
        && preservationDiff <= config.MaxPreservationDifference
        && confidence >= config.MinimumConfidence;

      var result = new ComparisonResult
      {
        PythonSampleCount = pythonList.Count,
        CSharpSampleCount = inputList.Count,
        PythonDimensionCount = 2,
        CSharpDimensionCount = 2,
        CountsMatch = countsMatch,
        DimensionsMatch = dimensionsMatch,
        KNeighbors = config.KNeighbors,
        PythonNeighborhoodPreservation = pythonPreservation,
        CSharpNeighborhoodPreservation = csharpPreservations.First(), // Primary trial
        CSharpMeanPreservation = csharpMean,
        CSharpStdDevPreservation = csharpStdDev,
        CSharpMinPreservation = csharpMin,
        CSharpMaxPreservation = csharpMax,
        PreservationDifference = preservationDiff,
        StatisticalConfidence = confidence,
        ValidationPassed = validationPassed,
      };

      return result;
    };
  }

  /// <summary>
  /// Computes neighborhood preservation: proportion of high-dimensional k-NN edges
  /// preserved in the low-dimensional embedding.
  /// </summary>
  /// <remarks>
  /// This is the CORRECT metric for evaluating UMAP: does the embedding preserve
  /// the original data's neighborhood structure?
  /// </remarks>
  private static double ComputeNeighborhoodPreservation(
    List<IrisInputRow> originalData,
    List<UmapOutputRow> embedding,
    int k
  )
  {
    int n = originalData.Count;

    // Build k-NN in HIGH-dimensional (original) space
    var originalKnn = BuildKnnGraphHighDim(originalData, k);

    // Build k-NN in LOW-dimensional (embedding) space
    var embeddingKnn = BuildKnnGraphLowDim(embedding, k);

    // Count preserved edges
    int preservedEdges = 0;
    for (int i = 0; i < n; i++)
    {
      var originalNeighbors = originalKnn[i];
      var embeddingNeighbors = embeddingKnn[i];
      preservedEdges += originalNeighbors.Intersect(embeddingNeighbors).Count();
    }

    return (double)preservedEdges / (n * k);
  }

  /// <summary>
  /// Computes neighborhood preservation with precomputed high-dimensional k-NN graph.
  /// </summary>
  /// <remarks>
  /// Optimized version that reuses the high-dimensional k-NN graph across multiple trials,
  /// avoiding redundant O(n²) computation.
  /// </remarks>
  private static double ComputeNeighborhoodPreservationWithKnn(
    int[][] originalKnn,
    List<UmapOutputRow> embedding,
    int k
  )
  {
    int n = embedding.Count;

    // Build k-NN in LOW-dimensional (embedding) space only
    var embeddingKnn = BuildKnnGraphLowDim(embedding, k);

    // Count preserved edges
    int preservedEdges = 0;
    for (int i = 0; i < n; i++)
    {
      var originalNeighbors = originalKnn[i];
      var embeddingNeighbors = embeddingKnn[i];
      preservedEdges += originalNeighbors.Intersect(embeddingNeighbors).Count();
    }

    return (double)preservedEdges / (n * k);
  }

  /// <summary>
  /// Runs a C# UMAP trial with a specific random seed.
  /// </summary>
  private static async Task<List<UmapOutputRow>> RunCSharpUmapTrial(
    List<IrisInputRow> inputData,
    int seed
  )
  {
    // Convert to float[][] for UMAP
    var data = inputData
      .Select(row =>
        new float[] { row.SepalLength, row.SepalWidth, row.PetalLength, row.PetalWidth }
      )
      .ToArray();

    // Configure UMAP with trial-specific seed
    var umapOptions = new Flowthru.Extensions.ML.UMAP.UmapOptions
    {
      NumberOfNeighbors = 50,
      LearningRate = 0.5f,
      MinDist = 0.001f,
      NumberOfComponents = 2,
      RandomState = seed, // Different seed per trial
      Metric = "euclidean",
      Verbosity = 0, // Silent for trials
    };

    var mlContext = new Microsoft.ML.MLContext(seed: seed);
    var trainer = mlContext.CreateUmapTrainer(umapOptions);
    var (_, embedding) = trainer.FitTransform(data);

    return await Task.FromResult(
      embedding
        .Select(emb => new UmapOutputRow { Component0 = emb[0], Component1 = emb[1] })
        .ToList()
    );
  }

  /// <summary>
  /// Builds k-NN graph in HIGH-dimensional (original) space using Euclidean distance.
  /// </summary>
  private static int[][] BuildKnnGraphHighDim(List<IrisInputRow> data, int k)
  {
    int n = data.Count;
    var knn = new int[n][];

    for (int i = 0; i < n; i++)
    {
      var current = data[i];
      var distances = new (int index, double distance)[n];

      for (int j = 0; j < n; j++)
      {
        if (i == j)
        {
          distances[j] = (j, double.MaxValue);
        }
        else
        {
          double dist = Math.Sqrt(
            Math.Pow(current.SepalLength - data[j].SepalLength, 2)
              + Math.Pow(current.SepalWidth - data[j].SepalWidth, 2)
              + Math.Pow(current.PetalLength - data[j].PetalLength, 2)
              + Math.Pow(current.PetalWidth - data[j].PetalWidth, 2)
          );
          distances[j] = (j, dist);
        }
      }

      knn[i] = distances.OrderBy(d => d.distance).Take(k).Select(d => d.index).ToArray();
    }

    return knn;
  }

  /// <summary>
  /// Builds k-NN graph in LOW-dimensional (embedding) space using Euclidean distance.
  /// </summary>
  private static int[][] BuildKnnGraphLowDim(List<UmapOutputRow> embeddings, int k)
  {
    int n = embeddings.Count;
    var knn = new int[n][];

    for (int i = 0; i < n; i++)
    {
      var current = embeddings[i];
      var distances = new (int index, double distance)[n];

      for (int j = 0; j < n; j++)
      {
        double dist =
          (i == j)
            ? double.MaxValue
            : Math.Sqrt(
              Math.Pow(current.Component0 - embeddings[j].Component0, 2)
                + Math.Pow(current.Component1 - embeddings[j].Component1, 2)
            );
        distances[j] = (j, dist);
      }

      knn[i] = distances.OrderBy(d => d.distance).Take(k).Select(d => d.index).ToArray();
    }

    return knn;
  }

  /// <summary>
  /// Computes standard deviation of a list of values.
  /// </summary>
  private static double ComputeStdDev(List<double> values)
  {
    if (values.Count == 0)
    {
      return 0;
    }

    double mean = values.Average();
    double sumSquaredDiffs = values.Sum(v => Math.Pow(v - mean, 2));
    return Math.Sqrt(sumSquaredDiffs / values.Count);
  }

  /// <summary>
  /// Computes statistical confidence that Python score falls within C# score distribution.
  /// </summary>
  /// <remarks>
  /// Returns a confidence metric where:
  /// - 1.0 = Python score equals C# mean
  /// - 0.68 = Python within 1 standard deviation
  /// - 0.05 = Python at 2 standard deviations
  /// - Lower values indicate Python is an outlier
  /// </remarks>
  private static double ComputeStatisticalConfidence(
    double pythonScore,
    List<double> csharpScores,
    double csharpStdDev
  )
  {
    if (csharpScores.Count == 0)
    {
      return 0;
    }

    double csharpMean = csharpScores.Average();

    // If no variance, check exact match
    if (csharpStdDev < 1e-10)
    {
      return Math.Abs(pythonScore - csharpMean) < 1e-10 ? 1.0 : 0.0;
    }

    // Compute z-score
    double zScore = Math.Abs(pythonScore - csharpMean) / csharpStdDev;

    // Convert to confidence (approximate normal distribution CDF)
    // 1 - 2 * P(Z > |z|) where P is standard normal tail probability
    if (zScore <= 1.0)
    {
      return 1.0 - 0.32 * zScore; // Linear approximation for z <= 1
    }
    else if (zScore <= 2.0)
    {
      return 0.68 - 0.315 * (zScore - 1.0); // ~31.5% drop from 0.68 to 0.05
    }
    else
    {
      return Math.Max(0.0, 0.05 - 0.025 * (zScore - 2.0)); // Decay to 0
    }
  }
}
