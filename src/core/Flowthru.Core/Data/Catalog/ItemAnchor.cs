namespace Flowthru.Data.Catalog;

/// <summary>
/// Typed entry anchor returned by <see cref="Item.Of{T}(string)"/> —
/// carries the catalog item's label and its container type
/// <typeparamref name="T"/>. Format extensions hang off this anchor
/// via extension methods, dispatching on <typeparamref name="T"/>'s
/// shape (<c>IEnumerable&lt;TRow&gt;</c> for collection-shaped items,
/// bare <c>T</c> for singletons) through generic-type-inference
/// against the receiver type.
/// </summary>
/// <remarks>
/// <para>
/// The user never constructs <see cref="ItemAnchor{T}"/> directly; the
/// constructor is internal. The public surface is
/// <see cref="Item.Of{T}"/> at entry, then a chain of format-specific
/// extension methods (<c>.Csv()</c>, <c>.Json()</c>,
/// <c>.EFCoreEntity&lt;TContext&gt;()</c>, etc.) that each return a
/// typed builder, terminating in <c>.Build()</c>.
/// </para>
/// </remarks>
public sealed class ItemAnchor<T> where T : notnull
{
  /// <summary>Catalog label for this item.</summary>
  public string Label { get; }

  internal ItemAnchor(string label)
  {
    Label = label ?? throw new ArgumentNullException(nameof(label));
  }
}

/// <summary>
/// Catalog-item entry point. <c>Item.Of&lt;T&gt;(label)</c> is the
/// single canonical entry — the type parameter expresses the
/// container shape (<c>IEnumerable&lt;TRow&gt;</c> for collections,
/// bare <c>T</c> for single values) which downstream format
/// extensions dispatch on.
/// </summary>
/// <example>
/// <code>
/// // Single-value items
/// Item.Of&lt;TrainedModel&gt;("model").Json().AtPath(p).Build();
/// Item.Of&lt;string&gt;("template").Text().AtPath(p).Build();
///
/// // Collection items — the row type is inferred from IEnumerable&lt;TRow&gt;
/// Item.Of&lt;IEnumerable&lt;CompaniesSchema&gt;&gt;("companies").Csv().AtPath(p).Build();
/// Item.Of&lt;IEnumerable&lt;Shuttle&gt;&gt;("shuttles").EFCoreTable&lt;MyDbContext&gt;()
///   .WithContextFactory(f).Build();
///
/// // List&lt;T&gt; etc. work too — anything implementing IEnumerable&lt;TRow&gt;
/// Item.Of&lt;List&lt;X&gt;&gt;("xs").Json().AtPath(p).Build();
/// </code>
/// </example>
public static class Item
{
  /// <summary>
  /// Begin a catalog-item declaration with a label. The type
  /// parameter <typeparamref name="T"/> declares the container type;
  /// downstream <c>.Format()</c> calls dispatch on its shape.
  /// </summary>
  public static ItemAnchor<T> Of<T>(string label) where T : notnull => new(label);
}
