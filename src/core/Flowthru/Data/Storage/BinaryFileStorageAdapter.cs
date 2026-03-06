using Flowthru.Data.Capabilities;
using Flowthru.Effects;

namespace Flowthru.Data.Storage;

/// <summary>
/// Storage adapter for binary files with byte array content.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Use Cases:</strong> Images (PNG, JPG), PDFs, serialized binary data
/// </para>
/// <para>
/// <strong>Capabilities:</strong>
/// </para>
/// <list type="bullet">
/// <item>ISeedable: true (file can exist before pipeline runs)</item>
/// <item>IReadOnly: false</item>
/// </list>
/// </remarks>
public sealed class BinaryFileStorageAdapter : IStorageAdapter<byte[]>, ISeedable
{
  private readonly string _filePath;

  public BinaryFileStorageAdapter(string filePath)
  {
    _filePath = filePath;
  }

  /// <inheritdoc/>
  public bool CanBeSeed => File.Exists(_filePath);

  /// <inheritdoc/>
  public FlowIO<byte[]> Load() =>
    FlowIO.LiftAsync(
      async (CancellationToken ct) =>
      {
        if (!File.Exists(_filePath))
        {
          throw new FileNotFoundException($"Binary file not found: {_filePath}");
        }

        return await File.ReadAllBytesAsync(_filePath, ct);
      }
    );

  /// <inheritdoc/>
  public FlowIO<FlowUnit> Save(byte[] data) =>
    FlowIO.LiftAsync(
      async (CancellationToken ct) =>
      {
        var directory = Path.GetDirectoryName(_filePath);
        if (!string.IsNullOrEmpty(directory))
        {
          Directory.CreateDirectory(directory);
        }

        await File.WriteAllBytesAsync(_filePath, data, ct);
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
            message: $"Binary file not found: {_filePath}",
            details: "File does not exist or is not accessible"
          );
        }

        try
        {
          // Attempt to open the file to verify it's accessible
          await using var stream = File.OpenRead(_filePath);
          return Data.Validation.ValidationResult.Success();
        }
        catch (Exception ex)
        {
          return Data.Validation.ValidationResult.Failure(
            catalogKey: Path.GetFileName(_filePath),
            errorType: Data.Validation.ValidationErrorType.NotFound,
            message: $"Binary file is not accessible: {_filePath}",
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
            message: $"Binary file not found: {_filePath}",
            details: "File does not exist or is not accessible"
          );
        }

        try
        {
          // Read the entire file to validate it's fully readable
          await File.ReadAllBytesAsync(_filePath, ct);
          return Data.Validation.ValidationResult.Success();
        }
        catch (Exception ex)
        {
          return Data.Validation.ValidationResult.Failure(
            catalogKey: Path.GetFileName(_filePath),
            errorType: Data.Validation.ValidationErrorType.DeserializationError,
            message: $"Failed to read binary file: {_filePath}",
            details: ex.Message
          );
        }
      }
    );
  }
}
