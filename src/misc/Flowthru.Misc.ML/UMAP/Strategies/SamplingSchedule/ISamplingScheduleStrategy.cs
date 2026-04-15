namespace Flowthru.Misc.ML.UMAP.Strategies.SamplingSchedule;

/// <summary>
/// Strategy interface for computing edge sampling schedules during layout optimization.
/// This is the sixth phase of the UMAP algorithm.
/// </summary>
/// <remarks>
/// <para>
/// The sampling schedule determines how frequently each edge in the fuzzy simplicial set
/// should be sampled during stochastic gradient descent. Edges with higher membership
/// strength (weight) are sampled more frequently.
/// </para>
/// <para>
/// <b>Standard approach (proportional sampling):</b>
/// </para>
/// <para>
/// Each edge is sampled proportionally to its weight. The number of epochs between samples
/// for an edge with weight <c>w</c> is:
/// </para>
/// <code>
/// epochs_per_sample[i] = n_epochs / (n_epochs * weight[i] / max_weight)
///                      = max_weight / weight[i]
/// </code>
/// <para>
/// This ensures that stronger edges (higher membership) are sampled more often, while
/// weaker edges may not be sampled at all if their expected sample count is less than 1.
/// </para>
/// <para>
/// Python UMAP reference: <c>make_epochs_per_sample()</c> function in <c>umap_.py</c> (lines 906-927)
/// </para>
/// </remarks>
public interface ISamplingScheduleStrategy
{
  /// <summary>
  /// Computes the sampling schedule for edges during SGD optimization.
  /// </summary>
  /// <param name="edgeWeights">
  /// Array of edge weights from the fuzzy simplicial set.
  /// These are the membership strengths after fuzzy set operations.
  /// Length: number of edges in the graph
  /// </param>
  /// <param name="nEpochs">
  /// Total number of optimization epochs to run.
  /// Must be positive.
  /// </param>
  /// <returns>
  /// Array of epochs-per-sample for each edge.
  /// Value of <c>epochs_per_sample[i]</c> means edge <c>i</c> should be sampled
  /// every <c>epochs_per_sample[i]</c> epochs (on average).
  /// Edges with weight too small to be sampled are marked with -1.
  /// Length: same as edgeWeights
  /// </returns>
  /// <remarks>
  /// <para>
  /// <b>Implementation requirements:</b>
  /// </para>
  /// <list type="number">
  ///   <item><description>Find maximum weight across all edges</description></item>
  ///   <item><description>Compute expected number of samples per edge: n_epochs * (weight / max_weight)</description></item>
  ///   <item><description>Invert to get epochs per sample: n_epochs / expected_samples</description></item>
  ///   <item><description>Mark edges with expected_samples ≤ 0 as -1 (never sampled)</description></item>
  /// </list>
  /// </remarks>
  SamplingScheduleResult ComputeSchedule(float[] edgeWeights, int nEpochs);
}

/// <summary>
/// Result of sampling schedule computation.
/// </summary>
/// <param name="EpochsPerSample">
/// Number of epochs between samples for each edge.
/// Array length matches the number of edges in the graph.
/// Value of -1 indicates the edge should never be sampled.
/// </param>
/// <param name="TotalExpectedSamples">
/// Total number of edge samples expected across all epochs.
/// Useful for progress estimation.
/// </param>
public sealed record SamplingScheduleResult(float[] EpochsPerSample, int TotalExpectedSamples);
