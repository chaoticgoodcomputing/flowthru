using System.Security.Cryptography;
using System.Text;
using Flowthru.Validation.Runtime;

namespace Flowthru.Data.Storage;

/// <summary>
/// Storage medium for file-based I/O. Reads and writes raw byte streams
/// to a file path; performs atomic writes via temp file plus rename.
/// </summary>
/// <remarks>
/// Thread-safe for reads. Concurrent writes to the same path should be
/// coordinated externally.
/// </remarks>
public sealed class FileStorageMedium : IStorageMedium, ISupportsFingerprint
{
  private readonly string _filePath;

  public FileStorageMedium(string filePath)
  {
    if (string.IsNullOrWhiteSpace(filePath))
    {
      throw new ArgumentException("File path cannot be null or whitespace", nameof(filePath));
    }
    _filePath = filePath;
  }

  /// <summary>The file path this medium reads/writes.</summary>
  public string FilePath => _filePath;

  /// <inheritdoc/>
  public StorageTraits Traits => new();

  /// <inheritdoc/>
  public FlowIO<Stream> ReadStream() =>
    FlowIO.LiftAsync<Stream>(ct =>
    {
      if (!File.Exists(_filePath))
      {
        throw new FileNotFoundException($"File not found at path: {_filePath}", _filePath);
      }

      Stream stream = new FileStream(
        _filePath,
        FileMode.Open,
        FileAccess.Read,
        FileShare.Read,
        bufferSize: 4096,
        useAsync: true
      );
      return Task.FromResult(stream);
    });

  /// <inheritdoc/>
  public FlowIO<FlowUnit> WriteStream(Stream stream) =>
    FlowIO.LiftAsync(async ct =>
    {
      if (stream is null)
      {
        throw new ArgumentNullException(nameof(stream));
      }

      var directory = Path.GetDirectoryName(_filePath);
      if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
      {
        Directory.CreateDirectory(directory);
      }

      // Write to temp file first, then atomic rename — avoids partial
      // writes on failure.
      var tempPath = $"{_filePath}.tmp.{Guid.NewGuid():N}";
      try
      {
        await using (
          var fileStream = new FileStream(
            tempPath,
            FileMode.Create,
            FileAccess.Write,
            FileShare.None,
            bufferSize: 4096,
            useAsync: true
          )
        )
        {
          await stream.CopyToAsync(fileStream, ct).ConfigureAwait(false);
          await fileStream.FlushAsync(ct).ConfigureAwait(false);
        }
        File.Move(tempPath, _filePath, overwrite: true);
        return FlowUnit.Default;
      }
      catch
      {
        if (File.Exists(tempPath))
        {
          try
          {
            File.Delete(tempPath);
          }
          catch
          {
            // Cleanup failure is non-fatal.
          }
        }
        throw;
      }
    });

  /// <inheritdoc/>
  public FlowIO<bool> Exists() => FlowIO.Lift(() => File.Exists(_filePath));

  /// <inheritdoc/>
  public FlowIO<ValidationResult> InspectTarget() =>
    FlowIO.LiftAsync(ct => LocalFileWriteProbe.ProbeAsync(_filePath, ct));

  /// <inheritdoc/>
  /// <remarks>
  /// <para>
  /// Derives the fingerprint from cheap filesystem metadata
  /// (last-write-time-utc-ticks and file length). The result is a
  /// stable, content-sensitive identity for typical edit patterns —
  /// any save through Flowthru rewrites the file and updates both
  /// values.
  /// </para>
  /// <para>
  /// <strong>Documented limitation.</strong> In-place byte edits
  /// that preserve both <c>mtime</c> and <c>size</c> produce a
  /// stale fingerprint and a false cache hit. This is the
  /// acceptable cost of metadata-only fingerprinting (the
  /// alternative — hashing file contents — defeats the
  /// "cheap" requirement). A future "deep fingerprint" variant
  /// could opt into content hashing; for now, users who require
  /// exact-content guarantees should wrap the file in such a
  /// variant.
  /// </para>
  /// <para>
  /// When the file is missing, the fingerprint surfaces through the
  /// FlowIO failure channel rather than synthesising an empty value
  /// — callers treat it as "fingerprint unknown" and the
  /// dependent step as a cache miss.
  /// </para>
  /// </remarks>
  public FlowIO<string> Fingerprint() =>
    FlowIO.Lift(
      () =>
      {
        if (!File.Exists(_filePath))
        {
          throw new FileNotFoundException(
            $"Cannot fingerprint file '{_filePath}': file does not exist.",
            _filePath
          );
        }
        var info = new FileInfo(_filePath);
        var payload = $"{info.LastWriteTimeUtc.Ticks}:{info.Length}";
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(payload));
        return Convert.ToHexString(bytes).ToLowerInvariant();
      },
      source: $"FileStorageMedium.Fingerprint[{_filePath}]"
    );
}
