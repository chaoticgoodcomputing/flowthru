namespace Flowthru.Data.Capabilities;

/// <summary>
/// Capability interface for storage adapters that support lazy streaming.
/// </summary>
/// <typeparam name="T">The element type being streamed</typeparam>
/// <remarks>
/// <para>
/// <strong>Purpose:</strong> Enable processing of large datasets without loading everything into memory.
/// </para>
/// <para>
/// <strong>Streaming vs Materialized:</strong>
/// </para>
/// <list type="bullet">
/// <item><strong>Materialized (Load):</strong> Loads entire dataset into memory (IEnumerable)</item>
/// <item><strong>Streaming (Stream):</strong> Yields elements lazily as they are read</item>
/// </list>
/// <para>
/// <strong>When to Use Streaming:</strong>
/// </para>
/// <list type="bullet">
/// <item>Large datasets that don't fit in memory</item>
/// <item>Pipeline stages that can process row-by-row</item>
/// <item>Early termination scenarios (find first match)</item>
/// <item>Backpressure-sensitive pipelines</item>
/// </list>
/// <para>
/// <strong>Trade-offs:</strong>
/// </para>
/// <list type="bullet">
/// <item>✅ Constant memory usage</item>
/// <item>✅ Faster time-to-first-element</item>
/// <item>❌ Cannot rewind or random access</item>
/// <item>❌ Multiple enumerations = multiple reads</item>
/// </list>
/// <para>
/// <strong>Discovery Pattern:</strong>
/// </para>
/// <para>
/// Nodes can optionally use streaming when available:
/// </para>
/// <code>
/// // Check if streaming is supported
/// if (catalogEntry is IStreamable&lt;RowType&gt; streamable)
/// {
///     await foreach (var row in streamable.Stream())
///     {
///         // Process row-by-row without loading all into memory
///     }
/// }
/// else
/// {
///     // Fall back to materialized load
///     var data = await catalogEntry.Load().Run();
/// }
/// </code>
/// </remarks>
/// <example>
/// <code>
/// public class CsvStorageAdapter&lt;T&gt; : IStorageAdapter&lt;IEnumerable&lt;T&gt;&gt;, IStreamable&lt;T&gt;
/// {
///     public IAsyncEnumerable&lt;T&gt; Stream()
///     {
///         return StreamImpl();
///     }
///
///     private async IAsyncEnumerable&lt;T&gt; StreamImpl()
///     {
///         using var stream = await _medium.ReadStream().Run();
///         await foreach (var row in _format.DeserializeRows(stream))
///         {
///             yield return row;  // Lazy yielding
///         }
///     }
/// }
///
/// // Usage in node
/// public async Task&lt;Output&gt; Transform(ICatalogEntry&lt;IEnumerable&lt;Input&gt;&gt; inputEntry)
/// {
///     if (inputEntry is IStreamable&lt;Input&gt; streamable)
///     {
///         // Stream processing - low memory
///         var count = 0;
///         await foreach (var row in streamable.Stream())
///         {
///             count++;
///             if (count > 1000) break;  // Early termination
///         }
///         return new Output { Count = count };
///     }
///     else
///     {
///         // Materialized processing - loads all
///         var data = await inputEntry.Load().Run();
///         return data.Match(
///             Succ: rows => new Output { Count = rows.Count() },
///             Fail: err => throw new Exception(err.ToString())
///         );
///     }
/// }
/// </code>
/// </example>
public interface IStreamable<T>
{
  /// <summary>
  /// Streams elements lazily without loading everything into memory.
  /// </summary>
  /// <returns>Async enumerable that yields elements as they are read</returns>
  /// <remarks>
  /// <para>
  /// <strong>Implementation Guidelines:</strong>
  /// </para>
  /// <list type="bullet">
  /// <item>Yield elements as they are read from storage</item>
  /// <item>Do not buffer or cache elements</item>
  /// <item>Support cancellation via CancellationToken (implicit in IAsyncEnumerable)</item>
  /// <item>Dispose resources properly (use async using)</item>
  /// </list>
  /// <para>
  /// <strong>Multiple Enumeration Warning:</strong>
  /// </para>
  /// <para>
  /// Each enumeration may re-read from storage. If you need to enumerate multiple times,
  /// materialize the stream first:
  /// </para>
  /// <code>
  /// var list = await streamable.Stream().ToListAsync();
  /// </code>
  /// </remarks>
  IAsyncEnumerable<T> Stream();
}
