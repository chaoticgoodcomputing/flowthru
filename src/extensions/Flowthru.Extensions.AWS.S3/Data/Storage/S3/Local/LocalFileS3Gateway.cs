using System.Security.Cryptography;

namespace Flowthru.Data.Storage.S3.Local;

/// <summary>
/// A local-development <see cref="IS3Gateway"/> backed by the filesystem — a
/// fully offline stand-in for S3 with no AWS account, no credentials, and no
/// network. Each object maps to a file at <c>{root}/{bucket}/{key}</c>, so the
/// directory tree is an inspectable record of the "bucket" contents.
/// </summary>
/// <remarks>
/// <para>
/// <strong>The filesystem is the store.</strong> Unlike a tabular backend, an
/// object store is already byte-stream shaped, so this stub needs no separate
/// in-memory model — reads, writes, existence checks, and deletes are direct
/// file operations rooted under one directory. Writes go through a temp file and
/// an atomic rename, mirroring <c>FileStorageMedium</c>, so a crashed write never
/// leaves a partial object.
/// </para>
/// <para>
/// <strong>Keys are confined to the root.</strong> A key resolving outside the
/// root directory (e.g. via <c>..</c> segments) is rejected — the stub will not
/// read or write outside its own tree.
/// </para>
/// <para>
/// <strong>ETag is a content hash.</strong> <see cref="GetETag"/> returns the
/// SHA-256 of the object bytes, so it is genuinely content-sensitive (AWS uses
/// MD5 for single-part objects; the digest algorithm is an implementation
/// detail the fingerprint contract does not constrain).
/// </para>
/// <para>
/// <strong>Single-process, no locking.</strong> Meant for local development,
/// demos, and tests — not shared or production storage. For that, use
/// <c>UseS3()</c> over the AWS-backed gateway.
/// </para>
/// <para>
/// <strong>Reads are forward-only, on purpose.</strong> <see cref="GetObject"/>
/// returns a non-seekable stream even though it is backed by a local file. Real
/// S3 (<c>AmazonS3Gateway</c>) hands back a forward-only response body, and the
/// <see cref="IS3Gateway"/> contract never promised seekability. Modelling that
/// faithfully means a format that needs random access (Parquet, Excel) exercises
/// its buffering path against this stub exactly as it would against S3 — so the
/// "seekable stub hides a seek-required-format bug" failure cannot slip past
/// offline tests or local development.
/// </para>
/// </remarks>
public sealed class LocalFileS3Gateway : IS3Gateway
{
  private readonly string _root;

  /// <summary>
  /// Build a gateway rooted at <paramref name="rootDirectory"/>. The directory
  /// is created on first write if it does not yet exist.
  /// </summary>
  public LocalFileS3Gateway(string rootDirectory)
  {
    if (string.IsNullOrWhiteSpace(rootDirectory))
    {
      throw new ArgumentException("Root directory cannot be null or whitespace.", nameof(rootDirectory));
    }
    _root = Path.GetFullPath(rootDirectory);
  }

  /// <inheritdoc/>
  /// <remarks>
  /// The returned stream is <strong>forward-only</strong> (<c>CanSeek == false</c>),
  /// matching the AWS gateway's response body. See the type-level remarks for why
  /// the stub deliberately withholds seekability the backing file would otherwise
  /// allow.
  /// </remarks>
  public Task<Stream> GetObject(string bucket, string key, CancellationToken ct)
  {
    ct.ThrowIfCancellationRequested();
    var path = ResolvePath(bucket, key);
    if (!File.Exists(path))
    {
      throw new FileNotFoundException($"No object at s3://{bucket}/{key}.", path);
    }
    var file = new FileStream(
      path, FileMode.Open, FileAccess.Read, FileShare.Read,
      bufferSize: 4096, useAsync: true);
    return Task.FromResult<Stream>(new ForwardOnlyStream(file));
  }

  /// <inheritdoc/>
  public async Task PutObject(string bucket, string key, Stream content, CancellationToken ct)
  {
    if (content is null) throw new ArgumentNullException(nameof(content));
    var path = ResolvePath(bucket, key);

    var directory = Path.GetDirectoryName(path);
    if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
    {
      Directory.CreateDirectory(directory);
    }

    // Temp file + atomic rename — no partial object is ever observable.
    var tempPath = $"{path}.tmp.{Guid.NewGuid():N}";
    try
    {
      await using (var fileStream = new FileStream(
        tempPath, FileMode.Create, FileAccess.Write, FileShare.None,
        bufferSize: 4096, useAsync: true))
      {
        await content.CopyToAsync(fileStream, ct).ConfigureAwait(false);
        await fileStream.FlushAsync(ct).ConfigureAwait(false);
      }
      File.Move(tempPath, path, overwrite: true);
    }
    catch
    {
      if (File.Exists(tempPath))
      {
        try { File.Delete(tempPath); } catch { /* cleanup is non-fatal */ }
      }
      throw;
    }
  }

