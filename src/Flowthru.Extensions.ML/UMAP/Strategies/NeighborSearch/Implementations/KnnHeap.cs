namespace Flowthru.Extensions.ML.UMAP.Strategies.NeighborSearch.Implementations;

/// <summary>
/// Heap-based priority queue for efficiently maintaining k-nearest neighbors.
/// Uses max-heap property where the largest (worst) distance is at the root.
/// </summary>
/// <remarks>
/// <para>
/// This data structure is the core of NN-Descent. It maintains the k-nearest neighbors
/// for each point using a max-heap, which allows O(log k) updates when better neighbors
/// are discovered.
/// </para>
/// <para>
/// The heap also tracks "flags" indicating whether each neighbor is "new" (recently added,
/// flag=1) or "old" (previously explored, flag=0). This enables efficient candidate generation
/// by distinguishing between neighbors that need exploration vs those already processed.
/// </para>
/// <para>
/// Python reference: Heap operations in <c>utils.py</c> including <c>make_heap()</c>,
/// <c>checked_flagged_heap_push()</c>, and <c>deheap_sort()</c>.
/// </para>
/// </remarks>
internal sealed class KnnHeap
{
  /// <summary>
  /// Neighbor indices for each point. Shape: [n_samples, k].
  /// Indices[i][j] is the j-th nearest neighbor index of point i.
  /// Organized as max-heap: Indices[i][0] is the k-th nearest (worst) neighbor.
  /// </summary>
  public int[][] Indices { get; }

  /// <summary>
  /// Distances to neighbors for each point. Shape: [n_samples, k].
  /// Distances[i][j] is the distance to the j-th nearest neighbor of point i.
  /// Organized as max-heap: Distances[i][0] is the largest distance (to k-th neighbor).
  /// </summary>
  public float[][] Distances { get; }

  /// <summary>
  /// Flags indicating new vs old neighbors. Shape: [n_samples, k].
  /// Flags[i][j] = 1 indicates a "new" neighbor (recently added).
  /// Flags[i][j] = 0 indicates an "old" neighbor (previously explored).
  /// </summary>
  public byte[][] Flags { get; }

  /// <summary>
  /// Hash sets for fast O(1) duplicate checking during neighbor insertion.
  /// Maintains the set of neighbor indices currently in each point's heap.
  /// </summary>
  private readonly HashSet<int>[] _neighborSets;

  /// <summary>
  /// Initializes a new k-NN heap for tracking nearest neighbors.
  /// </summary>
  /// <param name="nSamples">Number of data points.</param>
  /// <param name="k">Number of nearest neighbors to maintain per point.</param>
  public KnnHeap(int nSamples, int k)
  {
    Indices = new int[nSamples][];
    Distances = new float[nSamples][];
    Flags = new byte[nSamples][];
    _neighborSets = new HashSet<int>[nSamples];

    for (int i = 0; i < nSamples; i++)
    {
      Indices[i] = new int[k];
      Distances[i] = new float[k];
      Flags[i] = new byte[k];
      _neighborSets[i] = new HashSet<int>(k + 10); // Extra capacity for efficiency

      // Initialize with invalid values
      Array.Fill(Indices[i], -1);
      Array.Fill(Distances[i], float.PositiveInfinity);
      Array.Fill(Flags[i], (byte)0);
    }
  }

  /// <summary>
  /// Attempts to push a new neighbor into the heap if it improves the current k-NN.
  /// Returns true if the neighbor was added, false otherwise.
  /// </summary>
  /// <param name="sample">Index of the sample point.</param>
  /// <param name="neighbor">Index of the potential neighbor.</param>
  /// <param name="distance">Distance between sample and neighbor.</param>
  /// <param name="flag">Flag value (1=new, 0=old).</param>
  /// <returns>True if neighbor was added to heap, false if rejected.</returns>
  /// <remarks>
  /// This implements the "checked_flagged_heap_push" operation from PyNNDescent.
  /// It rejects if:
  /// 1. Distance is worse than current k-th neighbor
  /// 2. Neighbor already exists in the heap (prevents duplicates)
  ///
  /// Python reference: <c>checked_flagged_heap_push()</c> in <c>utils.py</c>.
  /// </remarks>
  public bool TryPush(int sample, int neighbor, float distance, byte flag)
  {
    // Fast O(1) duplicate check using hash set
    if (!_neighborSets[sample].Add(neighbor))
    {
      return false;
    }

    // Early exit if distance is worse than current k-th neighbor
    if (distance >= Distances[sample][0])
    {
      _neighborSets[sample].Remove(neighbor);
      return false;
    }

    // Track what we're evicting from the heap
    int evicted = Indices[sample][0];
    if (evicted >= 0)
    {
      _neighborSets[sample].Remove(evicted);
    }

    // Insert at root (position 0) and sift down
    Indices[sample][0] = neighbor;
    Distances[sample][0] = distance;
    Flags[sample][0] = flag;

    SiftDown(sample, 0);

    return true;
  }

