using DroppedNeuralNet.Data;
using DroppedNeuralNet.Data._01_Raw.Schemas;
using DroppedNeuralNet.Data._02_Intermediate.Schemas;
using DroppedNeuralNet.Data._03_Primary.Schemas;
using DroppedNeuralNet.Data._04_Analysis.Schemas;
using DroppedNeuralNet.Data._05_Candidates.Schemas;
using DroppedNeuralNet.Flows.Exploration.Steps;
using Flowthru.Core.Flows;
using Flowthru.Extensions.Python.Execution;
using Flowthru.Extensions.Python.Steps;

namespace DroppedNeuralNet.Flows.Exploration;

/// <summary>
/// Analytical pipeline that narrows the search space before the Solver validates permutations.
///
/// Step 1 (C#)    — FindLegalPairings:
///   Sieves PieceMetadata to enumerate every structurally valid (inp, out) Block candidate.
///   Pure dimension arithmetic; no blobs.
///
/// Step 2 (Python) — compute_pairing_scores:
///   For every legal (inp, out) candidate, computes ||W_out @ W_inp||_F.
///   Lower scores indicate residual coupling — the signal left by training.
///
/// Step 3 (Python) — run_hungarian:
///   Applies the Hungarian algorithm to the 48×48 score matrix.
///   Produces the globally optimal assignment of inp ↔ out pieces in O(n³).
///
/// Step 4 (Python) — rank_orderings:
///   Uses activation chaining on historical data to score Block execution orderings.
///   Emits a small ranked set of candidate permutations for the Solver to validate.
/// </summary>
public static class ExplorationFlow
{
  public static Flow Create(Catalog catalog, IPythonExecutor executor)
  {
    return FlowBuilder.CreateFlow(pipeline =>
    {
      pipeline.AddStep(
        label: "FindLegalPairings",
        description: "Sieve PieceMetadata to all dimension-valid (inp, out) Block candidates (C#).",
        transform: FindLegalPairingsStep.Create(),
        input: catalog.PieceMetadata,
        output: catalog.LegalPairings
      );

      pipeline.AddPythonStep<
        IEnumerable<PieceMetadata>,
        IEnumerable<PieceBlob>,
        IEnumerable<BlockCandidate>,
        IEnumerable<PairingScore>
      >(
        label: "ComputePairingScores",
        description: "Compute ||W_out @ W_inp||_F for every legal pairing. Lower = stronger residual coupling (Python).",
        module: "Flows.Exploration.Steps.compute_pairing_scores",
        function: "compute_pairing_scores",
        input: (catalog.PieceMetadata, catalog.Pieces, catalog.LegalPairings),
        output: catalog.PairingScores,
        executor: executor
      );

      pipeline.AddPythonStep<IEnumerable<PairingScore>, IEnumerable<BlockAssignment>>(
        label: "RunHungarian",
        description: "Globally optimal inp↔out assignment via Hungarian algorithm on the 48×48 score matrix (Python).",
        module: "Flows.Exploration.Steps.run_hungarian",
        function: "run_hungarian",
        input: catalog.PairingScores,
        output: catalog.BlockAssignments,
        executor: executor
      );

      pipeline.AddPythonStep<
        IEnumerable<BlockAssignment>,
        IEnumerable<PieceBlob>,
        IEnumerable<MeasurementSchema>,
        IEnumerable<CandidatePermutation>
      >(
        label: "RankOrderings",
        description: "Score Block execution orderings via activation chaining; emit top-N candidate permutations (Python).",
        module: "Flows.Exploration.Steps.rank_orderings",
        function: "rank_orderings",
        input: (catalog.BlockAssignments, catalog.Pieces, catalog.HistoricalData),
        output: catalog.CandidatePermutations,
        executor: executor
      );
    });
  }
}
