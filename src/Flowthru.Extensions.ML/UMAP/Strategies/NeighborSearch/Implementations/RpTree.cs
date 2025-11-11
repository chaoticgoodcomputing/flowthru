using MathNet.Numerics.LinearAlgebra;

namespace Flowthru.Extensions.ML.UMAP.Strategies.NeighborSearch.Implementations;

/// <summary>
/// Random Projection Tree for efficient approximate nearest neighbor initialization.
/// </summary>
/// <remarks>
/// <para>
/// RP-trees partition the data space recursively using random hyperplanes. Points close
/// in the original space tend to fall into the same leaf, providing high-quality initial
/// neighbor candidates for NN-descent.
/// </para>
/// <para>
/// This implementation uses angular random projection: hyperplanes are defined by the
/// difference between two randomly selected points, creating an angular split appropriate
/// for cosine/euclidean metrics.
/// </para>
/// <para>
/// Python reference: <c>rp_trees.py</c> - specifically <c>angular_random_projection_split()</c>
/// and <c>make_forest()</c> functions.
/// </para>
/// </remarks>
internal sealed class RpTree
{
  /// <summary>
  /// Hyperplane normal vectors for each internal node. Shape: [num_internal_nodes, dimensions].
  /// Each row is the normalized direction vector defining a splitting hyperplane.
  /// </summary>
  public float[][] Hyperplanes { get; }

  /// <summary>
  /// Hyperplane offsets for each internal node. Shape: [num_internal_nodes].
  /// For angular splits, offsets are typically 0.
  /// </summary>
  public float[] Offsets { get; }

  /// <summary>
  /// Children indices for each internal node. Shape: [num_internal_nodes].
  /// Children[i] = (left_child_index, right_child_index).
  /// Negative values indicate leaf nodes: -child_index points to leaf data.
  /// </summary>
  public (int Left, int Right)[] Children { get; }

  /// <summary>
  /// Leaf node data: indices of points in each leaf. Shape: [num_leaves][variable].
  /// LeafIndices[i] contains the indices of all points that fell into leaf i.
  /// </summary>
  public int[][] LeafIndices { get; }

  public RpTree(float[][] hyperplanes, float[] offsets, (int, int)[] children, int[][] leafIndices)
  {
    Hyperplanes = hyperplanes;
    Offsets = offsets;
    Children = children;
    LeafIndices = leafIndices;
  }

  /// <summary>
  /// Extracts all leaf arrays as a flat 2D array for heap initialization.
  /// Returns array shape: [num_leaves, max_leaf_size] with -1 for missing entries.
  /// </summary>
  public int[][] GetLeafArray()
  {
    return LeafIndices;
  }
}

/// <summary>
/// Builder for constructing random projection trees.
/// </summary>
internal static class RpTreeBuilder
{
  /// <summary>
  /// Builds a forest of random projection trees.
  /// </summary>
  /// <param name="data">Input data matrix [n_samples, n_features].</param>
  /// <param name="numTrees">Number of trees to build.</param>
  /// <param name="leafSize">Maximum points per leaf before stopping recursion.</param>
  /// <param name="random">Random number generator.</param>
  /// <returns>Array of RP-trees.</returns>
  public static RpTree[] BuildForest(Matrix<float> data, int numTrees, int leafSize, Random random)
  {
    var forest = new RpTree[numTrees];

    for (int t = 0; t < numTrees; t++)
    {
      forest[t] = BuildTree(data, leafSize, random);
    }

    return forest;
  }

  /// <summary>
  /// Builds a single random projection tree using angular splits.
  /// </summary>
  private static RpTree BuildTree(Matrix<float> data, int leafSize, Random random)
  {
    int nSamples = data.RowCount;
    int nFeatures = data.ColumnCount;

    // Initialize with all point indices
    int[] allIndices = Enumerable.Range(0, nSamples).ToArray();

    var hyperplanes = new List<float[]>();
    var offsets = new List<float>();
    var children = new List<(int, int)>();
    var leafIndices = new List<int[]>();

    // Recursively build tree
    BuildNode(data, allIndices, leafSize, random, hyperplanes, offsets, children, leafIndices);

    return new RpTree(
      hyperplanes.ToArray(),
      offsets.ToArray(),
      children.ToArray(),
      leafIndices.ToArray()
    );
  }

