using Flowthru.Core.Abstractions;

namespace DroppedNeuralNet.Data._08_Reporting.Schemas;

/// <summary>
/// Forward-pass evaluation result for a single candidate permutation produced by the
/// Solver flow. All candidates are always emitted — no tolerance gating — so the full
/// error distribution is preserved as a catalog entry for downstream analysis.
/// </summary>
[FlowthruSchema]
public partial record CandidateEvaluation
{
  public int CandidateIndex { get; init; }

  /// <summary>Maximum absolute error across all historical samples.</summary>
  public float MaxErr { get; init; }

  /// <summary>Mean absolute error across all historical samples.</summary>
  public float MeanErr { get; init; }

  /// <summary>1 if MaxErr is below the tolerance threshold used during evaluation, 0 otherwise.</summary>
  public int PassesTolerance { get; init; }

  /// <summary>JSON-encoded int[97] permutation (same encoding as CandidatePermutation.Permutation).</summary>
  public string Permutation { get; init; } = "[]";
}
