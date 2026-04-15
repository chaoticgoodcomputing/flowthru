using Flowthru.Core.Abstractions;

namespace DroppedNeuralNet.Data._03_Primary.Schemas;

/// <summary>
/// A structurally valid Block pairing: one inp piece and one out piece whose dimensions
/// are compatible with the residual connection constraint (inp.in_dim == out.out_dim == 48).
/// </summary>
[FlowthruSchema]
public partial record BlockCandidate
{
    /// <summary>PieceIndex of the Block.inp layer (Linear 48 → 96).</summary>
    public int InpPieceIndex { get; init; }

    /// <summary>PieceIndex of the Block.out layer (Linear 96 → 48).</summary>
    public int OutPieceIndex { get; init; }
}
