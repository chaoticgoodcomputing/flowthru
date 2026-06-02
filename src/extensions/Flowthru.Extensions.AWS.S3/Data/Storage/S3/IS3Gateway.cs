namespace Flowthru.Data.Storage.S3;

/// <summary>
/// The narrow seam the S3 storage medium calls — the only object operations a
/// read, write, existence check, write-probe, or fingerprint needs. Speaks a
/// neutral <c>(bucket, key)</c> + <see cref="Stream"/> vocabulary; no AWS SDK
/// type appears on this surface, so the shipped file-backed stub
/// (<see cref="Local.LocalFileS3Gateway"/>) can satisfy it with no
/// <c>AWSSDK.S3</c> reference, and tests run fully offline.
/// </summary>
/// <remarks>
/// <para>
/// Methods are asynchronous and may throw on backend failure — the medium lifts
/// each call into <c>FlowIO</c> so the failure becomes a value, never a thrown
/// exception escaping to the user. The seam itself stays minimal and
/// exception-based.
/// </para>
/// <para>
/// A gateway instance is bound to one set of credentials / one endpoint (one
/// catalog). Crossing AWS accounts or endpoints is two catalogs with two
/// gateways.
/// </para>
/// </remarks>
public interface IS3Gateway
{
  /// <summary>
  /// Open the object at <paramref name="bucket"/>/<paramref name="key"/> for
  /// reading. The returned stream is positioned at the beginning; the caller
  /// disposes it.
  /// </summary>
  /// <exception cref="System.IO.FileNotFoundException">
  /// No object exists at the key. (Production gateways translate the backend's
  /// not-found response to this; the medium lifts it into a <c>FlowIO</c>
  /// failure.)
  /// </exception>
  Task<Stream> GetObject(string bucket, string key, CancellationToken ct);

  /// <summary>
  /// Write <paramref name="content"/> to <paramref name="bucket"/>/<paramref name="key"/>,
  /// replacing any existing object. The write is all-or-nothing: a partially
  /// written object is never observable (S3 single-object PUT is atomic; the
  /// local stub writes to a temp file and renames).
  /// </summary>
  Task PutObject(string bucket, string key, Stream content, CancellationToken ct);

  /// <summary>
  /// True if an object exists at <paramref name="bucket"/>/<paramref name="key"/>.
  /// A missing object returns <c>false</c> rather than throwing, so the medium's
  /// <c>Exists()</c> distinguishes a seed input from a pipeline output without a
  /// failure value.
  /// </summary>
  Task<bool> ObjectExists(string bucket, string key, CancellationToken ct);

  /// <summary>
  /// Delete the object at <paramref name="bucket"/>/<paramref name="key"/>.
  /// Idempotent — deleting an absent object is a successful no-op. Used to clean
  /// up the write-probe sentinel.
  /// </summary>
  Task DeleteObject(string bucket, string key, CancellationToken ct);

  /// <summary>
  /// The current entity tag for <paramref name="bucket"/>/<paramref name="key"/>,
  /// or <see langword="null"/> when no object exists (or the backend exposes no
  /// validator). A stable, content-sensitive, cheap-to-derive identity — the
  /// fingerprint source the cache plan consumes. Implementations must not stream
  /// the object body to compute it.
  /// </summary>
  Task<string?> GetETag(string bucket, string key, CancellationToken ct);
}
