using Flowthru.Core.Abstractions;

namespace DroppedNeuralNet.Data._07_ModelOutput.Schemas;

/// <summary>
/// The recovered permutation of all 97 layer pieces in the order they are applied.
/// Indices 0–95 are Block pieces (each Block uses two consecutive slots: inp then out).
/// Index 96 is the LastLayer piece.
/// </summary>
[FlowthruSchema]
public partial record PermutationSolution
{
  /// <summary>
  /// 97-element array where Permutation[i] is the piece_index applied at position i.
  /// Positions 0 and 1 are inp/out for Block 0, positions 2 and 3 for Block 1, etc.
  /// Position 96 is the LastLayer.
  /// </summary>
  public int[] Permutation { get; init; } = Array.Empty<int>();
}
