using Flowthru.Core.Abstractions;
using Flowthru.Core.Data;
using Flowthru.Core.Data.Storage;
using Flowthru.DataFrames;

namespace Flowthru.Extensions.Spark.Data;

/// <summary>
/// Extension point for <c>ItemFactory.Frame</c> factory methods.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="TypedFrame{T}"/> items are always in-memory. There is intentionally no
/// file-backed variant: a <see cref="TypedFrame{T}"/> represents a deferred Spark execution
/// plan, not a materialized dataset. Persisting or loading a plan from disk is meaningless —
/// use a file-backed <c>ItemFactory.Enumerable.*</c> item at the point where data must be
/// materialized instead.
/// </para>
/// <para>
/// The in-memory storage adapter passes the <see cref="TypedFrame{T}"/> reference directly
/// between steps (Save → Load is a reference assignment). No Spark action is triggered; the
/// execution plan remains deferred until a step explicitly calls
/// <see cref="SparkRowHydrator{T}.Collect"/>.
/// </para>
/// </remarks>
public sealed class FrameItemFactory
{
    internal FrameItemFactory() { }

    /// <summary>
    /// Creates an in-memory catalog item holding a <see cref="TypedFrame{T}"/>.
    /// </summary>
    /// <typeparam name="TRow">
    /// The schema type for the frame's rows. Must be a flat schema — non-flat types
    /// contain nested or collection properties incompatible with scalar Spark columns.
    /// </typeparam>
    /// <param name="label">Unique catalog label for DAG resolution.</param>
    /// <returns>
    /// A catalog item with memory storage. No serialization occurs; the frame reference
    /// is passed between steps as-is.
    /// </returns>
    public Item<TypedFrame<TRow>> Memory<TRow>(string label)
        where TRow : notnull, IFlatSchema
    {
        var storage = new MemoryStorageAdapter<TypedFrame<TRow>>();
        return new Item<TypedFrame<TRow>>(label, storage);
    }
}
