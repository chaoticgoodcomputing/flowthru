using Flowthru.Misc.ML.UMAP;
using Flowthru.Misc.ML.UMAP.Core;
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
    public required UmapParameters UmapParameters { get; init; }

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

    public string InitStrategy { get; init; } = "random";
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
      var csharpRuntimeReports = new List<UmapRuntimeReport>();

      for (int trial = 0; trial < options.NumTrials; trial++)
      {
        var trialResult = await RunCSharpUmapTrial(
          inputFeatures,
          options.UmapParameters,
          options.InitStrategy,
          trial
        );

        // Compute preservation (reusing precomputed high-dim k-NN)
        var preservation = ComputeNeighborhoodPreservationWithKnn(
          originalKnn,
          trialResult.Item1,
          options.KNeighbors
        );
        csharpPreservations.Add(preservation);
        csharpRuntimeReports.Add(trialResult.Item2);
        Console.WriteLine($"   Trial {trial + 1}/{options.NumTrials}: {preservation:P2}");
      }

      // Compute C# statistics
      var csharpMean = csharpPreservations.Average();
      var csharpStdDev = ComputeStdDev(csharpPreservations);
      var csharpMin = csharpPreservations.Min();
      var csharpMax = csharpPreservations.Max();
      var preservationDiff = Math.Abs(pythonPreservation - csharpMean);

      // Average runtime metrics across all trials
      var avgTimings = AverageTimings(csharpRuntimeReports);
      var avgTotalTime = (int)csharpRuntimeReports.Average(r => r.TotalTimeMs);

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
        CSharpAvgTimings = avgTimings,
        CSharpAvgTotalTimeMs = avgTotalTime,
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
    IEnumerable<UmapOutputRow> embedding,
    int k
  )
  {
    int n = embedding.Count();

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
  private static async Task<(
    IEnumerable<UmapOutputRow> Embedding,
    UmapRuntimeReport RuntimeReport
  )> RunCSharpUmapTrial(
    float[][] inputFeatures,
    UmapParameters baseOptions,
    string initStrategy,
    int trialIndex
  )
  {
    // Create trial-specific options with different seed
    var trialOptions = new UmapParameters
    {
      NumberOfNeighbors = baseOptions.NumberOfNeighbors,
      LearningRate = baseOptions.LearningRate,
      MinDist = baseOptions.MinDist,
      NumberOfComponents = baseOptions.NumberOfComponents,
      RandomSeed = baseOptions.RandomSeed + trialIndex,
      NumberOfEpochs = baseOptions.NumberOfEpochs,
      Verbosity = 0, // Silent for trials
    };

    // Use high-level API that returns full result including runtime report
    var matrix = MathNet.Numerics.LinearAlgebra.Single.DenseMatrix.OfRowArrays(inputFeatures);
    var fitResult = UmapPipeline.Create(trialOptions).FitTransformWithReport(matrix);

    // Convert matrix result to output schema
    var embedding = Enumerable
      .Range(0, fitResult.Embedding.RowCount)
      .Select(i => new UmapOutputRow
      {
        Component0 = fitResult.Embedding[i, 0],
        Component1 = fitResult.Embedding[i, 1],
      });

    return await Task.FromResult((embedding, fitResult.RuntimeReport));
  }

  /// <summary>
  /// Averages timing metrics across multiple runtime reports.
  /// </summary>
  private static Dictionary<string, int> AverageTimings(List<UmapRuntimeReport> reports)
  {
    if (reports.Count == 0)
    {
      return new Dictionary<string, int>();
    }

    // Collect all unique stage names
    var allStages = reports.SelectMany(r => r.Timings.Keys).Distinct().ToList();

    // Compute average for each stage
    var avgTimings = new Dictionary<string, int>();
    foreach (var stage in allStages)
    {
      var stageTimings = reports
        .Where(r => r.Timings.ContainsKey(stage))
        .Select(r => r.Timings[stage])
        .ToList();

      if (stageTimings.Count > 0)
      {
        avgTimings[stage] = (int)stageTimings.Average();
      }
    }

    return avgTimings;
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
  private static int[][] BuildKnnGraphLowDim(IEnumerable<UmapOutputRow> embeddings, int k)
  {
    int n = embeddings.Count();
    var knn = new int[n][];

    for (int i = 0; i < n; i++)
    {
      var current = embeddings.ElementAt(i);
      var distances = new (int index, double distance)[n];

      for (int j = 0; j < n; j++)
      {
        double dist =
          (i == j)
            ? double.MaxValue
            : Math.Sqrt(
              Math.Pow(current.Component0 - embeddings.ElementAt(j).Component0, 2)
                + Math.Pow(current.Component1 - embeddings.ElementAt(j).Component1, 2)
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
  /// Computes a signed confidence that C# is not worse than the Python score.
  /// Positive values (0..1] indicate C# mean is likely >= Python (larger = more confident).
  /// Negative values [-1..0) indicate C# mean is likely < Python (more negative = more confident it's worse).
  /// Uses a one-sided normal approximation on the difference of means with standard error = sd / sqrt(n).
  /// </summary>
  private static double ComputeStatisticalConfidence(
    double pythonScore,
    List<double> csharpScores,
    double csharpStdDev
  )
  {
    int n = csharpScores?.Count ?? 0;
    if (n == 0)
    {
      return 0.0;
    }

    double csharpMean = csharpScores!.Average();

    // If no variance, return a signed hard decision.
    if (csharpStdDev < 1e-12 || n == 1)
    {
      return csharpMean >= pythonScore ? 1.0 : -1.0;
    }

    // Standard error of the mean
    double se = csharpStdDev / Math.Sqrt(n);

    // z for difference (positive means C# mean > Python)
    double z = (csharpMean - pythonScore) / se;

    // Convert z to one-sided probability that true mean > pythonScore:
    // phi(z) = NormalCDF(z)
    double phi = NormalCdf(z);

    // Map phi (0..1) to signed range [-1..1], where 0 means undecided, positive favors C#, negative disfavors.
    double signedConfidence = (phi - 0.5) * 2.0;

    // Clamp for numerical safety
    if (signedConfidence > 1.0)
      signedConfidence = 1.0;
    if (signedConfidence < -1.0)
      signedConfidence = -1.0;

    return signedConfidence;
  }

  // Standard normal CDF via erf approximation (Abramowitz & Stegun)
  private static double NormalCdf(double x)
  {
    // CDF = 0.5 * (1 + erf(x / sqrt(2)))
    return 0.5 * (1.0 + Erf(x / Math.Sqrt(2.0)));
  }

  // Approximation of the error function erf(x)
  private static double Erf(double x)
  {
    // Abramowitz and Stegun formula 7.1.26
    // Absolute error < 1.5e-7
    double sign = x < 0 ? -1.0 : 1.0;
    x = Math.Abs(x);

    double a1 = 0.254829592;
    double a2 = -0.284496736;
    double a3 = 1.421413741;
    double a4 = -1.453152027;
    double a5 = 1.061405429;
    double p = 0.3275911;

    double t = 1.0 / (1.0 + p * x);
    double y = 1.0 - (((((a5 * t + a4) * t) + a3) * t + a2) * t + a1) * t * Math.Exp(-x * x);

    return sign * y;
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
