namespace Flowthru.Core.Data.Storage;

/// <summary>
/// Interface for container adaptation - converts between streaming rows and in-memory containers.
/// </summary>
/// <typeparam name="TContainer">The in-memory container type (IEnumerable, IDataView, Seq, etc.)</typeparam>
/// <typeparam name="TRow">The row type (schema)</typeparam>
/// <remarks>
/// <para>
/// <strong>Responsibility:</strong> Abstract WHAT in-memory representation to use for data.
/// </para>
/// <para>
/// <strong>Separation of Concerns:</strong>
/// </para>
/// <para>
/// The container adapter is isolated from:
/// - Storage location (file vs memory) - handled by <see cref="IStorageMedium"/>
/// - Serialization format (CSV vs JSON) - handled by <see cref="IFormatSerializer{TRow}"/>
/// </para>
/// <para>
/// <strong>Bridge Pattern:</strong>
/// </para>
/// <para>
/// This adapter bridges between:
/// - <strong>Streaming rows</strong> (<see cref="IAsyncEnumerable{T}"/>) - format layer
/// - <strong>In-memory container</strong> (IEnumerable, IDataView) - application layer
/// </para>
/// <para>
/// <strong>Container Type Examples:</strong>
/// </para>
/// <list type="bullet">
/// <item><strong>IEnumerable&lt;TRow&gt;</strong> - Standard .NET collections</item>
/// <item><strong>IDataView</strong> - ML.NET's columnar data representation</item>
/// <item><strong>DataFrame</strong> - Pandas-style dataframe (future)</item>
/// <item><strong>IObservable&lt;TRow&gt;</strong> - Reactive streams (future)</item>
/// </list>
/// <para>
/// <strong>Design Pattern:</strong>
/// </para>
/// <para>
/// This is the top layer in the composition pattern:
/// </para>
/// <code>
/// Medium (bytes) → Format (rows) → Container (in-memory)
/// Stream         → IAsyncEnumerable&lt;TRow&gt; → IEnumerable&lt;TRow&gt; / IDataView
/// </code>
/// </remarks>
/// <example>
/// <code>
/// // IEnumerable container adapter
/// var enumerableAdapter = new EnumerableContainerAdapter&lt;CompanySchema&gt;();
/// var companies = await enumerableAdapter.FromRows(rowStream);
/// // Type: IEnumerable&lt;CompanySchema&gt;
///
/// // IDataView container adapter
/// var dataViewAdapter = new DataViewContainerAdapter&lt;CompanySchema&gt;(mlContext);
/// var dataView = await dataViewAdapter.FromRows(rowStream);
/// // Type: IDataView (ML.NET)
/// </code>
/// </example>
public interface IContainerAdapter<TContainer, TRow>
{
  /// <summary>
  /// Materializes an async stream of rows into an in-memory container.
  /// </summary>
  /// <param name="rows">Async stream of rows from format deserialization</param>
  /// <returns>Task producing the populated container</returns>
  /// <remarks>
  /// <para>
  /// <strong>Materialization Strategy:</strong>
  /// </para>
  /// <para>
  /// Different containers have different materialization approaches:
  /// </para>
  /// <list type="bullet">
  /// <item><strong>IEnumerable:</strong> Eager - load all rows into List</item>
  /// <item><strong>Seq:</strong> Lazy - wrap async enumerable</item>
  /// <item><strong>IDataView:</strong> Columnar - convert to columnar format</item>
  /// </list>
  /// <para>
  /// <strong>Memory Considerations:</strong>
  /// </para>
  /// <para>
  /// Be aware that eager containers (IEnumerable) will load all data into memory.
  /// For large datasets, prefer streaming containers (Seq, IDataView with lazy loading).
  /// </para>
  /// </remarks>
  Task<TContainer> FromRows(IAsyncEnumerable<TRow> rows);

  /// <summary>
  /// Converts an in-memory container back to an async stream of rows.
  /// </summary>
  /// <param name="container">The container to stream from</param>
  /// <returns>Async enumerable of rows for format serialization</returns>
  /// <remarks>
  /// <para>
  /// <strong>Streaming Strategy:</strong>
  /// </para>
  /// <para>
  /// Rows should be yielded lazily if the container supports it:
  /// </para>
  /// <list type="bullet">
  /// <item><strong>IEnumerable:</strong> Enumerate and yield</item>
  /// <item><strong>Seq:</strong> Already lazy - expose as async enumerable</item>
  /// <item><strong>IDataView:</strong> Enumerate rows from columnar format</item>
  /// </list>
  /// </remarks>
  IAsyncEnumerable<TRow> ToRows(TContainer container);
}
