using System.Runtime.CompilerServices;

namespace Flowthru.Core.Data.Storage.Container;

/// <summary>
/// Container adapter for IEnumerable&lt;T&gt; - standard .NET collection type.
/// </summary>
/// <typeparam name="T">The element type</typeparam>
/// <remarks>
/// <para>
/// <strong>Characteristics:</strong>
/// </para>
/// <list type="bullet">
/// <item><strong>Eager materialization:</strong> Loads all rows into memory (List)</item>
/// <item><strong>Standard .NET:</strong> Works with all .NET LINQ operations</item>
/// <item><strong>Multiple enumeration:</strong> Safe - data is cached in memory</item>
/// <item><strong>Memory bound:</strong> Not suitable for very large datasets</item>
/// </list>
/// <para>
/// <strong>Use Cases:</strong>
/// </para>
/// <list type="bullet">
/// <item>Small to medium datasets (&lt;100K rows)</item>
/// <item>When multiple enumerations are needed</item>
/// <item>Standard .NET pipelines without functional dependencies</item>
/// <item>Testing and prototyping</item>
/// </list>
/// <para>
/// <strong>Alternatives:</strong>
/// </para>
/// <list type="bullet">
/// </list>
/// </remarks>
/// <example>
/// <code>
/// var adapter = new EnumerableContainerAdapter&lt;CompanySchema&gt;();
///
/// // From rows (materializes to List)
/// var companies = await adapter.FromRows(rowStream);
///
/// // Multiple enumeration - safe
/// var count = companies.Count();
/// var firstFive = companies.Take(5).ToList();
///
/// // Back to rows
/// var rowsAgain = adapter.ToRows(companies);
/// </code>
/// </example>
public sealed class EnumerableContainerAdapter<T> : IContainerAdapter<IEnumerable<T>, T>
{
  /// <summary>
  /// Creates a new enumerable container adapter.
  /// </summary>
  public EnumerableContainerAdapter() { }

  /// <inheritdoc/>
  public async Task<IEnumerable<T>> FromRows(IAsyncEnumerable<T> rows)
  {
    if (rows == null)
    {
      throw new ArgumentNullException(nameof(rows));
    }

    // Eagerly materialize to List
    var list = new List<T>();
    await foreach (var row in rows)
    {
      list.Add(row);
    }

    return list;
  }

  /// <inheritdoc/>
  public async IAsyncEnumerable<T> ToRows(IEnumerable<T> container)
  {
    if (container == null)
    {
      throw new ArgumentNullException(nameof(container));
    }

    // Stream back to rows
    foreach (var row in container)
    {
      yield return row;
    }
  }
}
