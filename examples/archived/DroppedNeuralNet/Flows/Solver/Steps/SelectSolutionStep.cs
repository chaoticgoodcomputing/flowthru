using System.Text.Json;
using DroppedNeuralNet.Data._07_ModelOutput.Schemas;
using DroppedNeuralNet.Data._08_Reporting.Schemas;
using Flowthru.Core.Steps;

namespace DroppedNeuralNet.Flows.Solver.Steps;

/// <summary>
/// Picks the best <see cref="CandidateEvaluation"/> and writes it to <see cref="PermutationSolution"/>.
///
/// Selection priority:
///   1. First candidate whose <c>PassesTolerance == 1</c>, in CandidateIndex order.
///   2. If none passed, the candidate with the lowest <c>MaxErr</c>.
///
/// This step never throws when no candidate cleared tolerance — the full evaluation
/// report is available in the CandidateEvaluations catalog entry for inspection.
/// </summary>
[FlowthruStep]
public static class SelectSolutionStep
{
  public static Func<IEnumerable<CandidateEvaluation>, PermutationSolution> Create()
  {
    return (evaluations) =>
    {
      var ordered = evaluations.OrderBy(e => e.CandidateIndex).ToList();
      if (ordered.Count == 0)
        throw new InvalidOperationException(
          "CandidateEvaluations is empty — run Exploration before Solver."
        );

      var best =
        ordered.FirstOrDefault(e => e.PassesTolerance == 1)
        ?? ordered.MinBy(e => e.MaxErr)!;

      return new PermutationSolution
      {
        Permutation = JsonSerializer.Deserialize<int[]>(best.Permutation) ?? Array.Empty<int>(),
      };
    };
  }
}
