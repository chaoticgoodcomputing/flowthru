using Flowthru.Data.Schema;

namespace Flowthru.Extensions.DuckDB.Tests.Fixtures;

/// <summary>Event row used by the end-to-end transform tests.</summary>
[FlowthruSchema]
public partial record EventRow
{
  public required long Id { get; init; }
  public required string Country { get; init; }
  public required DateTime OccurredAt { get; init; }
  public required double Value { get; init; }
}

/// <summary>Aggregate row — exercises COUNT/SUM results and explicit CASTs.</summary>
[FlowthruSchema]
public partial record CountryTotalRow
{
  public required string Country { get; init; }
  public required long EventCount { get; init; }
  public required double TotalValue { get; init; }
}

/// <summary>Join-result row for the multi-input transform test.</summary>
[FlowthruSchema]
public partial record EnrichedEventRow
{
  public required long Id { get; init; }
  public required string Country { get; init; }
  public required string Region { get; init; }
}

/// <summary>Lookup row joined against <see cref="EventRow"/>.</summary>
[FlowthruSchema]
public partial record CountryRegionRow
{
  public required string Country { get; init; }
  public required string Region { get; init; }
}

/// <summary>
/// Counts every CLR materialization of <see cref="InstrumentedRow"/>.
/// Static and separate from the schema record so the property-mapping
/// planner and the schema source generator only ever see plain
/// instance properties on the record itself.
/// </summary>
public static class RowMaterializationCounter
{
  private static long _count;

  /// <summary>Number of <see cref="InstrumentedRow"/> instances materialized since the last reset.</summary>
  public static long Count => Volatile.Read(ref _count);

  public static void Increment() => Interlocked.Increment(ref _count);

  public static void Reset() => Interlocked.Exchange(ref _count, 0);
}

/// <summary>
/// Schema whose <see cref="Id"/> init-accessor bumps
/// <see cref="RowMaterializationCounter"/>. Every path that materializes
/// a row of this type in the CLR — object initializers, the Parquet
/// adapter's reflection-driven <c>Load()</c>, any copy — must set
/// <c>Id</c>, so a zero count after a transform proves no row of this
/// schema ever entered the CLR.
/// </summary>
[FlowthruSchema]
public partial record InstrumentedRow
{
  private long _id;

  public required long Id
  {
    get => _id;
    init
    {
      RowMaterializationCounter.Increment();
      _id = value;
    }
  }

  public required string Country { get; init; }
  public required double Value { get; init; }
}

/// <summary>
/// Column-for-column twin of <see cref="InstrumentedRow"/> with no
/// instrumentation — used to seed and verify the files around a
/// transform without touching the counter.
/// </summary>
[FlowthruSchema]
public partial record PlainRow
{
  public required long Id { get; init; }
  public required string Country { get; init; }
  public required double Value { get; init; }
}
