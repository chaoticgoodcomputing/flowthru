using System.Numerics;
using System.Runtime.CompilerServices;

namespace Flowthru.Misc.ML.UMAP.Strategies.NeighborSearch.Implementations;

/// <summary>
/// High-performance SIMD-optimized distance calculations for nearest neighbor search.
/// </summary>
/// <remarks>
/// <para>
/// Uses System.Numerics.Vector&lt;T&gt; for SIMD acceleration, automatically taking advantage of:
/// </para>
/// <list type="bullet">
///   <item><description>SSE/SSE2 (128-bit vectors, 4 floats) on x86/x64</description></item>
///   <item><description>AVX/AVX2 (256-bit vectors, 8 floats) on modern x86/x64</description></item>
///   <item><description>NEON (128-bit vectors, 4 floats) on ARM64</description></item>
/// </list>
/// <para>
/// For best performance, ensure data is aligned and input spans have lengths that are multiples
/// of Vector&lt;float&gt;.Count. The scalar remainder path handles unaligned data automatically.
/// </para>
/// <para>
/// Typical speedup: 2-4x vs scalar code, depending on vector width and CPU architecture.
/// </para>
/// </remarks>
internal static class VectorizedDistances
{
  /// <summary>
  /// Number of floats that can be processed in a single SIMD vector operation.
  /// Typical values: 4 (SSE), 8 (AVX), 16 (AVX-512).
  /// </summary>
  private static readonly int VectorSize = Vector<float>.Count;

  /// <summary>
  /// Computes squared Euclidean distance between two vectors using SIMD acceleration.
  /// Returns ||a - b||² = Σᵢ(aᵢ - bᵢ)²
  /// </summary>
  /// <param name="a">First vector.</param>
  /// <param name="b">Second vector (must be same length as a).</param>
  /// <returns>Squared Euclidean distance.</returns>
  /// <remarks>
  /// Use this when you need squared distance (e.g., for comparisons) to avoid sqrt overhead.
  /// </remarks>
  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public static float EuclideanSquared(ReadOnlySpan<float> a, ReadOnlySpan<float> b)
  {
    if (a.Length != b.Length)
    {
      throw new ArgumentException("Vectors must have the same length");
    }

    float sum = 0f;
    int i = 0;

    // SIMD vectorized path - processes VectorSize elements at a time
    int vectorEnd = a.Length - VectorSize;
    for (; i <= vectorEnd; i += VectorSize)
    {
      var va = new Vector<float>(a.Slice(i, VectorSize));
      var vb = new Vector<float>(b.Slice(i, VectorSize));
      var diff = va - vb;
      sum += Vector.Dot(diff, diff);
    }

    // Scalar remainder - handle remaining elements
    for (; i < a.Length; i++)
    {
      float diff = a[i] - b[i];
      sum += diff * diff;
    }

    return sum;
  }

  /// <summary>
  /// Computes Euclidean distance between two vectors using SIMD acceleration.
  /// Returns ||a - b|| = √(Σᵢ(aᵢ - bᵢ)²)
  /// </summary>
  /// <param name="a">First vector.</param>
  /// <param name="b">Second vector (must be same length as a).</param>
  /// <returns>Euclidean distance.</returns>
  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public static float Euclidean(ReadOnlySpan<float> a, ReadOnlySpan<float> b)
  {
    return MathF.Sqrt(EuclideanSquared(a, b));
  }

  /// <summary>
  /// Computes cosine similarity between two vectors using SIMD acceleration.
  /// Returns (a · b) / (||a|| × ||b||)
  /// </summary>
  /// <param name="a">First vector.</param>
  /// <param name="b">Second vector (must be same length as a).</param>
  /// <returns>Cosine similarity in range [-1, 1].</returns>
  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public static float CosineSimilarity(ReadOnlySpan<float> a, ReadOnlySpan<float> b)
  {
    if (a.Length != b.Length)
    {
      throw new ArgumentException("Vectors must have the same length");
    }

    float dotProduct = 0f;
    float normA = 0f;
    float normB = 0f;
    int i = 0;

    // SIMD vectorized path
    int vectorEnd = a.Length - VectorSize;
    for (; i <= vectorEnd; i += VectorSize)
    {
      var va = new Vector<float>(a.Slice(i, VectorSize));
      var vb = new Vector<float>(b.Slice(i, VectorSize));
      dotProduct += Vector.Dot(va, vb);
      normA += Vector.Dot(va, va);
      normB += Vector.Dot(vb, vb);
    }

    // Scalar remainder
    for (; i < a.Length; i++)
    {
      dotProduct += a[i] * b[i];
      normA += a[i] * a[i];
      normB += b[i] * b[i];
    }

    float magnitude = MathF.Sqrt(normA) * MathF.Sqrt(normB);
    return magnitude < 1e-8f ? 0f : dotProduct / magnitude;
  }

