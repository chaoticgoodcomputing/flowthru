namespace Flowthru.Extensions.MLPure.UMAP.Algorithms;

/// <summary>
/// Distance metrics for UMAP - pure Python port.
/// Based on umap/distances.py from the Python implementation.
/// </summary>
public static class Distances
{
  /// <summary>
  /// Standard Euclidean distance (Python: euclidean)
  /// </summary>
  public static float Euclidean(ReadOnlySpan<float> x, ReadOnlySpan<float> y)
  {
    float result = 0.0f;
    for (int i = 0; i < x.Length; i++)
    {
      float diff = x[i] - y[i];
      result += diff * diff;
    }
    return MathF.Sqrt(result);
  }

  /// <summary>
  /// Manhattan/L1 distance (Python: manhattan)
  /// </summary>
  public static float Manhattan(ReadOnlySpan<float> x, ReadOnlySpan<float> y)
  {
    float result = 0.0f;
    for (int i = 0; i < x.Length; i++)
    {
      result += MathF.Abs(x[i] - y[i]);
    }
    return result;
  }

  /// <summary>
  /// Cosine distance: 1 - cosine_similarity
  /// </summary>
  public static float Cosine(ReadOnlySpan<float> x, ReadOnlySpan<float> y)
  {
    float dotProduct = 0.0f;
    float normX = 0.0f;
    float normY = 0.0f;
    
    for (int i = 0; i < x.Length; i++)
    {
      dotProduct += x[i] * y[i];
      normX += x[i] * x[i];
      normY += y[i] * y[i];
    }
    
    if (normX == 0.0f || normY == 0.0f)
    {
      return 1.0f;
    }
    
    float cosineSimilarity = dotProduct / (MathF.Sqrt(normX) * MathF.Sqrt(normY));
    return 1.0f - cosineSimilarity;
  }

  /// <summary>
  /// Correlation distance: 1 - Pearson correlation
  /// </summary>
  public static float Correlation(ReadOnlySpan<float> x, ReadOnlySpan<float> y)
  {
    // Compute means
    float meanX = 0.0f, meanY = 0.0f;
    for (int i = 0; i < x.Length; i++)
    {
      meanX += x[i];
      meanY += y[i];
    }
    meanX /= x.Length;
    meanY /= y.Length;
    
    // Compute correlation
    float numerator = 0.0f;
    float denomX = 0.0f;
    float denomY = 0.0f;
    
    for (int i = 0; i < x.Length; i++)
    {
      float dx = x[i] - meanX;
      float dy = y[i] - meanY;
      numerator += dx * dy;
      denomX += dx * dx;
      denomY += dy * dy;
    }
    
    if (denomX == 0.0f || denomY == 0.0f)
    {
      return 1.0f;
    }
    
    float correlation = numerator / (MathF.Sqrt(denomX) * MathF.Sqrt(denomY));
    return 1.0f - correlation;
  }

  /// <summary>
  /// Get the distance function for a given metric name.
  /// </summary>
  public static Func<ReadOnlySpan<float>, ReadOnlySpan<float>, float> GetMetric(string metric)
  {
    return metric.ToLowerInvariant() switch
    {
      "euclidean" => Euclidean,
      "manhattan" => Manhattan,
      "cosine" => Cosine,
      "correlation" => Correlation,
      _ => throw new ArgumentException($"Unknown metric: {metric}", nameof(metric))
    };
  }
}
