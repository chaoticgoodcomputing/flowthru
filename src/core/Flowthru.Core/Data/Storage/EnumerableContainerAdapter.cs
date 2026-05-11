namespace Flowthru.Data.Storage;

/// <summary>
/// Container adapter for <see cref="IEnumerable{T}"/> — eagerly
/// materializes a row stream into a <see cref="List{T}"/>, and streams
/// rows out of an enumerable for serialization. The standard container
/// for tabular data flowing through Flowthru.
/// </summary>
/// <typeparam name="T">The element type.</typeparam>
/// <remarks>
/// Eager materialization: <see cref="FromRows"/> loads every row into
/// memory. For large datasets where bounded-memory processing matters,
/// consider streaming containers (currently Core ships only this eager
/// variant; future extensions may add lazy/columnar alternatives).
/// </remarks>
public sealed class EnumerableContainerAdapter<T> : IContainerAdapter<IEnumerable<T>, T>
{
  /// <inheritdoc/>
  public async Task<IEnumerable<T>> FromRows(IAsyncEnumerable<T> rows)
  {
    if (rows is null)
    {
      throw new ArgumentNullException(nameof(rows));
    }
    var list = new List<T>();
    await foreach (var row in rows.ConfigureAwait(false))
    {
      list.Add(row);
    }
    return list;
  }

  /// <inheritdoc/>
  public async IAsyncEnumerable<T> ToRows(IEnumerable<T> container)
  {
    if (container is null)
    {
      throw new ArgumentNullException(nameof(container));
    }
    foreach (var row in container)
    {
      yield return row;
      await Task.Yield();
    }
  }
}
