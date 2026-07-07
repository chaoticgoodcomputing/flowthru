using Flowthru.Data.Catalog;
using Flowthru.Data.Storage.Parquet;
using Flowthru.Prelude;
using StreamingBulkLoad.Data._01_Raw.Schemas;
using StreamingBulkLoad.Flows.StreamingIngest.Steps;

namespace StreamingBulkLoad.Data;

public partial class Catalog
{
  /// <summary>
  /// The multi-row-group Parquet dataset both variants read. Written once by the
  /// dataset generator with <see cref="WriteRowGroupSize"/>-row groups. In
  /// production this file would live in object storage (S3) — a forward-only
  /// stream that exercises the core make-seekable spill — but the streaming grain
  /// is identical on a local file.
  /// </summary>
  public IItem<IEnumerable<TransactionRecord>> RawTransactions =>
    CreateItem(() =>
      ItemFactory.Enumerable.Parquet<TransactionRecord>(
        "RawTransactions",
        $"{_basePath}/_01_Raw/Datasets/transactions.parquet",
        options: new ParquetItemOptions<TransactionRecord> { RowGroupSize = WriteRowGroupSize }));

  /// <summary>
  /// The streaming, transformed view of <see cref="RawTransactions"/>:
  /// <c>.AsStream().Map(Normalize).Where(IsValid)</c> wrapped as a read-only Item
  /// so <c>AddBulkLoad</c> can consume it on the DAG. O(batch) peak memory.
  /// </summary>
  public IReadOnlyItem<FlowSource<TransactionRecord>> CleanTransactionStream =>
    new CleanTransactionStreamView("CleanTransactionStream", RawTransactions.AsStream());

  /// <summary>
  /// The measured facts, written by the harness in <c>Program.cs</c>: one row per
  /// ingest variant. A Raw CSV so the Reporting Flow reads it like any other input.
  /// </summary>
  public IItem<IEnumerable<MemorySample>> MemorySamples =>
    CreateItem(() =>
      Item.Of<IEnumerable<MemorySample>>("MemorySamples")
        .Csv()
        .AtPath($"{_basePath}/_01_Raw/Datasets/memory_samples.csv")
        .Build());

  /// <summary>Markdown template for the memory report — <c>{{token}}</c> placeholders filled by the renderer step.</summary>
  public IItem<string> MemoryReportTemplate =>
    CreateItem(() =>
      Item.Of<string>("MemoryReportTemplate")
        .Text()
        .AtPath($"{_basePath}/_01_Raw/Templates/memory_report.md")
        .Build());
}
