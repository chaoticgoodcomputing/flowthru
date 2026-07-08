using Flowthru.Data.Catalog;
using Flowthru.Data.Storage;
using Flowthru.Prelude;

namespace Flowthru.Step.DuckDb;

/// <summary>
/// Binds one input catalog item to the SQL relation name a DuckDB
/// transform refers to it by. Build with <see cref="From{TRow}"/>; the
/// relation name defaults to the item's label, overridable when the
/// label doesn't read well in SQL (or collides with another input).
/// </summary>
/// <remarks>
/// <para>
/// Construction validates the item is byte-addressable (backed by a
/// file or object medium) and fails immediately when it isn't — a
/// memory- or database-backed item can never feed an engine transform,
/// and that's a wiring bug, not a runtime condition. <em>Where</em> the
/// bytes live is resolved later, when the step executes.
/// </para>
/// <para>
/// Relation names are always quoted in the generated SQL, so any
/// non-empty name is legal here — but names that aren't plain
/// identifiers (<c>orders</c>, <c>raw_events</c>) must be
/// double-quoted in the transform SQL too, per standard SQL rules.
/// </para>
/// </remarks>
public sealed class DuckDbInputRelation
{
  private DuckDbInputRelation(IItem item, string relationName, FlowIO<ByteLocation> location)
  {
    Item = item;
    RelationName = relationName;
    Location = location;
  }

  /// <summary>The catalog item feeding this relation — the step's DAG input.</summary>
  public IItem Item { get; }

  /// <summary>The name the transform SQL refers to this input by.</summary>
  public string RelationName { get; }

  /// <summary>
  /// Deferred resolution of where the item's bytes live; run by the
  /// step at execution time.
  /// </summary>
  internal FlowIO<ByteLocation> Location { get; }

  /// <summary>
  /// Bind <paramref name="item"/> as a transform input relation named
  /// <paramref name="relationName"/> (defaults to the item's label).
  /// </summary>
  /// <typeparam name="TRow">The item's row type.</typeparam>
  /// <param name="item">
  /// A byte-addressable, row-sequence catalog item — e.g. a Parquet
  /// item. Non-addressable items (memory, database, spreadsheet) are
  /// rejected here, at wire-up.
  /// </param>
  /// <param name="relationName">
  /// Optional SQL relation name override; <c>null</c> uses the item's
  /// label.
  /// </param>
  public static DuckDbInputRelation From<TRow>(
    IItem<IEnumerable<TRow>> item,
    string? relationName = null
  )
    where TRow : notnull
  {
    if (item is null) throw new ArgumentNullException(nameof(item));

    var name = relationName ?? item.Label;
    if (string.IsNullOrWhiteSpace(name))
    {
      throw new ArgumentException(
        "Relation name cannot be null or whitespace.", nameof(relationName)
      );
    }

    // LocateBytes() validates addressability eagerly (throwing for
    // memory/database-backed items) and defers the actual resolution
    // until the returned effect runs.
    return new DuckDbInputRelation(item, name, item.LocateBytes());
  }
}
