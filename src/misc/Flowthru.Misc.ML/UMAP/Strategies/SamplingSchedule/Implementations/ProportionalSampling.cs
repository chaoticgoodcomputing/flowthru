namespace Flowthru.Misc.ML.UMAP.Strategies.SamplingSchedule.Implementations;

/// <summary>
/// Proportional sampling schedule where edges are sampled proportionally to their weights.
/// This is the standard UMAP sampling strategy.
/// </summary>
/// <remarks>
/// <para>
/// This implementation follows the Python UMAP reference implementation exactly.
/// Each edge is sampled with frequency proportional to its membership strength,
/// ensuring that stronger connections in the fuzzy simplicial set receive more
/// optimization attention.
/// </para>
/// <para>
/// <b>Time complexity:</b> O(E) where E is the number of edges
/// </para>
/// <para>
/// <b>Space complexity:</b> O(E) for the output array
/// </para>
/// <para>
/// Python UMAP reference: <c>make_epochs_per_sample()</c> in <c>umap_.py</c> (lines 906-927)
/// </para>
/// </remarks>
public sealed class ProportionalSampling : ISamplingScheduleStrategy
{
    /// <summary>
    /// Computes proportional sampling schedule for edges.
    /// </summary>
    public SamplingScheduleResult ComputeSchedule(float[] edgeWeights, int nEpochs)
    {
        ValidateInputs(edgeWeights, nEpochs);

        var maxWeight = ComputeMaxWeight(edgeWeights);
        var epochsPerSample = ComputeEpochsPerSample(edgeWeights, maxWeight, nEpochs);
        var totalSamples = EstimateTotalSamples(epochsPerSample, nEpochs);

        return new SamplingScheduleResult(epochsPerSample, totalSamples);
    }

    /// <summary>
    /// Validates inputs are in acceptable ranges.
    /// </summary>
    private static void ValidateInputs(float[] edgeWeights, int nEpochs)
    {
        if (edgeWeights == null || edgeWeights.Length == 0)
        {
            throw new ArgumentException(
              "Edge weights array cannot be null or empty",
              nameof(edgeWeights)
            );
        }

        if (nEpochs <= 0)
        {
            throw new ArgumentException(
              $"Number of epochs must be positive, got {nEpochs}",
              nameof(nEpochs)
            );
        }
    }

    /// <summary>
    /// Finds the maximum weight in the edge weight array.
    /// </summary>
    private static float ComputeMaxWeight(float[] edgeWeights)
    {
        var max = 0.0f;
        foreach (var weight in edgeWeights)
        {
            if (weight > max)
            {
                max = weight;
            }
        }
        return max;
    }

    /// <summary>
    /// Computes epochs per sample for each edge using proportional sampling.
    /// </summary>
    /// <remarks>
    /// Python logic:
    /// <code>
    /// result = -1.0 * np.ones(weights.shape[0])
    /// n_samples = n_epochs * (weights / weights.max())
    /// result[n_samples > 0] = float(n_epochs) / n_samples[n_samples > 0]
    /// </code>
    /// </remarks>
    private static float[] ComputeEpochsPerSample(float[] edgeWeights, float maxWeight, int nEpochs)
    {
        var result = new float[edgeWeights.Length];

        for (var i = 0; i < edgeWeights.Length; i++)
        {
            // Expected number of times this edge will be sampled
            var nSamples = nEpochs * (edgeWeights[i] / maxWeight);

            if (nSamples > 0)
            {
                // Invert to get epochs per sample
                result[i] = nEpochs / nSamples;
            }
            else
            {
                // Mark as never sampled
                result[i] = -1.0f;
            }
        }

        return result;
    }

    /// <summary>
    /// Estimates total number of edge samples across all epochs.
    /// </summary>
    private static int EstimateTotalSamples(float[] epochsPerSample, int nEpochs)
    {
        var total = 0;
        foreach (var eps in epochsPerSample)
        {
            if (eps > 0)
            {
                // This edge will be sampled approximately nEpochs / eps times
                total += (int)Math.Ceiling(nEpochs / eps);
            }
        }
        return total;
    }
}