  /// <summary>
  /// Computes cosine distance (1 - cosine similarity) using SIMD acceleration.
  /// Returns 1 - (a · b) / (||a|| × ||b||), in range [0, 2].
  /// </summary>
  /// <param name="a">First vector.</param>
  /// <param name="b">Second vector (must be same length as a).</param>
  /// <returns>Cosine distance.</returns>
  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public static float CosineDistance(ReadOnlySpan<float> a, ReadOnlySpan<float> b)
  {
    return 1f - CosineSimilarity(a, b);
  }

  /// <summary>
  /// Computes Manhattan (L1) distance between two vectors using SIMD acceleration.
  /// Returns Σᵢ|aᵢ - bᵢ|
  /// </summary>
  /// <param name="a">First vector.</param>
  /// <param name="b">Second vector (must be same length as a).</param>
  /// <returns>Manhattan distance.</returns>
  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public static float Manhattan(ReadOnlySpan<float> a, ReadOnlySpan<float> b)
  {
    if (a.Length != b.Length)
    {
      throw new ArgumentException("Vectors must have the same length");
    }

    float sum = 0f;
    int i = 0;

    // SIMD vectorized path
    int vectorEnd = a.Length - VectorSize;
    for (; i <= vectorEnd; i += VectorSize)
    {
      var va = new Vector<float>(a.Slice(i, VectorSize));
      var vb = new Vector<float>(b.Slice(i, VectorSize));
      var diff = Vector.Abs(va - vb);
      sum += Vector.Dot(diff, Vector<float>.One);
    }

    // Scalar remainder
    for (; i < a.Length; i++)
    {
      sum += MathF.Abs(a[i] - b[i]);
    }

    return sum;
  }

  /// <summary>
  /// Computes dot product between two vectors using SIMD acceleration.
  /// Returns Σᵢ(aᵢ × bᵢ)
  /// </summary>
  /// <param name="a">First vector.</param>
  /// <param name="b">Second vector (must be same length as a).</param>
  /// <returns>Dot product.</returns>
  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public static float DotProduct(ReadOnlySpan<float> a, ReadOnlySpan<float> b)
  {
    if (a.Length != b.Length)
    {
      throw new ArgumentException("Vectors must have the same length");
    }

    float sum = 0f;
    int i = 0;

    // SIMD vectorized path
    int vectorEnd = a.Length - VectorSize;
    for (; i <= vectorEnd; i += VectorSize)
    {
      var va = new Vector<float>(a.Slice(i, VectorSize));
      var vb = new Vector<float>(b.Slice(i, VectorSize));
      sum += Vector.Dot(va, vb);
    }

    // Scalar remainder
    for (; i < a.Length; i++)
    {
      sum += a[i] * b[i];
    }

    return sum;
  }

  /// <summary>
  /// Computes squared L2 norm of a vector using SIMD acceleration.
  /// Returns ||a||² = Σᵢ(aᵢ²)
  /// </summary>
  /// <param name="a">Input vector.</param>
  /// <returns>Squared L2 norm.</returns>
  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public static float NormSquared(ReadOnlySpan<float> a)
  {
    float sum = 0f;
    int i = 0;

    // SIMD vectorized path
    int vectorEnd = a.Length - VectorSize;
    for (; i <= vectorEnd; i += VectorSize)
    {
      var va = new Vector<float>(a.Slice(i, VectorSize));
      sum += Vector.Dot(va, va);
    }

    // Scalar remainder
    for (; i < a.Length; i++)
    {
      sum += a[i] * a[i];
    }

    return sum;
  }

  /// <summary>
  /// Computes L2 norm of a vector using SIMD acceleration.
  /// Returns ||a|| = √(Σᵢ(aᵢ²))
  /// </summary>
  /// <param name="a">Input vector.</param>
  /// <returns>L2 norm.</returns>
  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public static float Norm(ReadOnlySpan<float> a)
  {
    return MathF.Sqrt(NormSquared(a));
  }

  /// <summary>
  /// Normalizes a vector to unit length in-place using SIMD-computed norm.
  /// </summary>
  /// <param name="vector">Vector to normalize (modified in-place).</param>
  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public static void NormalizeInPlace(Span<float> vector)
  {
    float norm = Norm(vector);
    if (norm < 1e-8f)
    {
      return; // Avoid division by zero
    }

    float invNorm = 1f / norm;
    int i = 0;

    // SIMD vectorized path
    int vectorEnd = vector.Length - VectorSize;
    var invNormVector = new Vector<float>(invNorm);
    for (; i <= vectorEnd; i += VectorSize)
    {
      var v = new Vector<float>(vector.Slice(i, VectorSize));
      v *= invNormVector;
      v.CopyTo(vector.Slice(i, VectorSize));
    }

    // Scalar remainder
    for (; i < vector.Length; i++)
    {
      vector[i] *= invNorm;
    }
  }
}
