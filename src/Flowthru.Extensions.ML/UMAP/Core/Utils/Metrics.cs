namespace Flowthru.Extensions.ML.UMAP.Core.Utils;

/// <summary>
/// Common metric functions for UMAP.
/// Provides standard distance metrics as static methods.
/// </summary>
public static class Metrics
{
  /// <summary>
  /// Euclidean distance metric (L2 norm).
  /// Computes the straight-line distance between two points in Euclidean space.
  /// </summary>
  /// <param name="x">First vector</param>
  /// <param name="y">Second vector</param>
  /// <returns>Euclidean distance between x and y</returns>
  public static float Euclidean(ReadOnlySpan<float> x, ReadOnlySpan<float> y)
  {
    float sum = 0f;
    for (int i = 0; i < x.Length; i++)
    {
      float diff = x[i] - y[i];
      sum += diff * diff;
    }
    return MathF.Sqrt(sum);
  }

  /// <summary>
  /// Manhattan distance metric (L1 norm).
  /// Computes the sum of absolute differences between coordinates.
  /// </summary>
  /// <param name="x">First vector</param>
  /// <param name="y">Second vector</param>
  /// <returns>Manhattan distance between x and y</returns>
  public static float Manhattan(ReadOnlySpan<float> x, ReadOnlySpan<float> y)
  {
    float sum = 0f;
    for (int i = 0; i < x.Length; i++)
    {
      sum += MathF.Abs(x[i] - y[i]);
    }
    return sum;
  }

  /// <summary>
  /// Cosine distance metric (1 - cosine similarity).
  /// Measures the angle between two vectors, ignoring magnitude.
  /// </summary>
  /// <param name="x">First vector</param>
  /// <param name="y">Second vector</param>
  /// <returns>Cosine distance between x and y (0 = identical direction, 2 = opposite)</returns>
  public static float Cosine(ReadOnlySpan<float> x, ReadOnlySpan<float> y)
  {
    float dotProduct = 0f;
    float normX = 0f;
    float normY = 0f;

    for (int i = 0; i < x.Length; i++)
    {
      dotProduct += x[i] * y[i];
      normX += x[i] * x[i];
      normY += y[i] * y[i];
    }

    float denominator = MathF.Sqrt(normX * normY);
    if (denominator < 1e-8f)
    {
      return 0f;
    }

    float cosineSimilarity = dotProduct / denominator;
    return 1f - cosineSimilarity;
  }
}
