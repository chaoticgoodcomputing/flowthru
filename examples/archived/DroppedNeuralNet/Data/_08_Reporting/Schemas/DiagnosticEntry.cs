using Flowthru.Core.Abstractions;

namespace DroppedNeuralNet.Data._08_Reporting.Schemas;

/// <summary>
/// A single diagnostic measurement emitted by the Validation flow.
///
/// Rows are grouped by <see cref="Category"/> (e.g. "FixedOrdering", "Candidate_3",
/// "PairingSignal") and keyed by <see cref="Metric"/> within each group. This flat
/// schema lets a single step emit heterogeneous signal types without requiring a
/// separate schema per check.
/// </summary>
[FlowthruSchema]
public partial record DiagnosticEntry
{
    /// <summary>Logical grouping, e.g. "FixedOrdering", "Candidate_7", "PairingSignal".</summary>
    public string Category { get; init; } = "";

    /// <summary>Name of the measured quantity, e.g. "MaxErr", "MeanErr", "ScoreStd".</summary>
    public string Metric { get; init; } = "";

    /// <summary>Numeric value of the measurement.</summary>
    public float Value { get; init; }

    /// <summary>Optional human-readable context emitted alongside the value.</summary>
    public string Notes { get; init; } = "";
}
