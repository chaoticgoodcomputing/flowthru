using Flowthru.Core.Abstractions;

namespace DroppedNeuralNet.Data._04_Analysis.Schemas;

/// <summary>
/// A single Block pairing selected by the Hungarian algorithm: the globally optimal
/// assignment of inp pieces to out pieces under the minimum total CoherenceScore objective.
/// </summary>
[FlowthruSchema]
public partial record BlockAssignment
{
    /// <summary>Sequential Block position assigned by the Hungarian solver (0–47). Not the execution order.</summary>
    public int BlockIndex { get; init; }

    public int InpPieceIndex { get; init; }
    public int OutPieceIndex { get; init; }

    /// <summary>Sinkhorn-normalized coherence score for this pairing — the cost the solver minimized.</summary>
    public float CoherenceScore { get; init; }
}
