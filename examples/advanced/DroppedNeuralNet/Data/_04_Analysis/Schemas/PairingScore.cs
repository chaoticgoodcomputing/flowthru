using Flowthru.Core.Abstractions;

namespace DroppedNeuralNet.Data._04_Analysis.Schemas;

/// <summary>
/// The Frobenius norm of the product ||out.W @ inp.W||_F for a candidate Block pairing.
/// Lower values indicate that the out layer is a better inverse of the inp layer,
/// which is the structural signal left in the weights by the residual training objective.
/// </summary>
[FlowthruSchema]
public partial record PairingScore
{
  public int InpPieceIndex { get; init; }
  public int OutPieceIndex { get; init; }

  /// <summary>||W_out @ W_inp||_F. Lower = stronger residual coupling between these two layers.</summary>
  public float ProductNorm { get; init; }
}
