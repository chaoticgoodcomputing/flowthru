namespace Flowthru.Data.Storage;

/// <summary>
/// Makes a forward-only byte source seekable <em>without</em> holding it
/// entirely in RAM. Seek-required formats (Parquet, Excel) need random access —
/// the footer / workbook directory lives at the end of the object — but an
/// S3 / HTTP response body is forward-only. Rather than buffer the whole object
/// into a <see cref="MemoryStream"/> (O(object) RAM), spill it to a bounded temp
/// file (O(object) disk, O(buffer) RAM) and hand back a seekable
/// <see cref="FileStream"/>. Already-seekable sources pass through untouched.
/// </summary>
/// <remarks>
/// This is the shared "make-seekable" primitive (ADR-0023) so Parquet and Excel
/// don't each carry their own copy/seek/dispose logic. The temp file is created
/// with <see cref="FileOptions.DeleteOnClose"/>, so <see cref="DisposeAsync"/>
/// deletes it — and the lifetime is owned by the caller's <c>await using</c>,
/// which for a streaming read is the <c>FlowSource</c> bracket.
/// </remarks>
public sealed class SeekableSpill : IAsyncDisposable
{
  private readonly bool _ownsStream;

  private SeekableSpill(Stream stream, bool ownsStream)
  {
    Stream = stream;
    _ownsStream = ownsStream;
  }

  /// <summary>The seekable stream, positioned at the start.</summary>
  public Stream Stream { get; }

  /// <summary>
  /// Returns a seekable view of <paramref name="source"/>. If it is already
  /// seekable it is returned as-is (ownership stays with the caller, so
  /// <see cref="DisposeAsync"/> leaves it open). Otherwise the source is spilled
  /// to a temp file (auto-deleted on dispose) and a <see cref="FileStream"/> is
  /// returned.
  /// </summary>
  public static async ValueTask<SeekableSpill> CreateAsync(
    Stream source,
    CancellationToken cancellationToken = default
  )
  {
    if (source is null) throw new ArgumentNullException(nameof(source));

    if (source.CanSeek)
    {
      source.Position = 0;
      return new SeekableSpill(source, ownsStream: false);
    }

    var file = new FileStream(
      Path.GetTempFileName(),
      FileMode.Create,
      FileAccess.ReadWrite,
      FileShare.None,
      bufferSize: 81920,
      FileOptions.Asynchronous | FileOptions.DeleteOnClose
    );
    try
    {
      await source.CopyToAsync(file, cancellationToken).ConfigureAwait(false);
      file.Position = 0;
    }
    catch
    {
      await file.DisposeAsync().ConfigureAwait(false); // DeleteOnClose removes the temp file
      throw;
    }

    return new SeekableSpill(file, ownsStream: true);
  }

  /// <summary>
  /// Disposes the spilled temp file (when this spill owns one). A pass-through
  /// of an already-seekable caller stream is left open — the caller owns it.
  /// </summary>
  public async ValueTask DisposeAsync()
  {
    if (_ownsStream)
    {
      await Stream.DisposeAsync().ConfigureAwait(false);
    }
  }
}
