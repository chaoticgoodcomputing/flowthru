using Flowthru.Extensions.MLPure.UMAP.Algorithms;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Single;
using Microsoft.ML;

namespace Flowthru.Extensions.MLPure.UMAP;

/// <summary>
/// Transforms data using a trained UMAP model - pure implementation.
/// Simplified transform using weighted average of nearest neighbors.
/// </summary>
public sealed class UmapTransformer
{
  private readonly UmapModelParameters _model;
  private readonly MLContext _environment;

  public UmapTransformer(MLContext environment, UmapModelParameters model)
  {
    _environment = environment ?? throw new ArgumentNullException(nameof(environment));
    _model = model ?? throw new ArgumentNullException(nameof(model));
  }

  /// <summary>
  /// Transform new data into embedding space.
  /// Pure implementation uses weighted average of nearest neighbor embeddings.
  /// </summary>
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

    var metric = Distances.GetMetric(_model.Options.Metric);
    var transformed = DenseMatrix.Create(nNewSamples, _model.EmbeddingDimension, 0f);

    for (int i = 0; i < nNewSamples; i++)
    {
      var point = newData.Row(i).ToArray();

      // Find k nearest neighbors in training data
      var distances = new List<(int index, float distance)>();
      for (int j = 0; j < _model.TrainingData.RowCount; j++)
      {
        var trainPoint = _model.TrainingData.Row(j).ToArray();
        float distance = metric(point, trainPoint);
        distances.Add((j, distance));
      }

      distances.Sort((a, b) => a.distance.CompareTo(b.distance));
      int k = Math.Min(_model.Options.NumberOfNeighbors, distances.Count);

      // Compute weighted average using inverse distance weights
      float totalWeight = 0.0f;
      var embedding = new float[_model.EmbeddingDimension];

      for (int j = 0; j < k; j++)
      {
        int neighborIdx = distances[j].index;
        float dist = distances[j].distance;
        float weight = dist > 0 ? 1.0f / (dist + 1e-6f) : 1e6f;

        totalWeight += weight;
        for (int d = 0; d < _model.EmbeddingDimension; d++)
        {
          embedding[d] += weight * _model.Embedding[neighborIdx, d];
        }
      }

      // Normalize
      for (int d = 0; d < _model.EmbeddingDimension; d++)
      {
        transformed[i, d] = embedding[d] / totalWeight;
      }
    }

    return transformed;
  }
}
