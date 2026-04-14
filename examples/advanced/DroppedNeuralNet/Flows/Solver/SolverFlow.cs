using DroppedNeuralNet.Data;
using DroppedNeuralNet.Data._01_Raw.Schemas;
using DroppedNeuralNet.Data._05_Candidates.Schemas;
using DroppedNeuralNet.Data._07_ModelOutput.Schemas;
using Flowthru.Core.Flows;
using Flowthru.Extensions.Python.Execution;
using Flowthru.Extensions.Python.Steps;

namespace DroppedNeuralNet.Flows.Solver;

/// <summary>
/// Validation pipeline: receives ranked candidate permutations from Exploration and
/// confirms the correct one via forward-pass against the historical prediction record.
///
/// Step 1 (Python) — validate_permutations:
///   Iterates CandidatePermutations in rank order, reconstructs each network from raw
///   piece blobs, runs a forward pass over the full historical dataset, and returns
///   the first permutation whose output matches <c>pred</c> within tolerance.
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
        PermutationSolution
      >(
        label: "ValidatePermutations",
        description: "Forward-pass each ranked candidate permutation; return the first that matches pred within tolerance (Python).",
        module: "Flows.Solver.Steps.validate_permutations",
        function: "validate_permutations",
        input: (catalog.CandidatePermutations, catalog.Pieces, catalog.HistoricalData),
        output: catalog.Solution,
        executor: executor
      );
    });
  }
}
