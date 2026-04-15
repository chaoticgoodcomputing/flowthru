using DroppedNeuralNet.Data._08_Reporting.Schemas;
using Flowthru.Core.Data;

namespace DroppedNeuralNet.Data;

public partial class Catalog
{
    /// <summary>
    /// Diagnostic measurements from the Validation flow.
    /// Each row is a (Category, Metric, Value, Notes) tuple covering pairing signal
    /// quality, fixed-ordering baseline error, per-candidate forward-pass errors, and
    /// per-block residual blame attribution.
    /// Persisted as JSON so reports survive process exit and can be inspected offline.
    /// </summary>
    public IItem<IEnumerable<DiagnosticEntry>> Diagnostics =>
      CreateItem(
        () =>
          ItemFactory.Enumerable.Json<DiagnosticEntry>(
            label: "Diagnostics",
            filePath: $"{_basePath}/_08_Reporting/Datasets/diagnostics.json"
          )
      );

    /// <summary>
    /// Forward-pass evaluation of every candidate permutation produced by the Solver flow.
    /// Always populated — no candidate is gated by tolerance. Downstream SelectSolution
    /// picks the best entry to write to Solution.
    /// </summary>
    public IItem<IEnumerable<CandidateEvaluation>> CandidateEvaluations =>
      CreateItem(
        () =>
          ItemFactory.Enumerable.Json<CandidateEvaluation>(
            label: "CandidateEvaluations",
            filePath: $"{_basePath}/_08_Reporting/Datasets/candidate_evaluations.json"
          )
      );
}
