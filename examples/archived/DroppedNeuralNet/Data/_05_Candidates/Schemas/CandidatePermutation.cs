using Flowthru.Core.Abstractions;

namespace DroppedNeuralNet.Data._05_Candidates.Schemas;

/// <summary>
/// A single candidate execution ordering for the 97 network pieces.
/// The permutation is JSON-encoded so it can cross the Arrow boundary as a flat string
/// and be decoded by Python steps without a nested-schema storage adapter.
/// </summary>
[FlowthruSchema]
public partial record CandidatePermutation
{
  public int CandidateIndex { get; init; }

  /// <summary>
  /// JSON-encoded int array of length 97.
  /// Positions 2k and 2k+1 are the inp/out pieces for Block k (k = 0..47).
  /// Position 96 is the LastLayer piece index.
  /// Example: "[12, 45, 3, 67, ...]"
  /// </summary>
  public string Permutation { get; init; } = "[]";
}
