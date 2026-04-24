using System.Xml.Serialization;
using Flowthru.Core.Abstractions;
using Flowthru.Core.Data.Capabilities;
using Flowthru.Core.Data.Validation;
using Flowthru.Core.Effects;

namespace Flowthru.Core.Data.Storage;

/// <summary>
/// Storage adapter for a single XML file deserialized to a singleton object.
/// </summary>
/// <typeparam name="T">The document type. Must be XML-serializable and structured-serializable.</typeparam>
/// <remarks>
/// <para>
/// Mirrors <see cref="SingletonJsonStorageAdapter{T}"/>: direct single-object serialization
/// that bypasses the medium/format/container composition, since singleton XML documents
/// do not stream rows.
/// </para>
/// <para>
/// <strong>Serialization:</strong> Uses <see cref="XmlSerializer"/>. Decorate <typeparamref name="T"/>
/// with <c>[XmlRoot]</c>, <c>[XmlElement]</c>, and <c>[XmlAttribute]</c> as needed.
/// </para>
/// </remarks>
public sealed class SingletonXmlStorageAdapter<T> : IStorageAdapter<T>
  where T : IStructuredSerializable
{
  private readonly string _filePath;
  private readonly XmlSerializer _serializer;

  /// <summary>Creates a new singleton XML storage adapter.</summary>
  /// <param name="filePath">Path to the XML file.</param>
  public SingletonXmlStorageAdapter(string filePath)
  {
    _filePath = filePath ?? throw new ArgumentNullException(nameof(filePath));
    _serializer = new XmlSerializer(typeof(T));
  }

  /// <inheritdoc />
  public StorageTraits Traits => new StorageTraits();

  /// <inheritdoc />
  public FlowIO<T> Load()
  {
    return FlowIO.LiftAsync(
      async (CancellationToken ct) =>
      {
        if (!File.Exists(_filePath))
        {
          throw new FileNotFoundException($"XML file not found at '{_filePath}'", _filePath);
        }

        await using var stream = File.OpenRead(_filePath);
        var result = (T?)_serializer.Deserialize(stream);
        return result
          ?? throw new InvalidOperationException($"Failed to deserialize XML from '{_filePath}'");
      }
    );
  }

  /// <inheritdoc />
  public FlowIO<FlowUnit> Save(T data)
  {
    return FlowIO.LiftAsync(
      async (CancellationToken ct) =>
      {
        if (data == null)
        {
          throw new ArgumentNullException(nameof(data));
        }

        var directory = Path.GetDirectoryName(_filePath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
          Directory.CreateDirectory(directory);
        }

        var tempPath = _filePath + ".tmp";
        await using (var stream = File.Create(tempPath))
        {
          _serializer.Serialize(stream, data);
        }

        if (File.Exists(_filePath))
        {
          File.Delete(_filePath);
        }

        File.Move(tempPath, _filePath);
        return FlowUnit.Default;
      }
    );
  }

  /// <inheritdoc />
  public FlowIO<bool> Exists() => FlowIO.Lift(() => File.Exists(_filePath));

  /// <inheritdoc />
  public FlowIO<ValidationResult> InspectShallow(int sampleSize)
  {
    return FlowIO.LiftAsync(
      async (CancellationToken ct) =>
      {
        if (!File.Exists(_filePath))
        {
          return ValidationResult.Failure(
            catalogKey: Path.GetFileName(_filePath),
            errorType: ValidationErrorType.NotFound,
            message: $"XML file not found: {_filePath}",
            details: "File does not exist or is not accessible"
          );
        }

        try
        {
          await using var stream = File.OpenRead(_filePath);
          _serializer.Deserialize(stream);
          return ValidationResult.Success();
        }
        catch (InvalidOperationException ex)
        {
          return ValidationResult.Failure(
            catalogKey: Path.GetFileName(_filePath),
            errorType: ValidationErrorType.DeserializationError,
            message: $"Invalid XML in file: {_filePath}",
            details: ex.InnerException?.Message ?? ex.Message
          );
        }
        catch (Exception ex)
        {
          return ValidationResult.Failure(
            catalogKey: Path.GetFileName(_filePath),
            errorType: ValidationErrorType.NotFound,
            message: $"Failed to access XML file: {_filePath}",
            details: ex.Message
          );
        }
      }
    );
  }

  /// <inheritdoc />
  public FlowIO<ValidationResult> InspectDeep() => InspectShallow(sampleSize: 0);

  /// <inheritdoc />
  public FlowIO<ValidationResult> InspectTarget() =>
    FlowIO.LiftAsync(ct => LocalFileWriteProbe.ProbeAsync(_filePath, ct));
}
