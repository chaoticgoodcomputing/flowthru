using DroppedNeuralNet.Data;
using DroppedNeuralNet.Data._01_Raw.Schemas;
using DroppedNeuralNet.Data._02_Intermediate.Schemas;
using DroppedNeuralNet.Data._03_Primary.Schemas;
using DroppedNeuralNet.Data._07_ModelOutput.Schemas;
using DroppedNeuralNet.Flows.Solver.Steps;
using Flowthru.Core.Flows;
using Flowthru.Core.Steps;
using Flowthru.Extensions.Python.Execution;
using Flowthru.Extensions.Python.Steps;

namespace DroppedNeuralNet.Flows.Solver;

/// <summary>
/// Two-step solver pipeline.
///
/// Step 1 (C#) — FindLegalPairings:
///   Reads PieceMetadata and emits every structurally valid (inp, out) Block pairing.
///   Operates on dimension/type metadata only — no blob data accessed.
///
/// Step 2 (Python) — test_permutations:
///   Receives PieceMetadata and Pieces (raw blobs) separately, along with the legal pairings
///   and historical measurements. Joins metadata to blobs by PieceIndex, performs a recursive
///   search over Block assignments, assembles candidate networks, and validates against pred.
///   Emits the recovered permutation once a match within tolerance is found.
/// </summary>
public static class SolverFlow
{
  public static Flow Create(Catalog catalog, IPythonExecutor executor)
  {
    return FlowBuilder.CreateFlow(pipeline =>
    {
      pipeline.AddStep(
        label: "FindLegalPairings",
        description: "Enumerate all (inp, out) piece pairings that satisfy Block dimension constraints (C#). Metadata only — no blobs.",
        transform: FindLegalPairingsStep.Create(),
        input: catalog.PieceMetadata,
        output: catalog.LegalPairings
      );

      pipeline.AddPythonStep<
        IEnumerable<PieceMetadata>,
        IEnumerable<PieceBlob>,
        IEnumerable<BlockCandidate>,
        IEnumerable<MeasurementSchema>,
        PermutationSolution
      >(
        label: "TestPermutations",
        description: "Join metadata to blobs by PieceIndex, search block assignments via forward-pass, emit the matching permutation (Python).",
        module: "Flows.Solver.Steps.test_permutations",
        function: "test_permutations",
        input: (
          catalog.PieceMetadata,
          catalog.Pieces,
          catalog.LegalPairings,
          catalog.HistoricalData
        ),
        output: catalog.Solution,
        executor: executor
      );
    });
  }
}
