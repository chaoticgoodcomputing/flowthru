using Flowthru.Core.Abstractions;

namespace DroppedNeuralNet.Data._02_Intermediate.Schemas;

/// <summary>
/// Layer variant — determines which position in a Block a piece can occupy.
/// </summary>
public enum LayerType
{
  /// <summary>Block.inp: Linear(48 → 96). Expands input into hidden dimension.</summary>
  [SerializedEnum("BlockInp")]
  BlockInp,

  /// <summary>Block.out: Linear(96 → 48). Projects back to residual dimension.</summary>
  [SerializedEnum("BlockOut")]
  BlockOut,

  /// <summary>LastLayer: Linear(48 → 1). Final regression head.</summary>
  [SerializedEnum("Last")]
  Last,
}

/// <summary>
/// Structural metadata for a single layer piece, derived from its tensor shapes.
/// Contains no weight data — C# steps only ever see dimensions and layer type.
/// Steps that need to reconstruct torch modules join this against <c>Pieces</c> by PieceIndex.
/// </summary>
[FlowthruSchema]
public partial record PieceMetadata
{
  public int PieceIndex { get; init; }

  /// <summary>Number of input features for this linear layer (weight matrix columns).</summary>
  public int InputDim { get; init; }

  /// <summary>Number of output features for this linear layer (weight matrix rows).</summary>
  public int OutputDim { get; init; }

  /// <summary>Structural role this piece can play within the reassembled network.</summary>
  public LayerType LayerType { get; init; }
}
