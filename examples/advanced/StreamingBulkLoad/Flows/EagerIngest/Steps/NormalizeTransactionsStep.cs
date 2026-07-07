using Flowthru.Step;
using StreamingBulkLoad.Data._01_Raw.Schemas;
using StreamingBulkLoad.Flows.Shared;

namespace StreamingBulkLoad.Flows.EagerIngest.Steps;

/// <summary>
/// The eager transform: normalise then filter over a fully-materialised
/// <c>IEnumerable</c>. Identical work to the streaming path's
/// <c>.Map(Normalize).Where(IsValid)</c> — the difference is that its input is
/// already an in-memory <c>List</c> (the eager Parquet Load buffered the whole
/// file), so peak memory is O(file). Deferred LINQ here changes nothing: the
/// backing list is resident for the life of the downstream bulk insert.
/// </summary>
[FlowthruStep]
public static class NormalizeTransactionsStep
{
  public static Func<IEnumerable<TransactionRecord>, IEnumerable<TransactionRecord>> Create() =>
    rows => rows.Select(TransactionCleaning.Normalize).Where(TransactionCleaning.IsValid);
}
