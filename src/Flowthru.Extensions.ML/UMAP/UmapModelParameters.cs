using MathNet.Numerics.LinearAlgebra;

namespace Flowthru.Extensions.ML.UMAP;

/// <summary>
/// Model parameters for a trained UMAP model.
/// </summary>
/// <remarks>
/// Pure implementation - contains the trained embedding and parameters.
/// Based on the Python UMAP implementation by Leland McInnes.
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
  /// Sigmas from fuzzy simplicial set construction.
  /// </summary>
  public float[] Sigmas { get; }

  /// <summary>
  /// Rhos from fuzzy simplicial set construction.
  /// </summary>
  public float[] Rhos { get; }

  public UmapModelParameters(
    Matrix<float> embedding,
    Matrix<float> trainingData,
    int[][] knnIndices,
    float[][] knnDistances,
    float a,
    float b,
    UmapOptions options,
    float[] sigmas,
    float[] rhos
  )
  {
    Embedding = embedding ?? throw new ArgumentNullException(nameof(embedding));
    TrainingData = trainingData ?? throw new ArgumentNullException(nameof(trainingData));
    KnnIndices = knnIndices ?? throw new ArgumentNullException(nameof(knnIndices));
    KnnDistances = knnDistances ?? throw new ArgumentNullException(nameof(knnDistances));
    A = a;
    B = b;
    Options = options ?? throw new ArgumentNullException(nameof(options));
    Sigmas = sigmas ?? throw new ArgumentNullException(nameof(sigmas));
    Rhos = rhos ?? throw new ArgumentNullException(nameof(rhos));
  }

  public int EmbeddingDimension => Embedding.ColumnCount;
  public int InputDimension => TrainingData.ColumnCount;
  public int NumberOfSamples => Embedding.RowCount;
}
