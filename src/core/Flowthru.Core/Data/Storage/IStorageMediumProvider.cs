namespace Flowthru.Data.Storage;

/// <summary>
/// Plug-in registration for <see cref="IStorageMedium"/> implementations
/// that respond to specific URI schemes. Extensions register concrete
/// providers (HTTP, S3, GCS, FTP) with the host's DI container; the
/// host-resolved <see cref="IStorageMediumResolver"/> dispatches a
/// path-or-URI string to the first provider whose
/// <see cref="CanHandle(Uri)"/> returns true.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Why this seam exists.</strong> The format × medium ×
/// container composition (§2.3) is structurally three independent
/// axes. Format extensions like Csv / Excel / Parquet / Json / Xml
/// deliberately do not depend on a concrete medium — they accept any
/// <see cref="IStorageMedium"/>. The resolver is the dispatch layer
/// that turns a user-supplied path-or-URI into the right medium
/// without requiring each format extension to know about every
/// medium extension. Adding S3 or FTP support is a single
/// <see cref="IStorageMediumProvider"/> implementation plus a DI
/// registration; no format extension or Core change required.
/// </para>
/// <para>
/// <strong>Bare paths and the file scheme.</strong> The resolver
/// always falls back to <see cref="FileStorageMediumProvider"/> for
/// bare paths and <c>file://</c> URIs, so extensions never need to
/// claim those.
/// </para>
/// </remarks>
public interface IStorageMediumProvider
{
  /// <summary>
  /// True if this provider can construct a medium for the supplied
  /// URI. Typically inspects <see cref="Uri.Scheme"/>.
  /// </summary>
  bool CanHandle(Uri uri);

  /// <summary>
  /// Construct a medium for the supplied URI. The caller has already
  /// verified <see cref="CanHandle(Uri)"/> returns true.
  /// </summary>
  IStorageMedium Create(Uri uri);
}