  /// <summary>
  /// Recursively builds a tree node using angular random projection split.
  /// </summary>
  /// <returns>Node index (positive for internal nodes, negative for leaves).</returns>
  private static int BuildNode(
    Matrix<float> data,
    int[] indices,
    int leafSize,
    Random random,
    List<float[]> hyperplanes,
    List<float> offsets,
    List<(int, int)> children,
    List<int[]> leafIndices
  )
  {
    // Base case: create leaf node if small enough
    if (indices.Length <= leafSize)
    {
      int leafIndex = leafIndices.Count;
      leafIndices.Add(indices);
      return -(leafIndex + 1); // Negative indicates leaf
    }

    // Angular random projection split
    var (leftIndices, rightIndices, hyperplane, offset) = AngularRandomProjectionSplit(
      data,
      indices,
      random
    );

    // Handle degenerate split (all points on one side)
    if (leftIndices.Length == 0 || rightIndices.Length == 0)
    {
      // Force a random split
      int mid = indices.Length / 2;
      leftIndices = indices[..mid];
      rightIndices = indices[mid..];

      // Use a random hyperplane
      hyperplane = new float[data.ColumnCount];
      for (int i = 0; i < hyperplane.Length; i++)
      {
        hyperplane[i] = (float)(random.NextDouble() * 2 - 1);
      }
      Normalize(hyperplane);
      offset = 0.0f;
    }

    // Create internal node
    int nodeIndex = hyperplanes.Count;
    hyperplanes.Add(hyperplane);
    offsets.Add(offset);
    children.Add((0, 0)); // Placeholder, will update

    // Recursively build children
    int leftChild = BuildNode(
      data,
      leftIndices,
      leafSize,
      random,
      hyperplanes,
      offsets,
      children,
      leafIndices
    );
    int rightChild = BuildNode(
      data,
      rightIndices,
      leafSize,
      random,
      hyperplanes,
      offsets,
      children,
      leafIndices
    );

    // Update children pointers
    children[nodeIndex] = (leftChild, rightChild);

    return nodeIndex;
  }

  /// <summary>
  /// Performs angular random projection split.
  /// Selects two random points and splits based on which side of their midpoint hyperplane.
  /// </summary>
  /// <returns>Tuple of (left_indices, right_indices, hyperplane, offset).</returns>
  private static (
    int[] Left,
    int[] Right,
    float[] Hyperplane,
    float Offset
  ) AngularRandomProjectionSplit(Matrix<float> data, int[] indices, Random random)
  {
    int nFeatures = data.ColumnCount;

    // Select two random points
    int leftIdx = random.Next(indices.Length);
    int rightIdx = random.Next(indices.Length);
    if (leftIdx == rightIdx)
    {
      rightIdx = (rightIdx + 1) % indices.Length;
    }

    int leftPoint = indices[leftIdx];
    int rightPoint = indices[rightIdx];

    var leftData = data.Row(leftPoint).ToArray();
    var rightData = data.Row(rightPoint).ToArray();

    // Normalize the points (for angular split)
    float leftNorm = Norm(leftData);
    float rightNorm = Norm(rightData);

    if (leftNorm < 1e-8f)
      leftNorm = 1.0f;
    if (rightNorm < 1e-8f)
      rightNorm = 1.0f;

    // Hyperplane is the difference between normalized points
    var hyperplane = new float[nFeatures];
    for (int d = 0; d < nFeatures; d++)
    {
      hyperplane[d] = (leftData[d] / leftNorm) - (rightData[d] / rightNorm);
    }

    // Normalize hyperplane
    Normalize(hyperplane);

    // Split points based on which side of hyperplane
    var leftList = new List<int>();
    var rightList = new List<int>();

    foreach (int idx in indices)
    {
      float margin = DotProduct(data.Row(idx).ToArray(), hyperplane);

      if (Math.Abs(margin) < 1e-8f)
      {
        // On the hyperplane - randomly assign
        if (random.Next(2) == 0)
        {
          leftList.Add(idx);
        }
        else
        {
          rightList.Add(idx);
        }
      }
      else if (margin > 0)
      {
        leftList.Add(idx);
      }
      else
      {
        rightList.Add(idx);
      }
    }

    return (leftList.ToArray(), rightList.ToArray(), hyperplane, 0.0f);
  }

  /// <summary>
  /// Computes the L2 norm of a vector.
  /// </summary>
  private static float Norm(float[] vector)
  {
    float sum = 0.0f;
    for (int i = 0; i < vector.Length; i++)
    {
      sum += vector[i] * vector[i];
    }
    return MathF.Sqrt(sum);
  }

  /// <summary>
  /// Normalizes a vector to unit length in-place.
  /// </summary>
  private static void Normalize(float[] vector)
  {
    float norm = Norm(vector);
    if (norm < 1e-8f)
      norm = 1.0f;

    for (int i = 0; i < vector.Length; i++)
    {
      vector[i] /= norm;
    }
  }

  /// <summary>
  /// Computes dot product between two vectors.
  /// </summary>
  private static float DotProduct(float[] a, float[] b)
  {
    float sum = 0.0f;
    for (int i = 0; i < a.Length; i++)
    {
      sum += a[i] * b[i];
    }
    return sum;
  }
}
