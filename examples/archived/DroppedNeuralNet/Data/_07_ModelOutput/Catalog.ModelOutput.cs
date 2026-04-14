using DroppedNeuralNet.Data._07_ModelOutput.Schemas;
using Flowthru.Core.Data;

namespace DroppedNeuralNet.Data;

public partial class Catalog
{
  /// <summary>
  /// The recovered permutation of all 97 pieces in original execution order.
  /// Persisted as JSON so the answer survives process exit.
  /// </summary>
  public IItem<PermutationSolution> Solution =>
    CreateItem(
      () =>
        ItemFactory.Single.Json<PermutationSolution>(
          label: "Solution",
          filePath: $"{_basePath}/_07_ModelOutput/solution.json"
        )
    );
}
