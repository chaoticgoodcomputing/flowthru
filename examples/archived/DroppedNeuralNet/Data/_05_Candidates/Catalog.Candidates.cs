using DroppedNeuralNet.Data._05_Candidates.Schemas;
using Flowthru.Core.Data;

namespace DroppedNeuralNet.Data;

public partial class Catalog
{
  /// <summary>
  /// Ranked candidate execution orderings produced by the rank_orderings step.
  /// Each row is a JSON-encoded int[97] permutation. Persisted as JSON so the
  /// Solver flow can be run independently after Exploration completes.
  /// </summary>
  public IItem<IEnumerable<CandidatePermutation>> CandidatePermutations =>
    CreateItem(
      () =>
        ItemFactory.Enumerable.Json<CandidatePermutation>(
          label: "CandidatePermutations",
          filePath: $"{_basePath}/_05_Candidates/Datasets/candidate_permutations.json"
        )
    );
}
