using DroppedNeuralNet.Data;
using DroppedNeuralNet.Data._01_Raw.Schemas;
using DroppedNeuralNet.Data._05_Candidates.Schemas;
using DroppedNeuralNet.Data._07_ModelOutput.Schemas;
using DroppedNeuralNet.Data._08_Reporting.Schemas;
using DroppedNeuralNet.Flows.Solver.Steps;
using Flowthru.Core.Flows;
using Flowthru.Extensions.Python.Execution;
using Flowthru.Extensions.Python.Steps;

namespace DroppedNeuralNet.Flows.Solver;

/// <summary>
/// Solver pipeline: receives ranked candidate permutations from Exploration and selects
/// the best solution via forward-pass evaluation and C# selection logic.
///
/// Step 1 (Python) — validate_permutations:
///   Reconstructs each candidate network from piece blobs, runs a forward pass over the
///   full historical dataset, and emits a <c>CandidateEvaluation</c> row per candidate
///   containing MaxErr, MeanErr, and a PassesTolerance flag. Never throws — all candidates
///   are always reported regardless of whether tolerance was met.
///
/// Step 2 (C#) — SelectSolution:
///   Reads the full evaluation catalog entry and writes the winning permutation to
///   <c>Solution</c>. Prefers the first tolerance-passing candidate; falls back to the
///   lowest-MaxErr candidate if none passed tolerance.
/// </summary>
public static class SolverFlow
{
  public static Flow Create(Catalog catalog, IPythonExecutor executor)
  {
    return FlowBuilder.CreateFlow(pipeline =>
    {
      pipeline.AddPythonStep<
        IEnumerable<CandidatePermutation>,
        IEnumerable<PieceBlob>,
        IEnumerable<MeasurementSchema>,
        IEnumerable<CandidateEvaluation>
      >(
        label: "ValidatePermutations",
        description: "Forward-pass each ranked candidate permutation; emit a CandidateEvaluation row per candidate — no tolerance gating (Python).",
        module: "Flows.Solver.Steps.validate_permutations",
        function: "validate_permutations",
        input: (catalog.CandidatePermutations, catalog.Pieces, catalog.HistoricalData),
        output: catalog.CandidateEvaluations,
        executor: executor
      );

      pipeline.AddStep(
        label: "SelectSolution",
        description: "Pick the best-scoring evaluated permutation as the solution (C#).",
        transform: SelectSolutionStep.Create(),
        input: catalog.CandidateEvaluations,
        output: catalog.Solution
      );
    });
  }
}
