using Flowthru.Core.Data.Capabilities;
using Flowthru.Core.Effects;

namespace Flowthru.Core.Data.Storage.Medium;

/// <summary>
/// Storage medium for file-based I/O operations.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Responsibility:</strong> Handle reading and writing raw byte streams to/from files.
/// </para>
/// <para>
/// <strong>Features:</strong>
/// </para>
/// <list type="bullet">
/// <item>Automatic directory creation for parent paths</item>
/// <item>Atomic writes via temp file + rename</item>
/// <item>Support for both absolute and relative paths</item>
/// <item>All storage traits use filesystem baseline defaults</item>
/// </list>
/// <para>
/// <strong>Thread Safety:</strong>
/// </para>
/// <para>
/// This class is thread-safe for reads but writes should be coordinated externally
/// if multiple threads write to the same file.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var medium = new FileStorageMedium("data/companies.csv");
///
/// // Check if file exists
/// var exists = await medium.Exists().Run();
///
/// // Read from file
/// var readResult = await medium.ReadStream().Run();
/// readResult.Match(
///     Succ: stream => { /* process stream */ },
///     Fail: error => Console.WriteLine($"Read failed: {error}")
/// );
///
/// // Write to file
/// using var writeStream = new MemoryStream(data);
/// var writeResult = await medium.WriteStream(writeStream).Run();
/// </code>
/// </example>
public sealed class FileStorageMedium : IStorageMedium
{
  private readonly string _filePath;

  /// <summary>
  /// Creates a new file storage medium.
  /// </summary>
  /// <param name="filePath">Path to the file (absolute or relative)</param>
  /// <exception cref="ArgumentNullException">Thrown if filePath is null</exception>
  /// <exception cref="ArgumentException">Thrown if filePath is empty or whitespace</exception>
  public FileStorageMedium(string filePath)
  {
    if (string.IsNullOrWhiteSpace(filePath))
    {
      throw new ArgumentException("File path cannot be null or whitespace", nameof(filePath));
    }

    _filePath = filePath;
  }

  /// <summary>
  /// Gets the file path for this storage medium.
  /// </summary>
  public string FilePath => _filePath;

  /// <inheritdoc/>
  public StorageTraits Traits => new StorageTraits();

  /// <inheritdoc/>
  public FlowIO<Stream> ReadStream()
  {
    return FlowIO.LiftAsync(
      async (CancellationToken ct) =>
      {
        if (!File.Exists(_filePath))
        {
          throw new FileNotFoundException($"File not found at path: {_filePath}", _filePath);
        }

        // Open file for reading with shared read access
        var stream = new FileStream(
          _filePath,
          FileMode.Open,
          FileAccess.Read,
          FileShare.Read,
          bufferSize: 4096,
          useAsync: true
        );

        return (Stream)stream;
      }
    );
  }

  /// <inheritdoc/>
  public FlowIO<FlowUnit> WriteStream(Stream stream)
  {
    return FlowIO.LiftAsync(
      async (CancellationToken ct) =>
      {
        if (stream == null)
        {
          throw new ArgumentNullException(nameof(stream));
        }

        // Ensure parent directory exists
        var directory = Path.GetDirectoryName(_filePath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
          Directory.CreateDirectory(directory);
        }

        // Write to temp file first for atomic operation
        var tempPath = $"{_filePath}.tmp.{Guid.NewGuid():N}";

        try
        {
          // Write to temp file
          using (
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
            await stream.CopyToAsync(fileStream, ct);
            await fileStream.FlushAsync(ct);
          }

          // Atomic rename: replace old file with new one
          File.Move(tempPath, _filePath, overwrite: true);

          return FlowUnit.Default;
        }
        catch
        {
          // Clean up temp file on failure
          if (File.Exists(tempPath))
          {
            try
            {
              File.Delete(tempPath);
            }
            catch
            {
              // Ignore cleanup errors
            }
          }
          throw;
        }
      }
    );
  }

  /// <inheritdoc/>
  public FlowIO<bool> Exists()
  {
    return FlowIO.Lift(() => File.Exists(_filePath));
  }

  /// <inheritdoc/>
  /// <remarks>
  /// Validates that the write destination is accessible.
  /// Walks up to the nearest existing ancestor to check write permissions,
  /// so a missing intermediate directory is not itself a failure.
  /// </remarks>
  public FlowIO<Data.Validation.ValidationResult> InspectTarget() =>
    FlowIO.LiftAsync(ct => LocalFileWriteProbe.ProbeAsync(_filePath, ct));
}
