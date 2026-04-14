using DroppedNeuralNet.Data._04_Analysis.Schemas;
using Flowthru.Core.Data;

namespace DroppedNeuralNet.Data;

public partial class Catalog
{
  /// <summary>
  /// Frobenius norms of the weight product ||out.W @ inp.W||_F for every legal (inp, out) pairing.
  /// Produced by the compute_pairing_scores Python step; persisted as JSON for inspection.
  /// </summary>
  public IItem<IEnumerable<PairingScore>> PairingScores =>
    CreateItem(
      () =>
        ItemFactory.Enumerable.Json<PairingScore>(
          label: "PairingScores",
          filePath: $"{_basePath}/_04_Analysis/Datasets/pairing_scores.json"
        )
    );

  /// <summary>
  /// Optimal (inp, out) Block pairings selected by the Hungarian algorithm.
  /// Minimises total ProductNorm across all 48 Block assignments.
  /// Persisted as JSON; consumed by the rank_orderings step.
  /// </summary>
  public IItem<IEnumerable<BlockAssignment>> BlockAssignments =>
    CreateItem(
      () =>
        ItemFactory.Enumerable.Json<BlockAssignment>(
          label: "BlockAssignments",
          filePath: $"{_basePath}/_04_Analysis/Datasets/block_assignments.json"
        )
    );
}
