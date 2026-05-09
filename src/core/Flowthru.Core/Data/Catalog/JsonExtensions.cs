using Flowthru.Data.Schema;
using Flowthru.Data.Storage;

namespace Flowthru.Data.Catalog;

/// <summary>
/// JSON item-builder extensions on <see cref="ItemAnchor{T}"/>. Two
/// overloads of <c>.Json()</c> — the singleton form for
/// <c>T : IStructuredSerializable</c>, the array form for
/// <c>ItemAnchor&lt;IEnumerable&lt;TRow&gt;&gt;</c> with
/// <c>TRow : IStructuredSerializable</c>. The constraint on
/// <c>IStructuredSerializable</c> prevents the singleton overload
/// from binding to a collection-typed anchor; the array overload's
/// receiver-type pattern <c>IEnumerable&lt;TRow&gt;</c> lets C# infer
/// <c>TRow</c> from anchors typed over <c>IEnumerable&lt;X&gt;</c>,
/// <c>List&lt;X&gt;</c>, <c>X[]</c>, or any other collection type.
/// </summary>
public static class JsonExtensions
{
  /// <summary>
  /// Singleton JSON: the file contains a single JSON object
  /// representing <typeparamref name="T"/>.
  /// </summary>
  public static JsonSingletonBuilder<T> Json<T>(this ItemAnchor<T> anchor)
    where T : notnull, IStructuredSerializable =>
    new(anchor.Label);

  /// <summary>
  /// Array JSON: the file contains a JSON array of
  /// <typeparamref name="TRow"/> elements.
  /// </summary>
  public static JsonArrayBuilder<TRow> Json<TRow>(
    this ItemAnchor<IEnumerable<TRow>> anchor
  )
    where TRow : notnull, IStructuredSerializable =>
    new(anchor.Label);
}

/// <summary>Tier-1 builder for a singleton-JSON catalog item.</summary>
public sealed class JsonSingletonBuilder<T> where T : notnull, IStructuredSerializable
{
  private readonly string _label;
  private string? _path;

  internal JsonSingletonBuilder(string label)
  {
    _label = label;
  }

  /// <summary>Set the filesystem path for this JSON file.</summary>
  public JsonSingletonBuilder<T> AtPath(string path)
  {
    if (string.IsNullOrWhiteSpace(path))
      throw new ArgumentException("Path cannot be null or whitespace.", nameof(path));
    _path = path;
    return this;
  }

  /// <summary>Materialise the <see cref="IItem{T}"/>.</summary>
  public IItem<T> Build()
  {
    if (_path is null)
      throw new InvalidOperationException(
        $"Json item '{_label}' requires AtPath(...) before Build()."
      );
    return new Item<T>(_label, new SingletonJsonAdapter<T>(_path));
  }
}

/// <summary>Tier-1 builder for an array-JSON catalog item.</summary>
public sealed class JsonArrayBuilder<TRow> where TRow : notnull, IStructuredSerializable
{
  private readonly string _label;
  private string? _path;
  private IStorageMediumResolver? _resolver;

  internal JsonArrayBuilder(string label)
  {
    _label = label;
  }

  /// <summary>
  /// Set the path or URI for this JSON file. Bare paths and
  /// <c>file://</c> URIs always resolve to local filesystem; other
  /// schemes (e.g. <c>https://</c>) require a corresponding
  /// <see cref="IStorageMediumResolver"/> via
  /// <see cref="WithResolver(IStorageMediumResolver)"/>.
  /// </summary>
  public JsonArrayBuilder<TRow> AtPath(string path)
  {
    if (string.IsNullOrWhiteSpace(path))
      throw new ArgumentException("Path cannot be null or whitespace.", nameof(path));
    _path = path;
    return this;
  }

  /// <summary>
  /// Provide an optional <see cref="IStorageMediumResolver"/> for
  /// non-filesystem schemes (HTTP, etc.). When omitted, falls back
  /// to <see cref="StorageMediumResolver.Filesystem"/>.
  /// </summary>
  public JsonArrayBuilder<TRow> WithResolver(IStorageMediumResolver resolver)
  {
    _resolver = resolver;
    return this;
  }

  /// <summary>Materialise the <see cref="IItem{T}"/>.</summary>
  public IItem<IEnumerable<TRow>> Build()
  {
    if (_path is null)
      throw new InvalidOperationException(
        $"JsonArray item '{_label}' requires AtPath(...) before Build()."
      );
    var medium = (_resolver ?? StorageMediumResolver.Filesystem).Resolve(_path);
    return new Item<IEnumerable<TRow>>(
      _label,
      new ComposedStorageAdapter<IEnumerable<TRow>, TRow>(
        medium,
        new JsonFormatSerializer<TRow>(),
        new EnumerableContainerAdapter<TRow>()
      )
    );
  }
}
