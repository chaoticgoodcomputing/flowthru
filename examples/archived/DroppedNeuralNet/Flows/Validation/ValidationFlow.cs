using DroppedNeuralNet.Data;
using DroppedNeuralNet.Data._01_Raw.Schemas;
using DroppedNeuralNet.Data._04_Analysis.Schemas;
using DroppedNeuralNet.Data._05_Candidates.Schemas;
using DroppedNeuralNet.Data._08_Reporting.Schemas;
using Flowthru.Core.Flows;
using Flowthru.Extensions.Python.Execution;
using Flowthru.Extensions.Python.Steps;

namespace DroppedNeuralNet.Flows.Validation;

/// <summary>
/// Diagnostic pipeline run after Exploration to distinguish wrong pairings from wrong orderings.
///
/// Step 1 (Python) — diagnose_pairings:
///   Runs three independent probes against the Hungarian block assignments:
///   <list type="bullet">
///     <item>
///       <strong>FixedOrdering</strong> — assembles the network in arbitrary BlockIndex order.
///       If max_err is still ~72, the (inp, out) pairings themselves are wrong and the
///       Frobenius signal has no discriminating power. If max_err is small (&lt;1) the
///       pairings are correct and only the beam search range is the issue.
///     </item>
///     <item>
///       <strong>PairingSignal</strong> — reports the mean, std, and range of ProductNorm
///       scores. A near-zero std means the cost matrix is flat and Hungarian is guessing.
///     </item>
///     <item>
///       <strong>Candidate_N</strong> — re-runs each ranked candidate permutation and
///       records its max_err individually so ordering quality is visible.
///     </item>
///   </list>
/// All diagnostics are emitted as (Category, Metric, Value, Notes) rows and persisted
/// to <c>_08_Reporting/Datasets/diagnostics.json</c>.
/// </summary>
public static class ValidationFlow
{
  public static Flow Create(Catalog catalog, IPythonExecutor executor)
  {
    return FlowBuilder.CreateFlow(pipeline =>
    {
      pipeline.AddPythonStep<
        IEnumerable<BlockAssignment>,
        IEnumerable<PieceBlob>,
        IEnumerable<MeasurementSchema>,
        IEnumerable<CandidatePermutation>,
        IEnumerable<DiagnosticEntry>
      >(
        label: "DiagnosePairings",
        description: "Probe pairing quality: fixed-order baseline, ProductNorm signal stats, per-candidate errors (Python).",
        module: "Flows.Validation.Steps.diagnose_pairings",
        function: "diagnose_pairings",
        input: (
          catalog.BlockAssignments,
          catalog.Pieces,
          catalog.HistoricalData,
          catalog.CandidatePermutations
        ),
        output: catalog.Diagnostics,
        executor: executor
      );
    });
  }
}
