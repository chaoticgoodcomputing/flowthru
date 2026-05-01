using Flowthru.Core.Effects;

namespace Flowthru.Core.Data.Storage;

/// <summary>
/// Optional interface for storage adapters that can return a row count without
/// materializing the full dataset.
/// </summary>
/// <remarks>
/// <para>
/// By default, <see cref="Item{T}.GetCountAsync"/> counts by calling <see cref="IStorageAdapter{T}.Load"/>
/// and enumerating the result. For I/O-bound adapters (databases, APIs) this materializes the
/// entire dataset just to count rows.
/// </para>
/// <para>
/// Implement this interface on a storage adapter to provide a cheap server-side count
/// (e.g. <c>COUNT(*)</c> SQL) that <see cref="Item{T}.GetCountAsync"/> will use instead.
/// </para>
/// <para>
/// <strong>The Flowthru engine does not call <see cref="Item{T}.GetCountAsync"/> during step execution.</strong>
/// This interface signals to metadata providers and user code that an adapter can be counted
/// cheaply — count-interested providers should check for it and skip adapters that lack it
/// rather than triggering a forced materialization. The reference <c>RowCountProvider</c> in
/// <c>Flowthru.Extensions.Metadata.Diagnostics</c> demonstrates the canonical pattern.
/// </para>
/// <para>
/// Existing adapters that do not implement this interface continue to work without change.
/// </para>
/// </remarks>
public interface IHasEfficientCount
{
  /// <summary>
  /// Returns the number of items in the backing store without materializing them.
  /// </summary>
  FlowIO<int> GetCountAsync();
}
