namespace Flowthru.Data.Storage;

/// <summary>
/// Storage medium for file-based I/O. Reads and writes raw byte streams
/// to a file path; performs atomic writes via temp file plus rename.
/// </summary>
/// <remarks>
/// Thread-safe for reads. Concurrent writes to the same path should be
/// coordinated externally.
/// </remarks>
public sealed class FileStorageMedium : IStorageMedium
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
}
