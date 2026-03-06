using Flowthru.Data.Capabilities;
using Flowthru.Data.Storage;
using Flowthru.Data.Validation;
using Flowthru.Effects;

namespace Flowthru.Extensions.MLNet.Storage;

/// <summary>
/// Storage adapter for ONNX model files.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Use Case:</strong> Pre-trained ONNX models for ML.NET inference
/// </para>
/// <para>
/// <strong>Validation:</strong> Implements inspection for early pipeline validation
/// </para>
/// <para>
/// <strong>Capabilities:</strong>
/// </para>
/// <list type="bullet">
/// <item>ISeedable: true (ONNX models are Layer 0 inputs)</item>
/// <item>Inspection: validates file before pipeline execution</item>
/// </list>
/// </remarks>
/// <example>
/// <code>
/// var adapter = new OnnxModelStorageAdapter("models/bert-base.onnx");
/// var validation = await adapter.ShallowInspect().RunAsync();
///
/// if (!validation.IsValid)
/// {
///     Console.WriteLine($"ONNX model validation failed: {validation.ErrorMessage}");
/// }
/// </code>
/// </example>
public sealed class OnnxModelStorageAdapter : IStorageAdapter<byte[]>, ISeedable
{
  private readonly string _filePath;

  /// <summary>
  /// Creates a new ONNX model storage adapter.
  /// </summary>
  /// <param name="filePath">Path to .onnx model file</param>
  public OnnxModelStorageAdapter(string filePath)
  {
    _filePath = filePath ?? throw new ArgumentNullException(nameof(filePath));
  }

  /// <inheritdoc/>
  public bool CanBeSeed => File.Exists(_filePath);

  /// <inheritdoc/>
  public FlowIO<byte[]> Load() =>
    FlowIO.LiftAsync(
      async (ct) =>
      {
        if (!File.Exists(_filePath))
        {
          throw new FileNotFoundException(
            $"ONNX model file not found: {_filePath}\n"
              + $"Please provide a valid ONNX model file. See docs/guides/using-onnx-models-from-huggingface.md",
            _filePath
          );
        }

        return await File.ReadAllBytesAsync(_filePath, ct);
      }
    );

  /// <inheritdoc/>
  public FlowIO<FlowUnit> Save(byte[] data) =>
    FlowIO.Lift<FlowUnit>(() =>
    {
      throw new InvalidOperationException(
        "ONNX models are read-only and should not be written by pipelines. "
          + "Models should be provided as seed data (Layer 0 inputs)."
      );
    });

  /// <inheritdoc/>
  public FlowIO<bool> Exists() => FlowIO.Lift(() => File.Exists(_filePath));

  /// <inheritdoc/>
  public FlowIO<ValidationResult> InspectShallow(int sampleSize) =>
    FlowIO.LiftAsync(
      async (ct) =>
      {
        // Check file existence
        if (!File.Exists(_filePath))
        {
          return ValidationResult.Failure(
            catalogKey: "OnnxModel",
            errorType: ValidationErrorType.NotFound,
            message: $"ONNX model file not found: {_filePath}",
            details: "Please provide a valid ONNX model file. See docs/guides/using-onnx-models-from-huggingface.md"
          );
        }

        // Check file extension
        var extension = Path.GetExtension(_filePath).ToLowerInvariant();
        if (extension != ".onnx")
        {
          return ValidationResult.Failure(
            catalogKey: "OnnxModel",
            errorType: ValidationErrorType.InvalidFormat,
            message: $"File does not have .onnx extension: {_filePath}",
            details: $"Found extension: {extension}"
          );
        }

        // Check file is readable and non-empty
        try
        {
          var fileInfo = new FileInfo(_filePath);
          if (fileInfo.Length == 0)
          {
            return ValidationResult.Failure(
              catalogKey: "OnnxModel",
              errorType: ValidationErrorType.InvalidFormat,
              message: $"ONNX model file is empty: {_filePath}",
              details: "File size is 0 bytes"
            );
          }

          // Check we can read the file
          using var stream = File.OpenRead(_filePath);
          var buffer = new byte[8];
          var bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);

          if (bytesRead == 0)
          {
            return ValidationResult.Failure(
              catalogKey: "OnnxModel",
              errorType: ValidationErrorType.InvalidFormat,
              message: $"ONNX model file cannot be read: {_filePath}",
              details: "Failed to read any bytes from file"
            );
          }

          return ValidationResult.Success();
        }
        catch (Exception ex)
        {
          return ValidationResult.Failure(
            catalogKey: "OnnxModel",
            errorType: ValidationErrorType.InspectionFailure,
            message: $"Error accessing ONNX model file: {_filePath}",
            details: ex.Message
          );
        }
      }
    );

  /// <inheritdoc/>
  public FlowIO<ValidationResult> InspectDeep() =>
    // For binary model files, deep inspection is equivalent to shallow
    // We can't meaningfully validate internal model structure without
    // loading it with ML.NET, which is beyond inspection scope
    InspectShallow(sampleSize: 0);

  /// <summary>
  /// Gets the file path to the ONNX model.
  /// </summary>
  public string FilePath => _filePath;
}