  /// <summary>
  /// Sifts down an element to restore max-heap property.
  /// </summary>
  /// <param name="sample">Index of the sample point.</param>
  /// <param name="index">Index within the heap to start sifting from.</param>
  private void SiftDown(int sample, int index)
  {
    int k = Indices[sample].Length;
    float distance = Distances[sample][index];
    int neighbor = Indices[sample][index];
    byte flag = Flags[sample][index];

    while (true)
    {
      int leftChild = 2 * index + 1;
      int rightChild = leftChild + 1;
      int largest = index;

      // Find largest among node and its children
      if (leftChild < k && Distances[sample][leftChild] > Distances[sample][largest])
      {
        largest = leftChild;
      }

      if (rightChild < k && Distances[sample][rightChild] > Distances[sample][largest])
      {
        largest = rightChild;
      }

      // If heap property is satisfied, stop
      if (largest == index)
      {
        break;
      }

      // Swap with larger child
      Indices[sample][index] = Indices[sample][largest];
      Distances[sample][index] = Distances[sample][largest];
      Flags[sample][index] = Flags[sample][largest];

      index = largest;
    }

    // Place original element at final position
    Indices[sample][index] = neighbor;
    Distances[sample][index] = distance;
    Flags[sample][index] = flag;
  }

  /// <summary>
  /// Clears all flags, setting them to 0 (marks all neighbors as "old").
  /// Called at the end of each NN-descent iteration.
  /// </summary>
  public void ClearFlags()
  {
    for (int i = 0; i < Flags.Length; i++)
    {
      Array.Fill(Flags[i], (byte)0);
    }
  }

  /// <summary>
  /// Converts the heap to sorted arrays (ascending by distance).
  /// This is the final step of NN-descent, producing the output k-NN graph.
  /// </summary>
  /// <returns>Tuple of (indices, distances) sorted by ascending distance.</returns>
  /// <remarks>
  /// Implements the "deheap sort" operation - the second half of heap sort.
  /// Repeatedly extracts the root (max element) and swaps to end, then restores heap.
  ///
  /// Python reference: <c>deheap_sort()</c> in <c>utils.py</c>.
  /// </remarks>
  public (int[][], float[][]) DeheapSort()
  {
    int nSamples = Indices.Length;
    int k = Indices[0].Length;

    var sortedIndices = new int[nSamples][];
    var sortedDistances = new float[nSamples][];

    for (int i = 0; i < nSamples; i++)
    {
      sortedIndices[i] = (int[])Indices[i].Clone();
      sortedDistances[i] = (float[])Distances[i].Clone();

      // Heap sort: repeatedly extract max and place at end
      for (int j = k - 1; j > 0; j--)
      {
        // Swap root (max) with last element
        (sortedIndices[i][0], sortedIndices[i][j]) = (sortedIndices[i][j], sortedIndices[i][0]);
        (sortedDistances[i][0], sortedDistances[i][j]) = (
          sortedDistances[i][j],
          sortedDistances[i][0]
        );

        // Restore heap property for reduced heap [0..j)
        SiftDownRange(sortedIndices[i], sortedDistances[i], 0, j);
      }
    }

    return (sortedIndices, sortedDistances);
  }

  /// <summary>
  /// Sifts down an element in a specific range [0, heapSize).
  /// Used during deheap sort to maintain heap property for progressively smaller heaps.
  /// </summary>
  private void SiftDownRange(int[] indices, float[] distances, int index, int heapSize)
  {
    float distance = distances[index];
    int neighbor = indices[index];

    while (true)
    {
      int leftChild = 2 * index + 1;
      int rightChild = leftChild + 1;
      int largest = index;

      if (leftChild < heapSize && distances[leftChild] > distances[largest])
      {
        largest = leftChild;
      }

      if (rightChild < heapSize && distances[rightChild] > distances[largest])
      {
        largest = rightChild;
      }

      if (largest == index)
      {
        break;
      }

      distances[index] = distances[largest];
      indices[index] = indices[largest];

      index = largest;
    }

    distances[index] = distance;
    indices[index] = neighbor;
  }
}
