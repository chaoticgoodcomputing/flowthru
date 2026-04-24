using Flowthru.Core.Data.Capabilities;
using Flowthru.Core.Effects;

namespace Flowthru.Core.Data.Storage;

/// <summary>
/// Storage adapter for plain text files with string content.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Use Cases:</strong> Markdown reports, configuration files, plain text logs
/// </para>
/// <para>
/// <strong>Storage Traits:</strong> All traits use filesystem baseline defaults
/// </para>
/// </remarks>
public sealed class TextFileStorageAdapter : IStorageAdapter<string>
{
  private readonly string _filePath;

  /// <summary>
  /// Initializes a new instance of the <see cref="TextFileStorageAdapter"/> class with the specified file path.
  /// </summary>
  /// <param name="filePath"></param>
  public TextFileStorageAdapter(string filePath)
  {
    _filePath = filePath;
  }

  /// <inheritdoc/>
  public StorageTraits Traits => new StorageTraits();

  /// <inheritdoc/>
  public FlowIO<string> Load() =>
    FlowIO.LiftAsync(
      async (CancellationToken ct) =>
      {
        if (!File.Exists(_filePath))
        {
          throw new FileNotFoundException($"Text file not found: {_filePath}");
        }

        return await File.ReadAllTextAsync(_filePath, ct);
      }
    );

  /// <inheritdoc/>
  public FlowIO<FlowUnit> Save(string data) =>
    FlowIO.LiftAsync(
      async (CancellationToken ct) =>
      {
        var directory = Path.GetDirectoryName(_filePath);
        if (!string.IsNullOrEmpty(directory))
        {
          Directory.CreateDirectory(directory);
        }

        await File.WriteAllTextAsync(_filePath, data, ct);
        return FlowUnit.Default;
      }
    );

  /// <inheritdoc/>
  public FlowIO<bool> Exists() => FlowIO.Lift(() => File.Exists(_filePath));

  /// <inheritdoc/>
  public FlowIO<Data.Validation.ValidationResult> InspectShallow(int sampleSize)
  {
    return FlowIO.LiftAsync(
      async (CancellationToken ct) =>
      {
        if (!File.Exists(_filePath))
        {
          return Data.Validation.ValidationResult.Failure(
            catalogKey: Path.GetFileName(_filePath),
            errorType: Data.Validation.ValidationErrorType.NotFound,
            message: $"Text file not found: {_filePath}",
            details: "File does not exist or is not accessible"
          );
        }

        try
        {
          // Attempt to read the file to verify it's accessible
          await using var stream = File.OpenRead(_filePath);
          return Data.Validation.ValidationResult.Success();
        }
        catch (Exception ex)
        {
          return Data.Validation.ValidationResult.Failure(
            catalogKey: Path.GetFileName(_filePath),
            errorType: Data.Validation.ValidationErrorType.NotFound,
            message: $"Text file is not accessible: {_filePath}",
            details: ex.Message
          );
        }
      }
    );
  }

  /// <inheritdoc/>
  public FlowIO<Data.Validation.ValidationResult> InspectDeep()
  {
    return FlowIO.LiftAsync(
      async (CancellationToken ct) =>
      {
        if (!File.Exists(_filePath))
        {
          return Data.Validation.ValidationResult.Failure(
            catalogKey: Path.GetFileName(_filePath),
            errorType: Data.Validation.ValidationErrorType.NotFound,
            message: $"Text file not found: {_filePath}",
            details: "File does not exist or is not accessible"
          );
        }

        try
        {
          // Read the entire file to validate it's fully readable
          await File.ReadAllTextAsync(_filePath, ct);
          return Data.Validation.ValidationResult.Success();
        }
        catch (Exception ex)
        {
          return Data.Validation.ValidationResult.Failure(
            catalogKey: Path.GetFileName(_filePath),
            errorType: Data.Validation.ValidationErrorType.DeserializationError,
            message: $"Failed to read text file: {_filePath}",
            details: ex.Message
          );
        }
      }
    );
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