  /// <inheritdoc/>
  public Task<bool> ObjectExists(string bucket, string key, CancellationToken ct)
  {
    ct.ThrowIfCancellationRequested();
    return Task.FromResult(File.Exists(ResolvePath(bucket, key)));
  }

  /// <inheritdoc/>
  public Task DeleteObject(string bucket, string key, CancellationToken ct)
  {
    ct.ThrowIfCancellationRequested();
    var path = ResolvePath(bucket, key);
    if (File.Exists(path))
    {
      File.Delete(path);
    }
    return Task.CompletedTask;
  }

  /// <inheritdoc/>
  public async Task<string?> GetETag(string bucket, string key, CancellationToken ct)
  {
    var path = ResolvePath(bucket, key);
    if (!File.Exists(path)) return null;

    await using var stream = new FileStream(
      path, FileMode.Open, FileAccess.Read, FileShare.Read,
      bufferSize: 4096, useAsync: true);
    var hash = await SHA256.HashDataAsync(stream, ct).ConfigureAwait(false);
    return Convert.ToHexString(hash).ToLowerInvariant();
  }

  /// <inheritdoc/>
  /// <remarks>
  /// The stub's honest answer is the backing file itself: the object
  /// <em>is</em> a local file at <c>{root}/{bucket}/{key}</c>, so a consumer
  /// reading the store natively gets a direct path with no access handoff to
  /// interpret. Absence is not an error — the location is where the object's
  /// bytes live or would land.
  /// </remarks>
  public Task<ByteLocation> LocateObject(string bucket, string key, CancellationToken ct)
  {
    ct.ThrowIfCancellationRequested();
    return Task.FromResult<ByteLocation>(new ByteLocation.LocalFile(ResolvePath(bucket, key)));
  }

  // Map (bucket, key) to a path under the root, rejecting any key that escapes
  // it. Key segments are split on '/' so an S3 key maps to a nested path on any
  // platform.
  private string ResolvePath(string bucket, string key)
  {
    if (string.IsNullOrWhiteSpace(bucket))
    {
      throw new ArgumentException("Bucket cannot be null or whitespace.", nameof(bucket));
    }

    var segments = new List<string> { _root, bucket };
    segments.AddRange((key ?? string.Empty).Split('/', StringSplitOptions.RemoveEmptyEntries));
    var combined = Path.GetFullPath(Path.Combine(segments.ToArray()));

    var rootWithSep = _root.EndsWith(Path.DirectorySeparatorChar)
      ? _root
      : _root + Path.DirectorySeparatorChar;
    if (!combined.StartsWith(rootWithSep, StringComparison.Ordinal))
    {
      throw new ArgumentException(
        $"Key '{key}' on bucket '{bucket}' resolves outside the gateway root.", nameof(key));
    }
    return combined;
  }

  /// <summary>
  /// Read-only, forward-only view over the backing <see cref="FileStream"/>:
  /// delegates reads, reports <see cref="CanSeek"/> as <c>false</c>, and refuses
  /// every seek/length/write operation. Owns the inner stream and disposes it.
  /// This is what makes the stub model real S3's non-seekable response body
  /// rather than the seekable file underneath it.
  /// </summary>
  private sealed class ForwardOnlyStream(Stream inner) : Stream
  {
    public override bool CanRead => inner.CanRead;
    public override bool CanSeek => false;
    public override bool CanWrite => false;
    public override long Length => throw new NotSupportedException("S3 object reads are forward-only.");
    public override long Position
    {
      get => throw new NotSupportedException("S3 object reads are forward-only.");
      set => throw new NotSupportedException("S3 object reads are forward-only.");
    }

    public override int Read(byte[] buffer, int offset, int count) =>
      inner.Read(buffer, offset, count);

    public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken ct) =>
      inner.ReadAsync(buffer, offset, count, ct);

    public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken ct = default) =>
      inner.ReadAsync(buffer, ct);

    public override void Flush() => inner.Flush();

    public override long Seek(long offset, SeekOrigin origin) =>
      throw new NotSupportedException("S3 object reads are forward-only.");

    public override void SetLength(long value) => throw new NotSupportedException();

    public override void Write(byte[] buffer, int offset, int count) =>
      throw new NotSupportedException("S3 object reads are read-only.");

    protected override void Dispose(bool disposing)
    {
      if (disposing)
      {
        inner.Dispose();
      }
      base.Dispose(disposing);
    }

    public override async ValueTask DisposeAsync()
    {
      await inner.DisposeAsync().ConfigureAwait(false);
      await base.DisposeAsync().ConfigureAwait(false);
    }
  }
}
