using Flowthru.Flow;
using StreamingBulkLoad.Data;

namespace StreamingBulkLoad.Flows.StreamingIngest;

/// <summary>
/// The streaming path: pull the same Parquet one row group at a time, apply the
/// same normalise + filter as lazy <c>FlowSource</c> combinators, and bulk-write
/// to the same SQLite table batch-by-batch inside one transaction (O(batch)).
/// The whole load is a single on-DAG <c>AddBulkLoad</c> — scheduled, pre-flighted,
/// and under the read cap like any other Step.
/// </summary>
/// <remarks>
/// The transform lives in <c>catalog.CleanTransactionStream</c>
/// (<c>.AsStream().Map(Normalize).Where(IsValid)</c>); <c>AddBulkLoad</c> wires
/// that streaming view straight into the EF Core bulk sink. This is the runnable
/// shape of the #111 downstream case: a large dataset loaded on a
/// memory-constrained host without buffering the whole thing.
/// </remarks>
public static class StreamingIngestFlow
{
  public static BuiltFlow Create(Catalog catalog) =>
    FlowBuilder.CreateFlow("StreamingIngest", flow =>
      flow.AddBulkLoad(
        source: catalog.CleanTransactionStream,
        sink: catalog.NewTransactionSink(),
        label: "StreamTransactionsToSqlite"));
}
