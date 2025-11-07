using Flowthru.Extensions.ML.UMAP.Algorithms;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Single;
using Microsoft.ML;
using Microsoft.ML.Data;
using Microsoft.ML.Runtime;

namespace Flowthru.Extensions.ML.UMAP;

/// <summary>
/// Trains a UMAP (Uniform Manifold Approximation and Projection) dimensionality reduction model.
/// </summary>
/// <remarks>
/// Based on the UMAP Python implementation by Leland McInnes.
/// <para>
/// UMAP is a manifold learning technique for dimensionality reduction that preserves
/// both local and global structure of high-dimensional data.
/// </para>
/// <para>
/// Citation: McInnes, L, Healy, J, "UMAP: Uniform Manifold Approximation and Projection
/// for Dimension Reduction", ArXiv e-prints 1802.03426, 2018
/// https://arxiv.org/abs/1802.03426
/// </para>
/// <para>
/// Additional Reference: Healy, J., McInnes, L. "Uniform manifold approximation and projection"
/// Nat Rev Methods Primers 4, 82 (2024). https://doi.org/10.1038/s43586-024-00363-x
/// </para>
/// </remarks>
public sealed class UmapTrainer
{
  private readonly UmapOptions _options;
  private readonly MLContext _environment;

  /// <summary>
  /// Creates a new UMAP trainer with the specified options.
  /// </summary>
  public UmapTrainer(MLContext environment, UmapOptions? options = null)
  {
    _environment = environment ?? throw new ArgumentNullException(nameof(environment));
    _options = options ?? new UmapOptions();
    _options.Validate();
  }

  /// <summary>
  /// Trains a UMAP model on the provided data.
  /// </summary>
  /// <param name="data">Input data where each row is a sample and each column is a feature.</param>
  /// <returns>Trained UMAP model parameters.</returns>
  public UmapModelParameters Fit(float[][] data)
  {
    if (data == null || data.Length == 0)
    {
      throw new ArgumentException("Data cannot be null or empty", nameof(data));
    }

    int nSamples = data.Length;
    int nFeatures = data[0].Length;

    if (nSamples < _options.NumberOfNeighbors)
    {
      throw new ArgumentException(
        $"Number of samples ({nSamples}) must be at least {_options.NumberOfNeighbors}",
        nameof(data)
      );
    }

    // Convert to matrix
    var dataMatrix = DenseMatrix.OfRowArrays(data);

    return FitInternal(dataMatrix);
  }

  /// <summary>
  /// Trains a UMAP model on the provided data matrix.
  /// </summary>
  public UmapModelParameters Fit(Matrix<float> data)
  {
    if (data == null)
    {
      throw new ArgumentNullException(nameof(data));
    }

    return FitInternal(data);
  }

