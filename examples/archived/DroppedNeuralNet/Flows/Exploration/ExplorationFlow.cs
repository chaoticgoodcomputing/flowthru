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
/// Step 2 (Python) — compute_svd_activation_scores:
///   Scores each legal (inp, out) candidate by SVD subspace alignment. The top-R left
///   singular vectors of W_inp (principal hidden directions written to) are compared against
///   the top-R right singular vectors of W_out (principal hidden directions read from).
///   High overlap → trained pair; orthogonal subspaces → mismatch.
///   No historical data required — purely geometric.
///
///   Supersedes compute_activation_scores (Pearson correlation on data pass, v2) and
///   compute_pairing_scores (Frobenius of W_out @ W_inp, v1). Both are retained but
///   commented out for reference.
///
/// Step 3 (Python) — run_gumbel_sinkhorn:
///   Runs K=500 Gumbel-perturbed Sinkhorn solves over a temperature-annealed log-score
///   matrix (τ: 2.0 → 0.05), accumulates per-pair vote counts, and returns the consensus
///   perfect matching. More robust than a single Hungarian solve against a flat cost matrix.
///
///   Supersedes run_hungarian (single deterministic solve + linear Sinkhorn), which is
///   retained but commented out.
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

            /* SUPERSEDED — weight-space Frobenius signal collapses under normalization.
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
            */

            pipeline.AddPythonStep<
          IEnumerable<PieceMetadata>,
          IEnumerable<PieceBlob>,
          IEnumerable<BlockCandidate>,
          IEnumerable<PairingScore>
        >(
          label: "ComputeSvdActivationScores",
          description: "Score each (inp, out) candidate by SVD subspace alignment between inp write-directions and out read-directions. Pure weight geometry, no data pass required (Python).",
          module: "Flows.Exploration.Steps.compute_svd_activation_scores",
          function: "compute_svd_activation_scores",
          input: (catalog.PieceMetadata, catalog.Pieces, catalog.LegalPairings),
          output: catalog.PairingScores,
          executor: executor
        );

            /* SUPERSEDED — Pearson correlation between mean activation and column attention norms.
               Required a full data pass; SVD subspace alignment captures the same signal geometrically.
            pipeline.AddPythonStep<
              IEnumerable<PieceMetadata>,
              IEnumerable<PieceBlob>,
              IEnumerable<BlockCandidate>,
              IEnumerable<MeasurementSchema>,
              IEnumerable<PairingScore>
            >(
              label: "ComputeActivationScores",
              description: "Run historical data through each inp piece; score each out piece by residual response magnitude. Lower = trained pair (Python).",
              module: "Flows.Exploration.Steps.compute_activation_scores",
              function: "compute_activation_scores",
              input: (
                catalog.PieceMetadata,
                catalog.Pieces,
                catalog.LegalPairings,
                catalog.HistoricalData
              ),
              output: catalog.PairingScores,
              executor: executor
            );
            */

            pipeline.AddPythonStep<IEnumerable<PairingScore>, IEnumerable<BlockAssignment>>(
          label: "RunGumbelSinkhorn",
          description: "Consensus inp↔out assignment via K Gumbel-Sinkhorn perturbation samples with temperature annealing (Python).",
          module: "Flows.Exploration.Steps.run_gumbel_sinkhorn",
          function: "run_gumbel_sinkhorn",
          input: catalog.PairingScores,
          output: catalog.BlockAssignments,
          executor: executor
        );

            /* SUPERSEDED — single deterministic Hungarian solve + linear-space Sinkhorn normalization.
               Fragile at low signal std (~0.07); replaced by Gumbel-Sinkhorn consensus.
            pipeline.AddPythonStep<IEnumerable<PairingScore>, IEnumerable<BlockAssignment>>(
              label: "RunHungarian",
              description: "Globally optimal inp↔out assignment via Hungarian algorithm on the 48×48 score matrix (Python).",
              module: "Flows.Exploration.Steps.run_hungarian",
              function: "run_hungarian",
              input: catalog.PairingScores,
              output: catalog.BlockAssignments,
              executor: executor
            );
            */

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
