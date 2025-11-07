using Flowthru.Extensions.ML.UMAP.Algorithms;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Single;
using Microsoft.ML;
using Microsoft.ML.Runtime;

namespace Flowthru.Extensions.ML.UMAP;

/// <summary>
/// Transforms data using a trained UMAP model.
/// </summary>
/// <remarks>
/// Projects new data points into the learned low-dimensional embedding space.
/// Based on the UMAP Python implementation by Leland McInnes.
/// <para>
/// Citation: McInnes, L, Healy, J, "UMAP: Uniform Manifold Approximation and Projection
/// for Dimension Reduction", ArXiv e-prints 1802.03426, 2018
/// https://arxiv.org/abs/1802.03426
/// </para>
/// </remarks>
public sealed class UmapTransformer
{
  private readonly UmapModelParameters _model;
  private readonly MLContext _environment;

  /// <summary>
  /// Creates a new UMAP transformer from trained model parameters.
  /// </summary>
  public UmapTransformer(MLContext environment, UmapModelParameters model)
  {
    _environment = environment ?? throw new ArgumentNullException(nameof(environment));
    _model = model ?? throw new ArgumentNullException(nameof(model));
  }

  /// <summary>
  /// Transforms new data points into the learned embedding space.
  /// </summary>
  /// <param name="data">New data to transform.</param>
  /// <returns>Transformed low-dimensional embedding.</returns>
  public float[][] Transform(float[][] data)
  {
    if (data == null || data.Length == 0)
    {
      throw new ArgumentException("Data cannot be null or empty", nameof(data));
    }

    var dataMatrix = DenseMatrix.OfRowArrays(data);
    var transformed = TransformInternal(dataMatrix);
    return transformed.ToRowArrays();
  }

  /// <summary>
  /// Transforms new data points into the learned embedding space.
  /// </summary>
  public Matrix<float> Transform(Matrix<float> data)
  {
    if (data == null)
    {
      throw new ArgumentNullException(nameof(data));
    }

    return TransformInternal(data);
  }

  private Matrix<float> TransformInternal(Matrix<float> newData)
  {
    int nNewSamples = newData.RowCount;
    int nFeatures = newData.ColumnCount;

    if (nFeatures != _model.InputDimension)
    {
      throw new ArgumentException(
        $"Input data has {nFeatures} features, but model expects {_model.InputDimension}",
        nameof(newData)
      );
    }

    // For each new point, find its nearest neighbors in the training data
    var metric = DistanceMetrics.GetMetric(_model.Options.Metric);
    var transformed = DenseMatrix.Create(nNewSamples, _model.EmbeddingDimension, 0f);

    for (int i = 0; i < nNewSamples; i++)
    {
      var point = newData.Row(i).AsArray();

      // Find k nearest neighbors in training data
      var neighbors = new (int Index, float Distance)[_model.Options.NumberOfNeighbors];

      for (int j = 0; j < _model.TrainingData.RowCount; j++)
      {
        var trainPoint = _model.TrainingData.Row(j).AsArray();
        float distance = metric(point, trainPoint);

        // Insert into sorted neighbors list
        if (j < _model.Options.NumberOfNeighbors)
        {
          neighbors[j] = (j, distance);
          if (j == _model.Options.NumberOfNeighbors - 1)
          {
            Array.Sort(neighbors, (a, b) => a.Distance.CompareTo(b.Distance));
          }
        }
        else if (distance < neighbors[^1].Distance)
        {
          neighbors[^1] = (j, distance);
          Array.Sort(neighbors, (a, b) => a.Distance.CompareTo(b.Distance));
        }
      }

      // Compute weighted average of neighbor embeddings
      float totalWeight = 0f;
      var embeddingSum = new float[_model.EmbeddingDimension];

      foreach (var (neighborIdx, distance) in neighbors)
      {
        // Use exponential decay as weight (similar to membership strength)
        float weight = MathF.Exp(-distance);
        totalWeight += weight;

        for (int d = 0; d < _model.EmbeddingDimension; d++)
        {
          embeddingSum[d] += weight * _model.Embedding[neighborIdx, d];
        }
      }

      // Normalize by total weight
      if (totalWeight > 0)
      {
        for (int d = 0; d < _model.EmbeddingDimension; d++)
        {
          transformed[i, d] = embeddingSum[d] / totalWeight;
        }
      }
    }

    return transformed;
  }

  /// <summary>
  /// Gets the trained model parameters.
  /// </summary>
  public UmapModelParameters Model => _model;
}
