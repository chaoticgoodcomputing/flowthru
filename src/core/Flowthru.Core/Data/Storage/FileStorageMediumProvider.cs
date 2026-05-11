namespace Flowthru.Data.Storage;

/// <summary>
/// <see cref="IStorageMediumProvider"/> for the <c>file://</c> scheme
/// — the always-available fallback. The
/// <see cref="StorageMediumResolver"/> uses this provider implicitly
/// for bare paths (no scheme) and explicitly for <c>file://</c> URIs;
/// extensions never need to register it themselves.
/// </summary>
public sealed class FileStorageMediumProvider : IStorageMediumProvider
{
  /// <inheritdoc/>
  public bool CanHandle(Uri uri) => uri.IsFile;

  /// <inheritdoc/>
  public IStorageMedium Create(Uri uri) => new FileStorageMedium(uri.LocalPath);
}
