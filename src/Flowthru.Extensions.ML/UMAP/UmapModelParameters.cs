using MathNet.Numerics.LinearAlgebra;
using Microsoft.ML;
using Microsoft.ML.Data;

namespace Flowthru.Extensions.ML.UMAP;

/// <summary>
/// Model parameters for a trained UMAP model.
/// </summary>
/// <remarks>
/// Contains the trained embedding and parameters needed for transforming new data.
/// Based on the UMAP Python implementation by Leland McInnes.
/// <para>
/// Citation: McInnes, L, Healy, J, "UMAP: Uniform Manifold Approximation and Projection
/// for Dimension Reduction", ArXiv e-prints 1802.03426, 2018
/// https://arxiv.org/abs/1802.03426
/// </para>
/// </remarks>
public sealed class UmapModelParameters
{
  /// <summary>
  /// The trained low-dimensional embedding.
  /// </summary>
  public Matrix<float> Embedding { get; }

  /// <summary>
  /// The original high-dimensional training data (stored for transform operations).
  /// </summary>
  public Matrix<float> TrainingData { get; }

  /// <summary>
  /// K-nearest neighbor indices from training.
  /// </summary>
  public int[][] KnnIndices { get; }

  /// <summary>
  /// K-nearest neighbor distances from training.
  /// </summary>
  public float[][] KnnDistances { get; }

  /// <summary>
  /// Parameter 'a' for the UMAP distance function: 1/(1 + a*x^(2b)).
  /// </summary>
  public float A { get; }

  /// <summary>
  /// Parameter 'b' for the UMAP distance function: 1/(1 + a*x^(2b)).
  /// </summary>
  public float B { get; }

  /// <summary>
  /// The options used to train this model.
  /// </summary>
  public UmapOptions Options { get; }

  /// <summary>
  /// Creates a new UMAP model parameters instance.
  /// </summary>
  public UmapModelParameters(
    Matrix<float> embedding,
    Matrix<float> trainingData,
    int[][] knnIndices,
    float[][] knnDistances,
    float a,
    float b,
    UmapOptions options
  )
  {
    Embedding = embedding ?? throw new ArgumentNullException(nameof(embedding));
    TrainingData = trainingData ?? throw new ArgumentNullException(nameof(trainingData));
    KnnIndices = knnIndices ?? throw new ArgumentNullException(nameof(knnIndices));
    KnnDistances = knnDistances ?? throw new ArgumentNullException(nameof(knnDistances));
    A = a;
    B = b;
    Options = options ?? throw new ArgumentNullException(nameof(options));
  }

  /// <summary>
  /// Gets the dimensionality of the low-dimensional embedding space.
  /// </summary>
  public int EmbeddingDimension => Embedding.ColumnCount;

  /// <summary>
  /// Gets the dimensionality of the original high-dimensional input space.
  /// </summary>
  public int InputDimension => TrainingData.ColumnCount;

  /// <summary>
  /// Gets the number of training samples.
  /// </summary>
  public int NumberOfSamples => Embedding.RowCount;
}
