using Flowthru.Prelude;

namespace Flowthru.Data.Storage;

/// <summary>
/// Storage adapter for binary files with <see cref="byte"/>-array
/// content. Direct <see cref="IStorageAdapter{T}"/> implementation —
/// the medium × format × container composition isn't useful for the
/// raw "load file → byte[], save byte[] → file" path.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Use cases.</strong> PNG/JPG images, PDFs, serialized
/// binary blobs, anything where the payload is just a byte buffer
/// and the schema lives in the data itself.
/// </para>
/// </remarks>
public sealed class BinaryFileStorageAdapter : IStorageAdapter<byte[]>
{
  private readonly string _filePath;

  public BinaryFileStorageAdapter(string filePath)
  {
    _filePath = filePath ?? throw new ArgumentNullException(nameof(filePath));
  }

  /// <inheritdoc/>
  public StorageTraits Traits => new();

  /// <inheritdoc/>
  public FlowIO<byte[]> Load() =>
    FlowIO.LiftAsync(async ct =>
    {
      if (!File.Exists(_filePath))
        throw new FileNotFoundException($"Binary file not found: {_filePath}");
      return await File.ReadAllBytesAsync(_filePath, ct).ConfigureAwait(false);
    }, source: $"BinaryFileStorageAdapter.Load[{_filePath}]");

  /// <inheritdoc/>
  public FlowIO<FlowUnit> Save(byte[] data) =>
    FlowIO.LiftAsync(async ct =>
    {
      var directory = Path.GetDirectoryName(_filePath);
      if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);
      await File.WriteAllBytesAsync(_filePath, data, ct).ConfigureAwait(false);
      return FlowUnit.Default;
    }, source: $"BinaryFileStorageAdapter.Save[{_filePath}]");

  /// <inheritdoc/>
  public FlowIO<bool> Exists() => FlowIO.Lift(() => File.Exists(_filePath));

  /// <inheritdoc/>
  public FlowIO<ValidationResult> InspectShallow(int sampleSize) =>
    FlowIO.LiftAsync(async ct =>
    {
      if (!File.Exists(_filePath))
        return ValidationResult.Failure(
          catalogKey: Path.GetFileName(_filePath),
          errorType: ValidationErrorType.NotFound,
          message: $"Binary file not found: {_filePath}"
        );

      try
      {
        await using var stream = File.OpenRead(_filePath);
        return ValidationResult.Success();
      }
      catch (Exception ex)
      {
        return ValidationResult.Failure(
          catalogKey: Path.GetFileName(_filePath),
          errorType: ValidationErrorType.NotFound,
          message: $"Binary file is not accessible: {_filePath}",
          details: ex.Message
        );
      }
    }, source: $"BinaryFileStorageAdapter.InspectShallow[{_filePath}]");

  /// <inheritdoc/>
  public FlowIO<ValidationResult> InspectDeep() =>
    FlowIO.LiftAsync(async ct =>
    {
      if (!File.Exists(_filePath))
        return ValidationResult.Failure(
          catalogKey: Path.GetFileName(_filePath),
          errorType: ValidationErrorType.NotFound,
          message: $"Binary file not found: {_filePath}"
        );

      try
      {
        await File.ReadAllBytesAsync(_filePath, ct).ConfigureAwait(false);
        return ValidationResult.Success();
      }
      catch (Exception ex)
      {
        return ValidationResult.Failure(
          catalogKey: Path.GetFileName(_filePath),
          errorType: ValidationErrorType.DeserializationError,
          message: $"Failed to read binary file: {_filePath}",
          details: ex.Message
        );
      }
    }, source: $"BinaryFileStorageAdapter.InspectDeep[{_filePath}]");

  /// <inheritdoc/>
  public FlowIO<ValidationResult> InspectTarget() =>
    FlowIO.LiftAsync(ct => LocalFileWriteProbe.ProbeAsync(_filePath, ct),
      source: $"BinaryFileStorageAdapter.InspectTarget[{_filePath}]");
}
