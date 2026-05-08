namespace Flowthru.Data.Storage;

/// <summary>
/// Optional capability for storage adapters that can return a row count
/// without materializing the full dataset (e.g., a database adapter
/// running <c>COUNT(*)</c> server-side, an HTTP adapter consulting a
/// <c>Content-Length</c>-style header).
/// </summary>
/// <remarks>
/// <para>
/// Adapters that don't implement this interface count by enumeration
/// (load-and-count) at the consumer's discretion. Metadata providers
/// interested in row counts check for this interface and skip adapters
/// that lack it rather than forcing a materialization.
/// </para>
/// <para>
/// The Flowthru engine itself does not call this during step execution;
/// it is purely a metadata/diagnostic affordance.
/// </para>
/// </remarks>
public interface IHasEfficientCount
{
  /// <summary>
  /// Returns the number of items in the backing store without
  /// materializing them.
  /// </summary>
  FlowIO<int> GetCountAsync();
}
