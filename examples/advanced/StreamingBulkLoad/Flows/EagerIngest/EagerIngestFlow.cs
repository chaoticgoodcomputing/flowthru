using Flowthru.Flow;
using StreamingBulkLoad.Data;
using StreamingBulkLoad.Data._01_Raw.Schemas;
using StreamingBulkLoad.Flows.EagerIngest.Steps;

namespace StreamingBulkLoad.Flows.EagerIngest;

/// <summary>
/// The eager baseline: read the Parquet as a materialised <c>IEnumerable</c>
/// (O(file)), normalise + filter, and bulk-write to SQLite in one shot. A single
/// ordinary Step — the memory cost is entirely in the eager Load buffering the
/// whole file before the transform ever runs.
/// </summary>
public static class EagerIngestFlow
{
  public static BuiltFlow Create(Catalog catalog) =>
    FlowBuilder.CreateFlow("EagerIngest", flow =>
      flow.AddStep<IEnumerable<TransactionRecord>, IEnumerable<TransactionRecord>>(
        label: "NormalizeAndLoadEager",
        transform: NormalizeTransactionsStep.Create(),
        inputs: catalog.RawTransactions,
        outputs: catalog.EagerTransactionsTable));
}
