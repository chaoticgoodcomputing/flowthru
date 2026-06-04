namespace Flowthru.Tests.Kits.Storage;

/// <summary>
/// Forward-only, read-only view over an inner stream: delegates reads but
/// reports <see cref="CanSeek"/> as <c>false</c> and throws on every
/// seek/length/position/write operation.
/// </summary>
/// <remarks>
/// <para>
/// Models the one property of a real S3 or HTTP response body that a seekable
/// test stand-in (a <see cref="MemoryStream"/> or <see cref="System.IO.FileStream"/>)
/// silently papers over: non-seekability. Wrapping a fixture stream in this lets a
/// test exercise the same forward-only path production would hit — the gap that let
/// the Parquet/S3 seek bug pass CI (issue #105).
/// </para>
/// <para>
/// The wrapper does <strong>not</strong> dispose the inner stream — the caller that
/// created the inner stream owns its lifetime (typically via a <c>using</c>).
/// </para>
/// </remarks>
public sealed class NonSeekableStream(Stream inner) : Stream
{
  private readonly Stream _inner = inner ?? throw new ArgumentNullException(nameof(inner));

  public override bool CanRead => _inner.CanRead;
  public override bool CanSeek => false;
  public override bool CanWrite => false;
  public override long Length => throw new NotSupportedException("Stream is forward-only.");
  public override long Position
  {
    get => throw new NotSupportedException("Stream is forward-only.");
    set => throw new NotSupportedException("Stream is forward-only.");
  }

  public override int Read(byte[] buffer, int offset, int count) =>
    _inner.Read(buffer, offset, count);

  public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken ct) =>
    _inner.ReadAsync(buffer, offset, count, ct);

  public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken ct = default) =>
    _inner.ReadAsync(buffer, ct);

  public override void Flush() => _inner.Flush();

  public override long Seek(long offset, SeekOrigin origin) =>
    throw new NotSupportedException("Stream is forward-only.");

  public override void SetLength(long value) => throw new NotSupportedException();

  public override void Write(byte[] buffer, int offset, int count) =>
    throw new NotSupportedException("Stream is read-only.");
}
