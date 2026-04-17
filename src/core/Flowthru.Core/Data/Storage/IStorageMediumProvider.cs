namespace Flowthru.Core.Data.Storage;

/// <summary>
/// Factory for creating <see cref="IStorageMedium"/> instances for a specific URI scheme.
/// </summary>
/// <remarks>
/// <para>
/// Implement this interface to add support for a new remote storage scheme (e.g., "sftp", "s3").
/// Providers are selected by scheme when <see cref="StorageMediumResolver"/> dispatches a URI.
/// </para>
/// <para>
/// <strong>Registration:</strong>
/// </para>
/// <list type="bullet">
/// <item>
/// <strong>DI-based (recommended):</strong> Register as <c>IStorageMediumProvider</c>
/// singleton via an extension's <c>Use*()</c> builder method. The
/// <see cref="StorageMediumResolver"/> collects all registered providers automatically.
/// </item>
/// <item>
/// <strong>Direct construction:</strong> Pass to
/// <see cref="StorageMediumResolver.Register"/> when building a resolver manually
/// outside the DI container.
/// </item>
/// </list>
/// <para>
/// <strong>Example (SFTP provider):</strong>
/// </para>
/// <code>
/// public sealed class SftpStorageMediumProvider : IStorageMediumProvider
/// {
///     private readonly SftpOptions _options;
///
///     public SftpStorageMediumProvider(SftpOptions options) => _options = options;
///
///     public bool CanHandle(Uri uri) => uri.Scheme == "sftp";
///
///     public IStorageMedium Create(Uri uri) =>
///         new SftpStorageMedium(uri, _options);
/// }
/// </code>
/// </remarks>
public interface IStorageMediumProvider
{
  /// <summary>
  /// Returns <c>true</c> if this provider can handle the given URI.
  /// </summary>
  /// <param name="uri">The parsed URI from a catalog entry's path string.</param>
  bool CanHandle(Uri uri);

  /// <summary>
  /// Creates a storage medium for the given URI.
  /// </summary>
  /// <param name="uri">The parsed URI from a catalog entry's path string.</param>
  IStorageMedium Create(Uri uri);
}
