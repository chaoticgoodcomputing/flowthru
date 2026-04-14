using DroppedNeuralNet.Data._08_Reporting.Schemas;
using Flowthru.Core.Data;

namespace DroppedNeuralNet.Data;

public partial class Catalog
{
  /// <summary>
  /// Diagnostic measurements from the Validation flow.
  /// Each row is a (Category, Metric, Value, Notes) tuple covering pairing signal
  /// quality, fixed-ordering baseline error, and per-candidate forward-pass errors.
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
}
