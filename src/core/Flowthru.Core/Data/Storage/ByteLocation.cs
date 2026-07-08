namespace Flowthru.Data.Storage;

/// <summary>
/// Where an item's bytes live, addressed for a consumer that reaches the
/// storage <em>natively</em> (an embedded engine pointed at a file or
/// object, a bulk copier) rather than through
/// <see cref="IStorageAdapter{T}.Load"/>. Closed sum: the bytes are either
/// a file on the local filesystem (<see cref="LocalFile"/>) or an object
/// behind a remote URI plus the access handoff needed to reach it
/// (<see cref="RemoteUri"/>).
/// </summary>
/// <remarks>
/// <para>
/// The hierarchy is closed via the private constructor — no derived case
/// can be added outside this file. Pattern-match exhaustively; new cases
/// added here will surface as compile diagnostics at every consumer until
/// handled.
/// </para>
/// <para>
/// A location is obtained via
/// <see cref="ISupportsByteLocation.LocateBytes"/> and describes where the
/// bytes live <em>or would land</em> — a write target is addressable
/// before anything has been written, so existence stays
/// <see cref="IStorageAdapter{T}.Exists"/>'s question. Access material in
/// <see cref="RemoteUri.Access"/> is resolved when the locating effect
/// runs; it is never stored in a catalog or carried by the DAG.
/// </para>
/// </remarks>
public abstract record ByteLocation
{
  private ByteLocation() { }

  /// <summary>
  /// The bytes are a file on the local filesystem at the absolute
  /// <see cref="Path"/>. A native consumer opens the path directly; no
  /// access handoff is required.
  /// </summary>
  public sealed record LocalFile(string Path) : ByteLocation;

  /// <summary>
  /// The bytes are an object behind <see cref="Uri"/> — e.g.
  /// <c>s3://bucket/key</c> — reachable with the accompanying
  /// <see cref="Access"/> handoff.
  /// </summary>
  /// <param name="Uri">The object's URI; the scheme tells a consumer which access vocabulary applies.</param>
  /// <param name="Access">
  /// Access material a native consumer needs to reach <paramref name="Uri"/>
  /// — endpoint, region, credential entries — keyed by names the producing
  /// medium documents. Minted at locate time by the medium's gateway;
  /// empty when the endpoint needs none.
  /// </param>
  public sealed record RemoteUri(Uri Uri, IReadOnlyDictionary<string, string> Access) : ByteLocation;

  /// <summary>
  /// Terminal pattern match. Use this to consume a ByteLocation at the
  /// boundary where you must collapse the sum into a single result type.
  /// </summary>
  public TResult Match<TResult>(
    Func<LocalFile, TResult> onLocalFile,
    Func<RemoteUri, TResult> onRemoteUri
  ) =>
    this switch
    {
      LocalFile local => onLocalFile(local),
      RemoteUri remote => onRemoteUri(remote),
      _ => throw new InvalidOperationException("Unreachable: ByteLocation is a closed sum"),
    };
}
