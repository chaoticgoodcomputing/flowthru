using Flowthru.Extensions.ML.UMAP.Algorithms;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Single;
using Microsoft.ML;

namespace Flowthru.Extensions.ML.UMAP;

/// <summary>
/// Trains a UMAP model - pure Python port.
/// Based on the Python UMAP implementation by Leland McInnes.
/// This is an unoptimized, direct translation prioritizing correctness.
/// </summary>
public sealed class UmapTrainer
{
  private readonly UmapOptions _options;
  private readonly MLContext _environment;

  public UmapTrainer(MLContext environment, UmapOptions? options = null)
  {
    _environment = environment ?? throw new ArgumentNullException(nameof(environment));
    _options = options ?? new UmapOptions();
    _options.Validate();
  }

  /// <summary>
  /// Train UMAP on float[][] data.
  /// Python reference: UMAP.fit() method in umap_.py (lines ~2700-3200)
  /// </summary>
  public UmapModelParameters Fit(float[][] data)
  {
    if (data == null || data.Length == 0)
    {
      throw new ArgumentException("Data cannot be null or empty", nameof(data));
    }

    var dataMatrix = DenseMatrix.OfRowArrays(data);
    return FitInternal(dataMatrix);
  }

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

    if (nSamples < _options.NumberOfNeighbors)
    {
      throw new ArgumentException(
        $"Number of samples ({nSamples}) must be at least {_options.NumberOfNeighbors}",
        nameof(data)
      );
    }

    // Determine number of epochs (Python: lines ~2750-2760)
    int nEpochs = _options.NumberOfEpochs ?? (nSamples <= 10000 ? 500 : 200);

    var random = Utils.CreateRandom(_options.RandomState);

    // Step 1: Compute k-nearest neighbors
    // Python: nearest_neighbors function call (line ~2800)
    if (_options.Verbosity >= 1)
    {
      Console.WriteLine($"Computing {_options.NumberOfNeighbors}-nearest neighbors...");
    }

    _options.ProgressReporter?.Report(("K-NN", 0.0f, "Computing nearest neighbors"));

    var metric = Distances.GetMetric(_options.Metric);
    var (knnIndices, knnDistances) = NearestNeighbors.ComputeKnn(
      data,
      _options.NumberOfNeighbors,
      metric
    );

    _options.ProgressReporter?.Report(("K-NN", 1.0f, "Nearest neighbors computed"));

    // Step 2: Compute fuzzy simplicial set
    // Python: smooth_knn_dist call (line ~2850)
    if (_options.Verbosity >= 1)
    {
      Console.WriteLine("Computing fuzzy simplicial set...");
    }

    _options.ProgressReporter?.Report(("Fuzzy Set", 0.0f, "Computing membership strengths"));

    var (sigmas, rhos) = FuzzySimplicialSet.SmoothKnnDist(
      knnDistances,
      (float)_options.NumberOfNeighbors,
      _options.LocalConnectivity
    );

    var graph = FuzzySimplicialSet.ComputeMembershipStrengths(
      knnIndices,
      knnDistances,
      sigmas,
      rhos,
      _options.SetOpMixRatio
    );

    _options.ProgressReporter?.Report(("Fuzzy Set", 1.0f, "Fuzzy simplicial set constructed"));

    // Step 3: Find a/b parameters for curve fitting
    // Python: find_ab_params call (line ~2730)
    float a,
      b;
    if (_options.A.HasValue && _options.B.HasValue)
    {
      a = _options.A.Value;
      b = _options.B.Value;
    }
    else
    {
      (a, b) = FuzzySimplicialSet.FindAbParams(_options.Spread, _options.MinDist);
    }

    if (_options.Verbosity >= 1)
    {
      Console.WriteLine($"Using a={a:F4}, b={b:F4}");
    }

    // Step 4: Initialize embedding
    // Python: spectral_layout or random initialization (lines ~2900-2950)
    if (_options.Verbosity >= 1)
    {
      Console.WriteLine($"Initializing embedding ({_options.Init})...");
    }

    _options.ProgressReporter?.Report(("Initialization", 0.0f, "Initializing embedding"));

    Matrix<float> embedding;
    switch (_options.Init.ToLowerInvariant())
    {
      case "random":
        embedding = Spectral.RandomInit(nSamples, _options.NumberOfComponents, random);
        break;
      case "spectral":
      case "pca":
        // Use random for pure implementation (spectral/PCA require complex eigenvalue computation)
        embedding = Spectral.SpectralLayout(graph, _options.NumberOfComponents, random);
        break;
      default:
        throw new ArgumentException($"Unknown init method: {_options.Init}");
    }

    _options.ProgressReporter?.Report(("Initialization", 1.0f, "Embedding initialized"));

    // Step 5: Optimize embedding using SGD
    // Python: simplicial_set_embedding -> optimize_layout_euclidean (lines ~3000-3100)
    if (_options.Verbosity >= 1)
    {
      Console.WriteLine($"Optimizing embedding ({nEpochs} epochs)...");
    }

    _options.ProgressReporter?.Report(("Optimization", 0.0f, $"Starting {nEpochs} epochs"));

    embedding = Layout.OptimizeLayoutEuclidean(
      embedding,
      graph,
      nEpochs,
      _options.LearningRate,
      a,
      b,
      _options.RepulsionStrength,
      _options.NegativeSampleRate,
      random,
      _options.ProgressReporter
    );

    _options.ProgressReporter?.Report(("Optimization", 1.0f, "Optimization complete"));

    if (_options.Verbosity >= 1)
    {
      Console.WriteLine("UMAP training complete.");
    }

    return new UmapModelParameters(
      embedding,
      data,
      knnIndices,
      knnDistances,
      a,
      b,
      _options,
      sigmas,
      rhos
    );
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
