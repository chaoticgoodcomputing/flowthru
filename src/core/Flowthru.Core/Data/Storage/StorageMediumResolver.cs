namespace Flowthru.Data.Storage;

/// <summary>
/// Default <see cref="IStorageMediumResolver"/> implementation —
/// composes a list of <see cref="IStorageMediumProvider"/>s (typically
/// supplied by DI from extension registrations) with a built-in
/// fallback to <see cref="FileStorageMediumProvider"/> for bare paths
/// and <c>file://</c> URIs.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Dispatch order.</strong> URI schemes are tried against
/// registered providers in registration order; first match wins.
/// Bare paths (and <c>file://</c> URIs) bypass provider dispatch and
/// resolve directly via the built-in
/// <see cref="FileStorageMediumProvider"/>.
/// </para>
/// <para>
/// <strong>Filesystem-only fallback.</strong>
/// <see cref="Filesystem"/> exposes a singleton resolver constructed
/// with no extra providers — used by format extensions when their
/// caller passes <c>null</c> for the resolver argument.
/// </para>
/// <para>
/// <strong>Ambient resolver slot.</strong> <see cref="Current"/> exposes
/// the resolver currently in scope on this async-flow, set by
/// <see cref="PushAmbient(IStorageMediumResolver)"/> at catalog
/// materialization time. Catalog item builders consult this slot when
/// no explicit <c>.WithResolver(...)</c> is supplied, so end-users do
/// not need to thread a resolver through every catalog property.
/// </para>
/// </remarks>
public sealed class StorageMediumResolver : IStorageMediumResolver
{
  private static readonly FileStorageMediumProvider _fileProvider = new();
  private static readonly System.Threading.AsyncLocal<IStorageMediumResolver?> _ambient = new();

  private readonly IReadOnlyList<IStorageMediumProvider> _providers;

  public StorageMediumResolver(IEnumerable<IStorageMediumProvider> providers)
  {
    if (providers is null) throw new ArgumentNullException(nameof(providers));
    _providers = providers.ToList();
  }

  /// <summary>
  /// Singleton resolver with no registered providers — only resolves
  /// bare paths and <c>file://</c> URIs. Used as the default fallback
  /// when format-extension factories receive a null resolver
  /// argument.
  /// </summary>
  public static IStorageMediumResolver Filesystem { get; } =
    new StorageMediumResolver(Array.Empty<IStorageMediumProvider>());

  /// <summary>
  /// The <see cref="IStorageMediumResolver"/> currently in scope on the
  /// active async-flow, or <c>null</c> if no ambient resolver has been
  /// pushed. Catalog item builders read this value when no explicit
  /// resolver is supplied to <c>.WithResolver(...)</c>.
  /// </summary>
  /// <remarks>
  /// The slot is backed by <see cref="System.Threading.AsyncLocal{T}"/>,
  /// so nested <see cref="PushAmbient(IStorageMediumResolver)"/> scopes
  /// observe their immediate parent on dispose.
  /// </remarks>
  public static IStorageMediumResolver? Current => _ambient.Value;

  /// <summary>
  /// Push <paramref name="resolver"/> onto the ambient slot for the
  /// active async-flow. Returns an <see cref="IDisposable"/> that
  /// restores the previous value on dispose. Catalog implementations
  /// wrap their factory invocations with this so format builders inside
  /// the closure observe the resolver without ceremony.
  /// </summary>
  /// <param name="resolver">
  /// The resolver to install. Passing <c>null</c> is supported and
  /// effectively clears the slot for the scope (useful when a nested
  /// catalog wants to "opt out" of an outer scope's resolver).
  /// </param>
  public static IDisposable PushAmbient(IStorageMediumResolver? resolver)
  {
    var previous = _ambient.Value;
    _ambient.Value = resolver;
    return new AmbientScope(previous);
  }

  /// <inheritdoc/>
  public IStorageMedium Resolve(string pathOrUri)
  {
    if (string.IsNullOrWhiteSpace(pathOrUri))
    {
      throw new ArgumentException(
        "Path or URI must be a non-empty string.", nameof(pathOrUri)
      );
    }

    // Try to parse as an absolute URI. Bare paths (relative or
    // platform-rooted like /foo or C:\foo) fall through to file
    // medium without going through the provider list.
    if (Uri.TryCreate(pathOrUri, UriKind.Absolute, out var uri)
        && !string.IsNullOrEmpty(uri.Scheme)
        && !uri.IsFile)
    {
      foreach (var provider in _providers)
      {
        if (provider.CanHandle(uri)) return provider.Create(uri);
      }

      throw new InvalidOperationException(
        $"No IStorageMediumProvider is registered for URI scheme '{uri.Scheme}://'. "
        + $"Either register the corresponding extension (e.g. builder.UseHttp() for "
        + $"http/https) or pass a bare file path. Got: '{pathOrUri}'."
      );
    }

    // Bare path or file:// — resolve via the built-in file provider.
    return uri is { IsFile: true }
      ? _fileProvider.Create(uri)
      : new FileStorageMedium(pathOrUri);
  }

  private sealed class AmbientScope : IDisposable
  {
    private readonly IStorageMediumResolver? _previous;
    private bool _disposed;

    public AmbientScope(IStorageMediumResolver? previous)
    {
      _previous = previous;
    }

    public void Dispose()
    {
      if (_disposed) return;
      _disposed = true;
      _ambient.Value = _previous;
    }
  }
}
