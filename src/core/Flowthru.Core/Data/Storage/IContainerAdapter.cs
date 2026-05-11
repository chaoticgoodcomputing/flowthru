namespace Flowthru.Data.Storage;

/// <summary>
/// Container adapter — bridges between streaming rows
/// (<see cref="IAsyncEnumerable{T}"/>, format-layer output) and in-memory
/// containers (<see cref="IEnumerable{T}"/>, ML.NET <c>IDataView</c>,
/// pandas-style dataframes, etc.). The third member of the
/// medium × format × container composition.
/// </summary>
/// <typeparam name="TContainer">The in-memory container type.</typeparam>
/// <typeparam name="TRow">The row type the container holds.</typeparam>
public interface IContainerAdapter<TContainer, TRow>
{
  /// <summary>
  /// Materializes an async stream of rows into an in-memory container.
  /// Eager containers (<see cref="IEnumerable{T}"/>) load every row into
  /// the container; lazy containers (Seq, observables) wrap the stream
  /// without forcing it.
  /// </summary>
  Task<TContainer> FromRows(IAsyncEnumerable<TRow> rows);

  /// <summary>
  /// Converts an in-memory container back into an async stream of rows
  /// for format serialization. Yields lazily if the container supports it.
  /// </summary>
  IAsyncEnumerable<TRow> ToRows(TContainer container);
}
