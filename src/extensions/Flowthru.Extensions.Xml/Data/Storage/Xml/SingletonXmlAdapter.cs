using System.Xml.Serialization;
using Flowthru.Data.Schema;
using Flowthru.Prelude;

namespace Flowthru.Data.Storage.Xml;

/// <summary>
/// Storage adapter for a single <typeparamref name="T"/> value
/// persisted as one XML document. Mirrors the
/// <see cref="SingletonJsonAdapter{T}"/> pattern: implements
/// <see cref="IStorageAdapter{T}"/> directly rather than going through
/// <c>IFormatSerializer&lt;TRow&gt;</c> + <c>ComposedStorageAdapter</c>,
/// because <see cref="XmlSerializer"/> is a whole-document API and the
/// extension does not currently expose a row-streaming mode.
/// </summary>
/// <typeparam name="T">
/// Document type. Must implement <see cref="IStructuredSerializable"/>
/// (asserted at the smart-constructor call site) and be public with a
/// parameterless constructor (a hard requirement of
/// <see cref="XmlSerializer"/>; not currently encoded in the type
/// system, so callers see the runtime exception if they violate it).
/// </typeparam>
/// <remarks>
/// <para>
/// <strong>Serialisation.</strong> Uses <see cref="XmlSerializer"/>.
/// Decorate <typeparamref name="T"/> with
/// <see cref="XmlRootAttribute"/> / <see cref="XmlElementAttribute"/> /
/// <see cref="XmlAttributeAttribute"/> as required.
/// </para>
/// <para>
/// <strong>Atomic writes.</strong> Save streams to a temp file
/// adjacent to the destination, then renames; partial writes never
/// leak into the target path even on crash.
/// </para>
/// <para>
/// <strong>No row streaming.</strong> Streaming row-wise XML is a
/// future <c>XmlFormatSerializer&lt;TRow&gt;</c> implementing
/// <c>IFormatRowReader&lt;TRow&gt;</c> +
/// <c>IFormatStreamReader&lt;TRow&gt;</c>; not in this adapter's
/// scope.
/// </para>
/// </remarks>
public sealed class SingletonXmlAdapter<T> : IStorageAdapter<T>
  where T : notnull, IStructuredSerializable
{
  private readonly string _filePath;
  private readonly FileStorageMedium _medium;
  private readonly XmlSerializer _serializer;

  public SingletonXmlAdapter(string filePath)
  {
    if (string.IsNullOrWhiteSpace(filePath))
    {
      throw new ArgumentException("File path cannot be null or whitespace", nameof(filePath));
    }
    _filePath = filePath;
    _medium = new FileStorageMedium(filePath);
    _serializer = new XmlSerializer(typeof(T));
  }

  /// <inheritdoc/>
  public StorageTraits Traits => _medium.Traits;

  /// <inheritdoc/>
  public FlowIO<T> Load() =>
    FlowIO.LiftAsync(async ct =>
    {
      ct.ThrowIfCancellationRequested();
      await using var stream = new FileStream(
        _filePath,
        FileMode.Open,
        FileAccess.Read,
        FileShare.Read,
        bufferSize: 4096,
        useAsync: true
      );
      // XmlSerializer is sync-only; no async deserialise on the
      // framework API. The FileStream is async-capable so the buffer
      // reads are non-blocking, but the XML parse runs synchronously.
      var value = (T?)_serializer.Deserialize(stream);
      if (value is null)
      {
        throw new InvalidOperationException(
          $"Deserialized null value from singleton XML file at '{_filePath}'."
        );
      }
      return value;
    });

  /// <inheritdoc/>
  public FlowIO<FlowUnit> Save(T data) =>
    FlowIO.LiftAsync(async ct =>
    {
      if (data is null) throw new ArgumentNullException(nameof(data));
      ct.ThrowIfCancellationRequested();

      var directory = Path.GetDirectoryName(_filePath);
      if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
      {
        Directory.CreateDirectory(directory);
      }
      var tempPath = $"{_filePath}.tmp.{Guid.NewGuid():N}";
      try
      {
        await using (
          var fs = new FileStream(
            tempPath,
            FileMode.Create,
            FileAccess.Write,
            FileShare.None,
            bufferSize: 4096,
            useAsync: true
          )
        )
        {
          _serializer.Serialize(fs, data);
        }
        File.Move(tempPath, _filePath, overwrite: true);
        return FlowUnit.Default;
      }
      catch
      {
        if (File.Exists(tempPath))
        {
          try { File.Delete(tempPath); } catch { /* cleanup best-effort */ }
        }
        throw;
      }
    });

  /// <inheritdoc/>
  public FlowIO<bool> Exists() => _medium.Exists();

  /// <inheritdoc/>
  public FlowIO<ValidationResult> InspectShallow(int sampleSize) =>
    FlowIO.LiftAsync<ValidationResult>(async ct =>
    {
      ct.ThrowIfCancellationRequested();
      var label = typeof(T).Name;
      if (!File.Exists(_filePath))
      {
        return ValidationResult.Failure(
          catalogKey: label,
          errorType: ValidationErrorType.NotFound,
          message: $"Singleton XML file not found at '{_filePath}'"
        );
      }
      try
      {
        await using var stream = new FileStream(
          _filePath,
          FileMode.Open,
          FileAccess.Read,
          FileShare.Read,
          bufferSize: 4096,
          useAsync: true
        );
        _ = _serializer.Deserialize(stream);
        return ValidationResult.Success();
      }
      catch (InvalidOperationException ex)
      {
        // XmlSerializer wraps the underlying parse error in
        // InvalidOperationException's InnerException — surface that
        // wrapped detail directly so the operator sees the actual
        // schema-mismatch / parse failure.
        return ValidationResult.Failure(
          catalogKey: label,
          errorType: ValidationErrorType.DeserializationError,
          message: $"Failed to deserialize singleton XML for '{label}'",
          details: ex.InnerException?.Message ?? ex.Message
        );
      }
      catch (Exception ex)
      {
        return ValidationResult.Failure(
          catalogKey: label,
          errorType: ValidationErrorType.DeserializationError,
          message: $"Failed to access XML file for '{label}'",
          details: ex.Message
        );
      }
    });

  /// <inheritdoc/>
  public FlowIO<ValidationResult> InspectDeep() => InspectShallow(sampleSize: 0);

  /// <inheritdoc/>
  public FlowIO<ValidationResult> InspectTarget() => _medium.InspectTarget();
}
