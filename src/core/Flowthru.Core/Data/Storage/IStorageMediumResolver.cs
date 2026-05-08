namespace Flowthru.Data.Storage;

/// <summary>
/// Dispatch surface that turns a user-supplied path-or-URI string
/// into a concrete <see cref="IStorageMedium"/>. Format-extension
/// smart constructors accept an optional resolver so users can
/// declare a catalog item with the same signature regardless of
/// whether the source lives on disk, on HTTP, on S3, or anywhere
/// else a registered provider can reach.
/// </summary>
/// <remarks>
/// <para>
/// Bare paths (e.g. <c>/data/file.csv</c>, <c>C:\data\file.csv</c>)
/// and <c>file://</c> URIs always resolve to a
/// <see cref="FileStorageMedium"/>. URIs with non-file schemes are
/// dispatched to the first registered
/// <see cref="IStorageMediumProvider"/> whose
/// <see cref="IStorageMediumProvider.CanHandle(Uri)"/> returns true.
/// </para>
/// <para>
/// <strong>Filesystem-only default.</strong> Format extensions accept
/// the resolver as nullable; when omitted they fall back to
/// <see cref="StorageMediumResolver.Filesystem"/>, a singleton that
/// only knows how to construct file-backed mediums. This keeps the
/// existing <c>Csv&lt;T&gt;("label", "/data/file.csv")</c> ergonomics
/// for catalogs that don't need network mediums.
/// </para>
/// </remarks>
public interface IStorageMediumResolver
{
  /// <summary>
  /// Resolve a path-or-URI string to the appropriate
  /// <see cref="IStorageMedium"/>. Throws when the URI scheme has no
  /// registered provider — the diagnostic names the scheme and
  /// suggests the relevant <c>UseXxx()</c> registration.
  /// </summary>
  IStorageMedium Resolve(string pathOrUri);
}
