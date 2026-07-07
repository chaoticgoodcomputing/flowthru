using Flowthru.Data.Schema;

namespace StreamingBulkLoad.Data._01_Raw.Schemas;

/// <summary>
/// One synthetic financial transaction. A single row type serves two roles so
/// the eager and streaming ingest paths are apples-to-apples:
/// <list type="bullet">
///   <item>
///     the <em>Parquet row schema</em> — <c>[FlowthruSchema]</c> generates the
///     <c>IFlatSchema</c> / <c>IBinarySerializable</c> the Parquet serializer
///     needs; and
///   </item>
///   <item>
///     the <em>EF Core entity</em> persisted to the SQLite <c>Transactions</c>
///     table (keyed on <see cref="Id"/>, value-generated-never).
///   </item>
/// </list>
/// The record is written to Parquet once by the dataset generator, then read
/// back two ways — eagerly (materialised into a <c>List</c>, O(file)) and via a
/// streaming <c>FlowSource</c> (one row group at a time, O(batch)).
/// </summary>
[FlowthruSchema]
public partial record TransactionRecord
{
  /// <summary>Dense surrogate key assigned by the generator (0..N-1). Used as the SQLite primary key.</summary>
  public required int Id { get; init; }

  /// <summary>Owning account. Low cardinality, so the column dictionary-encodes well in Parquet.</summary>
  public required int AccountId { get; init; }

  /// <summary>Signed amount in integer cents — avoids floating-point drift in a money column.</summary>
  public required long AmountCents { get; init; }

  /// <summary>
  /// Free-text category as it arrives from the source — deliberately noisy
  /// (mixed case, stray whitespace) so the streaming <c>.Map</c> normalisation
  /// has something to do.
  /// </summary>
  public required string Category { get; init; }

  /// <summary>Transaction timestamp in UTC.</summary>
  public required DateTime TimestampUtc { get; init; }
}
