using Flowthru.Extensions.MLPure.UMAP;
using Microsoft.ML;
using UmapReferenceComparisons.Data._01_Raw.Schemas;
using UmapReferenceComparisons.Data._03_Reports.Schemas;

namespace UmapReferenceComparisons.Helpers.Nodes;

/// <summary>
/// Compares C# UMAP implementation against Python reference output using neighborhood preservation validation.
/// </summary>
/// <remarks>
/// <para><strong>Generic Multi-Trial Validation:</strong></para>
/// <para>
/// This node validates UMAP correctness by measuring how well each implementation preserves
/// the original high-dimensional neighborhood structure. Since UMAP uses random initialization,
/// we run multiple C# trials with different seeds and use statistical testing to confirm
/// that C# and Python implementations are mathematically equivalent.
/// </para>
/// <para>
/// <strong>Key Metrics:</strong>
/// </para>
/// <list type="number">
/// <item><strong>Python Neighborhood Preservation:</strong> Proportion of high-dim k-NN edges preserved in Python embedding</item>
/// <item><strong>C# Neighborhood Preservation:</strong> Distribution of preservation scores across multiple trials</item>
/// </list>
/// <para>
/// <strong>Parameterization:</strong>
/// </para>
/// <para>
/// The node is fully parameterized to work with any dataset and UMAP configuration:
/// </para>
/// <list type="bullet">
/// <item>Dataset name for logging</item>
/// <item>Feature extraction function (converts input rows to float[][])</item>
/// <item>UMAP hyperparameters (n_neighbors, learning_rate, min_dist, etc.)</item>
/// <item>Validation thresholds (minimum preservation, confidence, trials)</item>
/// </list>
/// </remarks>
public static class CompareUmapImplementationsNode
{
  /// <summary>
  /// Configuration for UMAP comparison validation.
  /// </summary>
  public record Params
  {
    /// <summary>
    /// Name of the dataset being validated (e.g., "iris", "digits").
    /// </summary>
    public required string DatasetName { get; init; }

    /// <summary>
    /// UMAP hyperparameters for C# trials.
    /// </summary>
    public required UmapOptions UmapOptions { get; init; }

    /// <summary>
    /// Number of nearest neighbors to use for preservation metrics.
    /// Should match or be close to the n_neighbors parameter used in UMAP.
    /// </summary>
    public int KNeighbors { get; init; } = 15;

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

