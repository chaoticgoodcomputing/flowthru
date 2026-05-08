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
/// </remarks>
public sealed class StorageMediumResolver : IStorageMediumResolver
{
  private static readonly FileStorageMediumProvider _fileProvider = new();

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
}
