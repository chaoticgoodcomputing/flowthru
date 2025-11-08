namespace Flowthru.Extensions.MLPure.UMAP.Algorithms;

/// <summary>
/// Utility functions for UMAP - pure Python port.
/// Based on umap/utils.py from the Python implementation.
/// </summary>
public static class Utils
{
  /// <summary>
  /// Create a simple random number generator wrapper matching Python's behavior.
  /// </summary>
  public static Random CreateRandom(int? seed)
  {
    return seed.HasValue ? new Random(seed.Value) : new Random();
  }

  /// <summary>
  /// Clip value to range [-4.0, 4.0] (matching Python: clip in layouts.py)
  /// </summary>
  public static float Clip(float val)
  {
    if (val > 4.0f)
    {
      return 4.0f;
    }
    else if (val < -4.0f)
    {
      return -4.0f;
    }
    else
    {
      return val;
    }
  }

  /// <summary>
  /// Reduced Euclidean distance (squared) - from layouts.py:rdist
  /// </summary>
  public static float RDist(ReadOnlySpan<float> x, ReadOnlySpan<float> y)
  {
    float result = 0.0f;
    for (int i = 0; i < x.Length; i++)
    {
      float diff = x[i] - y[i];
      result += diff * diff;
    }
    return result;
  }
}