  /// <summary>
  /// Creates a UMAP comparison node that validates C# implementation against Python reference.
  /// </summary>
  /// <remarks>
  /// Expects input data in universal <see cref="UmapInput"/> format with pre-extracted features.
  /// </remarks>
  public static Func<
    (
      IEnumerable<UmapInput> inputData,
      IEnumerable<UmapOutputRow> pythonOutput,
      IEnumerable<UmapInput> csharpTrialInputs
    ),
    Task<ComparisonResult>
  > Create(Params options)
  {
    return async (input) =>
    {
      var (inputData, pythonOutput, _) = input;

      var inputList = inputData.ToList();
      var pythonList = pythonOutput.ToList();

      Console.WriteLine($"\n=== UMAP Validation: {options.DatasetName} ===");
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

      // Extract feature vectors from UmapInput
      var inputFeatures = inputList.Select(row => row.Features).ToArray();

      // Pre-compute high-dimensional k-NN graph (shared across all metrics)
      Console.WriteLine($"\n1. Building high-dimensional k-NN graph (k={options.KNeighbors})...");
      var originalKnn = BuildKnnGraphHighDim(inputFeatures, options.KNeighbors);
      Console.WriteLine($"   High-dimensional k-NN graph computed.");

      // METRIC 1: Python neighborhood preservation
      Console.WriteLine($"\n2. Computing Python neighborhood preservation...");
      var pythonPreservation = ComputeNeighborhoodPreservationWithKnn(
        originalKnn,
        pythonList,
        options.KNeighbors
      );
      Console.WriteLine($"   Python preservation: {pythonPreservation:P2}");

      // METRIC 2: Run multiple C# trials
      Console.WriteLine($"\n3. Running {options.NumTrials} C# UMAP trials with different seeds...");
      var csharpPreservations = new List<double>();

      for (int trial = 0; trial < options.NumTrials; trial++)
      {
        var csharpEmbedding = await RunCSharpUmapTrial(inputFeatures, options.UmapOptions, trial);

        // Compute preservation (reusing precomputed high-dim k-NN)
        var preservation = ComputeNeighborhoodPreservationWithKnn(
          originalKnn,
          csharpEmbedding,
          options.KNeighbors
        );
        csharpPreservations.Add(preservation);
        Console.WriteLine($"   Trial {trial + 1}/{options.NumTrials}: {preservation:P2}");
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

      // Determine if validation passed (only relative comparison matters)
      var validationPassed =
        countsMatch
        && dimensionsMatch
        && preservationDiff <= options.MaxPreservationDifference
        && confidence >= options.MinimumConfidence;

      var message = BuildResultMessage(
        options.DatasetName,
        pythonPreservation,
        csharpMean,
        preservationDiff,
        confidence,
        options,
        validationPassed
      );

      Console.WriteLine($"\n{message}");

      var result = new ComparisonResult
      {
        Dataset = options.DatasetName,
        PythonSampleCount = pythonList.Count,
        CSharpSampleCount = inputList.Count,
        PythonDimensionCount = 2,
        CSharpDimensionCount = 2,
        CountsMatch = countsMatch,
        DimensionsMatch = dimensionsMatch,
        KNeighbors = options.KNeighbors,
        PythonNeighborhoodPreservation = pythonPreservation,
        CSharpNeighborhoodPreservation = csharpPreservations.First(), // Primary trial
        CSharpMeanPreservation = csharpMean,
        CSharpStdDevPreservation = csharpStdDev,
        CSharpMinPreservation = csharpMin,
        CSharpMaxPreservation = csharpMax,
        PreservationDifference = preservationDiff,
        StatisticalConfidence = confidence,
        ValidationPassed = validationPassed,
        Message = message,
      };

      return result;
    };
  }

  /// <summary>
  /// Computes neighborhood preservation using a precomputed high-dimensional k-NN graph.
  /// </summary>
  private static double ComputeNeighborhoodPreservationWithKnn(
    int[][] originalKnn,
    List<UmapOutputRow> embedding,
    int k
  )
  {
    int n = embedding.Count;

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
  /// Runs a C# UMAP trial with a specific random seed.
  /// </summary>
  private static async Task<List<UmapOutputRow>> RunCSharpUmapTrial(
    float[][] inputFeatures,
    UmapOptions baseOptions,
    int trialIndex
  )
  {
    // Create trial-specific options with different seed
    var trialOptions = new UmapOptions
    {
      NumberOfNeighbors = baseOptions.NumberOfNeighbors,
      LearningRate = baseOptions.LearningRate,
      MinDist = baseOptions.MinDist,
      NumberOfComponents = baseOptions.NumberOfComponents,
      RandomState = baseOptions.RandomState + trialIndex,
      Metric = baseOptions.Metric,
      NumberOfEpochs = baseOptions.NumberOfEpochs,
      Verbosity = 0, // Silent for trials
    };

    var mlContext = new MLContext(seed: trialOptions.RandomState);
    var trainer = mlContext.CreateUmapTrainer(trialOptions);
    var (_, embedding) = trainer.FitTransform(inputFeatures);

    return await Task.FromResult(
      embedding
        .Select(emb => new UmapOutputRow { Component0 = emb[0], Component1 = emb[1] })
        .ToList()
    );
  }

  /// <summary>
  /// Builds k-NN graph in HIGH-dimensional (original) space using Euclidean distance.
  /// </summary>
  private static int[][] BuildKnnGraphHighDim(float[][] data, int k)
  {
    int n = data.Length;
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
          double dist = 0;
          for (int d = 0; d < current.Length; d++)
          {
            double diff = current[d] - data[j][d];
            dist += diff * diff;
          }
          distances[j] = (j, Math.Sqrt(dist));
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

  /// <summary>
  /// Builds result message for validation.
  /// </summary>
  private static string BuildResultMessage(
    string datasetName,
    double pythonPreservation,
    double csharpMeanPreservation,
    double preservationDifference,
    double statisticalConfidence,
    Params config,
    bool validationPassed
  )
  {
    if (validationPassed)
    {
      return $"✓ Validation passed for {datasetName}: "
        + $"Python {pythonPreservation:P2}, "
        + $"C# {csharpMeanPreservation:P2}, "
        + $"diff {preservationDifference:P2} (max: {config.MaxPreservationDifference:P2}), "
        + $"confidence {statisticalConfidence:P2} (min: {config.MinimumConfidence:P2})";
    }

    var errors = new List<string>();
    if (preservationDifference > config.MaxPreservationDifference)
    {
      errors.Add(
        $"Preservation difference {preservationDifference:P2} exceeds threshold {config.MaxPreservationDifference:P2}"
      );
    }
    if (statisticalConfidence < config.MinimumConfidence)
    {
      errors.Add(
        $"Statistical confidence {statisticalConfidence:P2} below threshold {config.MinimumConfidence:P2}"
      );
    }

    return $"✗ Validation failed for {datasetName}: {string.Join("; ", errors)}";
  }
}
