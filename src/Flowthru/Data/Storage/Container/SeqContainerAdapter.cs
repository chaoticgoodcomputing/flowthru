using System.Runtime.CompilerServices;
using LanguageExt;
using static LanguageExt.Prelude;

namespace Flowthru.Data.Storage.Container;

/// <summary>
/// Container adapter for LanguageExt Seq&lt;T&gt; - lazy immutable sequence.
/// </summary>
/// <typeparam name="T">The element type</typeparam>
/// <remarks>
/// <para>
/// <strong>Characteristics:</strong>
/// </para>
/// <list type="bullet">
/// <item><strong>Lazy evaluation:</strong> Rows computed on demand</item>
/// <item><strong>Immutable:</strong> Functional programming friendly</item>
/// <item><strong>Caching:</strong> Results cached after first enumeration</item>
/// <item><strong>Functional operations:</strong> Rich LINQ-like API with monadic operations</item>
/// </list>
/// <para>
/// <strong>Use Cases:</strong>
/// </para>
/// <list type="bullet">
/// <item>Functional programming pipelines using LanguageExt</item>
/// <item>When immutability is desired</item>
/// <item>Large datasets with lazy processing</item>
/// <item>Composable transformations</item>
/// </list>
/// <para>
/// <strong>LanguageExt Integration:</strong>
/// </para>
/// <para>
/// Seq provides monadic operations that compose well with other LanguageExt types:
/// </para>
/// <code>
/// var result = from company in catalog.Companies.Load()
///              where company.Rating &gt; 4.0
///              select company.Name;
/// </code>
/// </remarks>
/// <example>
/// <code>
/// var adapter = new SeqContainerAdapter&lt;CompanySchema&gt;();
///
/// // From rows (lazy)
/// var companies = await adapter.FromRows(rowStream);
///
/// // Lazy transformations
/// var highRated = companies
///     .Filter(c => c.Rating &gt; 4.5)
///     .Map(c => c.Name);
///
/// // Back to rows
/// var rowsAgain = adapter.ToRows(companies);
/// </code>
/// </example>
public sealed class SeqContainerAdapter<T> : IContainerAdapter<Seq<T>, T>
{
  /// <summary>
  /// Creates a new Seq container adapter.
  /// </summary>
  public SeqContainerAdapter() { }

  /// <inheritdoc/>
  public async Task<Seq<T>> FromRows(IAsyncEnumerable<T> rows)
  {
    if (rows == null)
    {
      throw new ArgumentNullException(nameof(rows));
    }

    // Materialize to list first, then convert to Seq
    // This ensures the async enumeration completes
    var list = new List<T>();
    await foreach (var row in rows)
    {
      list.Add(row);
    }

    return toSeq(list);
  }

  /// <inheritdoc/>
  public async IAsyncEnumerable<T> ToRows(Seq<T> container)
  {
    // Seq is already IEnumerable, so we can enumerate directly
    foreach (var row in container)
    {
      yield return row;
    }
  }
}