  private UmapModelParameters FitInternal(Matrix<float> data)
  {
    int nSamples = data.RowCount;
    int nFeatures = data.ColumnCount;

    // Determine number of epochs
    int nEpochs = _options.NumberOfEpochs ?? (nSamples <= 10000 ? 500 : 200);

    // Determine whether to use approximate k-NN
    bool useApproximateKnn =
      _options.UseApproximateNearestNeighbors ?? (nSamples > 10000 && nFeatures > 50);

    if (_options.Verbosity >= 1)
    {
      Console.WriteLine($"\n=== UMAP Training ===");
      Console.WriteLine(
        $"Dataset: {nSamples:N0} samples × {nFeatures} features → {_options.NumberOfComponents} components"
      );
      Console.WriteLine(
        $"Parameters: n_neighbors={_options.NumberOfNeighbors}, min_dist={_options.MinDist}, metric={_options.Metric}"
      );
      Console.WriteLine($"Epochs: {nEpochs}, Learning rate: {_options.LearningRate}");
      Console.WriteLine(
        $"k-NN Method: {(useApproximateKnn ? $"Approximate (trees={_options.AnnNumTrees}, leaf_size={_options.AnnLeafSize})" : "Exact")}"
      );
      Console.WriteLine();
    }

    var progressReporter = _options.ProgressReporter;

    // Create random number generator
    var random = _options.RandomState.HasValue
      ? new Random(_options.RandomState.Value)
      : new Random();

    // Step 1: Compute k-nearest neighbors
    if (_options.Verbosity >= 1)
    {
      Console.WriteLine("Phase 1/5: Computing k-nearest neighbors...");
    }
    progressReporter?.Report(("Phase 1/5: k-NN", 0.0f, "Starting"));

    // Extract data rows once for efficiency
    var dataRows = new float[nSamples][];
    for (int i = 0; i < nSamples; i++)
    {
      dataRows[i] = data.Row(i).AsArray();
    }

    int[][] knnIndices;
    float[][] knnDistances;

    if (useApproximateKnn && _options.Metric.ToLowerInvariant() == "euclidean")
    {
      // Use approximate k-NN with Random Projection Trees
      (knnIndices, knnDistances) = ApproximateNearestNeighbors.ComputeApproximateKnn(
        dataRows,
        _options.NumberOfNeighbors,
        _options.AnnNumTrees,
        _options.AnnLeafSize,
        _options.AnnSearchK,
        _options.Verbosity,
        progressReporter,
        random
      );
    }
    else if (_options.Metric.ToLowerInvariant() == "euclidean")
    {
      // Use optimized exact Euclidean k-NN
      (knnIndices, knnDistances) = NearestNeighbors.ComputeKnnEuclidean(
        data,
        _options.NumberOfNeighbors,
        _options.Verbosity,
        progressReporter
      );
    }
    else
    {
      // Use exact k-NN with custom metric
      (knnIndices, knnDistances) = NearestNeighbors.ComputeKnn(
        data,
        _options.NumberOfNeighbors,
        DistanceMetrics.GetMetric(_options.Metric),
        _options.Verbosity,
        progressReporter
      );
    }

    // Step 2: Construct fuzzy simplicial set
    if (_options.Verbosity >= 1)
    {
      Console.WriteLine("\nPhase 2/5: Constructing fuzzy simplicial set...");
    }
    progressReporter?.Report(("Phase 2/5: Fuzzy Simplicial Set", 0.0f, "Starting"));

    var graph = FuzzySimplicialSet.FuzzySimplicialSetFromKnn(
      knnIndices,
      knnDistances,
      _options.NumberOfNeighbors,
      _options.LocalConnectivity,
      _options.SetOpMixRatio,
      _options.Verbosity,
      progressReporter
    );

    if (_options.Verbosity >= 1)
    {
      int nonZeroCount = graph.EnumerateIndexed().Count(e => e.Item3 > 0);
      Console.WriteLine($"Fuzzy simplicial set constructed: {nonZeroCount:N0} edges");
    }
    progressReporter?.Report(("Phase 2/5: Fuzzy Simplicial Set", 1.0f, "Complete"));

    // Step 3: Find a and b parameters for the UMAP curve
    if (_options.Verbosity >= 1)
    {
      Console.WriteLine("\nPhase 3/5: Computing curve parameters...");
    }
    progressReporter?.Report(("Phase 3/5: Curve Parameters", 0.0f, "Starting"));

    var (a, b) = Layout.FindAbParams(_options.Spread, _options.MinDist);

    if (_options.Verbosity >= 1)
    {
      Console.WriteLine($"UMAP curve parameters: a={a:F4}, b={b:F4}");
    }
    progressReporter?.Report(("Phase 3/5: Curve Parameters", 1.0f, $"a={a:F4}, b={b:F4}"));

    // Step 4: Initialize embedding
    if (_options.Verbosity >= 1)
    {
      Console.WriteLine("\nPhase 4/5: Initializing embedding...");
    }
    progressReporter?.Report(("Phase 4/5: Initialize Embedding", 0.0f, "Starting"));

    var embedding = Layout.InitializeEmbedding(graph, _options.NumberOfComponents, random);

    if (_options.Verbosity >= 1)
    {
      Console.WriteLine(
        $"Embedding initialized: {nSamples:N0} × {_options.NumberOfComponents} dimensions"
      );
    }
    progressReporter?.Report(("Phase 4/5: Initialize Embedding", 1.0f, "Complete"));

    // Step 5: Optimize embedding layout using SGD
    if (_options.Verbosity >= 1)
    {
      Console.WriteLine($"\nPhase 5/5: Optimizing embedding layout...");
    }
    progressReporter?.Report(("Phase 5/5: SGD Optimization", 0.0f, "Starting"));

    embedding = Layout.OptimizeLayout(
      graph,
      embedding,
      nEpochs,
      _options.LearningRate,
      a,
      b,
      _options.RepulsionStrength,
      _options.NegativeSampleRate,
      random,
      _options.Verbosity,
      progressReporter
    );

    if (_options.Verbosity >= 1)
    {
      Console.WriteLine($"\n=== UMAP Training Complete ===\n");
    }
    progressReporter?.Report(("Complete", 1.0f, "UMAP training finished"));

    // Create and return model parameters
    return new UmapModelParameters(embedding, data, knnIndices, knnDistances, a, b, _options);
  }

  /// <summary>
  /// Fits a UMAP model and immediately transforms the training data.
  /// </summary>
  public (UmapModelParameters Model, float[][] Embedding) FitTransform(float[][] data)
  {
    var model = Fit(data);
    var embedding = model.Embedding.ToRowArrays();
    return (model, embedding);
  }

  /// <summary>
  /// Fits a UMAP model and immediately transforms the training data.
  /// </summary>
  public (UmapModelParameters Model, Matrix<float> Embedding) FitTransform(Matrix<float> data)
  {
    var model = Fit(data);
    return (model, model.Embedding);
  }
}
