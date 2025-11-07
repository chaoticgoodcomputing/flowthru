using System.Numerics;

namespace Flowthru.Extensions.ML.UMAP.Algorithms;

/// <summary>
/// Distance metric implementations for UMAP.
/// </summary>
/// <remarks>
/// Based on the UMAP Python implementation by Leland McInnes.
/// Reference: https://github.com/lmcinnes/umap
/// </remarks>
public static class DistanceMetrics
{
  /// <summary>
  /// Computes the Euclidean distance between two vectors using SIMD optimization.
  /// </summary>
  public static float Euclidean(ReadOnlySpan<float> x, ReadOnlySpan<float> y)
  {
    if (x.Length != y.Length)
      throw new ArgumentException("Vectors must have the same length");

    return MathF.Sqrt(EuclideanSquared(x, y));
  }

  /// <summary>
  /// Computes the squared Euclidean distance between two vectors using SIMD optimization.
  /// </summary>
  /// <remarks>
  /// Uses System.Numerics.Vector for SIMD acceleration when available.
  /// Falls back to scalar operations for remaining elements.
  /// </remarks>
  public static float EuclideanSquared(ReadOnlySpan<float> x, ReadOnlySpan<float> y)
  {
    if (x.Length != y.Length)
      throw new ArgumentException("Vectors must have the same length");

    float sum = 0f;
    int i = 0;
    int length = x.Length;

    // SIMD path: Process Vector<float>.Count elements at a time
    if (Vector.IsHardwareAccelerated && length >= Vector<float>.Count)
    {
      int vectorSize = Vector<float>.Count;
      int vectorLength = length - (length % vectorSize);

      for (; i < vectorLength; i += vectorSize)
      {
        var vx = new Vector<float>(x.Slice(i, vectorSize));
        var vy = new Vector<float>(y.Slice(i, vectorSize));
        var diff = vx - vy;
        sum += Vector.Dot(diff, diff);
      }
    }

    // Scalar path: Handle remaining elements
    for (; i < length; i++)
    {
      float diff = x[i] - y[i];
      sum += diff * diff;
    }

    return sum;
  }

  /// <summary>
  /// Computes the dot product between two vectors using SIMD optimization.
  /// </summary>
  /// <remarks>
  /// Uses System.Numerics.Vector for SIMD acceleration when available.
  /// Used in optimized Euclidean k-NN computation following ML.NET K-Means approach.
  /// </remarks>
  public static float DotProduct(ReadOnlySpan<float> x, ReadOnlySpan<float> y)
  {
    if (x.Length != y.Length)
      throw new ArgumentException("Vectors must have the same length");

    float sum = 0f;
    int i = 0;
    int length = x.Length;

    // SIMD path: Process Vector<float>.Count elements at a time
    if (Vector.IsHardwareAccelerated && length >= Vector<float>.Count)
    {
      int vectorSize = Vector<float>.Count;
      int vectorLength = length - (length % vectorSize);

      for (; i < vectorLength; i += vectorSize)
      {
        var vx = new Vector<float>(x.Slice(i, vectorSize));
        var vy = new Vector<float>(y.Slice(i, vectorSize));
        sum += Vector.Dot(vx, vy);
      }
    }

    // Scalar path: Handle remaining elements
    for (; i < length; i++)
    {
      sum += x[i] * y[i];
    }

    return sum;
  }

  /// <summary>
  /// Computes the cosine distance between two vectors (1 - cosine similarity).
  /// </summary>
  public static float Cosine(ReadOnlySpan<float> x, ReadOnlySpan<float> y)
  {
    if (x.Length != y.Length)
      throw new ArgumentException("Vectors must have the same length");

    float dotProduct = 0f;
    float normX = 0f;
    float normY = 0f;

    for (int i = 0; i < x.Length; i++)
    {
      dotProduct += x[i] * y[i];
      normX += x[i] * x[i];
      normY += y[i] * y[i];
    }

    if (normX == 0f || normY == 0f)
      return 1f; // Maximum distance when one vector is zero

    return 1f - (dotProduct / (MathF.Sqrt(normX) * MathF.Sqrt(normY)));
  }

  /// <summary>
  /// Computes the correlation distance between two vectors.
  /// </summary>
  public static float Correlation(ReadOnlySpan<float> x, ReadOnlySpan<float> y)
  {
    if (x.Length != y.Length)
      throw new ArgumentException("Vectors must have the same length");

    // Compute means
    float meanX = 0f;
    float meanY = 0f;
    for (int i = 0; i < x.Length; i++)
    {
      meanX += x[i];
      meanY += y[i];
    }
    meanX /= x.Length;
    meanY /= y.Length;

    // Compute correlation
    float numerator = 0f;
    float denomX = 0f;
    float denomY = 0f;

    for (int i = 0; i < x.Length; i++)
    {
      float dx = x[i] - meanX;
      float dy = y[i] - meanY;
      numerator += dx * dy;
      denomX += dx * dx;
      denomY += dy * dy;
    }

    if (denomX == 0f || denomY == 0f)
      return 1f;

    return 1f - (numerator / (MathF.Sqrt(denomX) * MathF.Sqrt(denomY)));
  }

  /// <summary>
  /// Computes the Manhattan (L1) distance between two vectors.
  /// </summary>
  public static float Manhattan(ReadOnlySpan<float> x, ReadOnlySpan<float> y)
  {
    if (x.Length != y.Length)
      throw new ArgumentException("Vectors must have the same length");

    float sum = 0f;
    for (int i = 0; i < x.Length; i++)
    {
      sum += MathF.Abs(x[i] - y[i]);
    }
    return sum;
  }

  /// <summary>
  /// Gets a distance function by name.
  /// </summary>
  public static Func<ReadOnlySpan<float>, ReadOnlySpan<float>, float> GetMetric(string metricName)
  {
    return metricName.ToLowerInvariant() switch
    {
      "euclidean" => Euclidean,
      "cosine" => Cosine,
      "correlation" => Correlation,
      "manhattan" => Manhattan,
      _ => throw new ArgumentException($"Unknown metric: {metricName}"),
    };
  }
}
