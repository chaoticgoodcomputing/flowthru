using System.Runtime.CompilerServices;
using System.Xml.Serialization;
using Flowthru.Core.Abstractions;
using Flowthru.Core.Data.Validation;

namespace Flowthru.Core.Data.Storage;

/// <summary>
/// Read-only storage adapter that deserializes every <c>*.xml</c> file in a directory,
/// yielding each as an <see cref="XmlDocument{T}"/> wrapper that carries the source file name.
/// </summary>
/// <typeparam name="T">The document type for each XML file.</typeparam>
/// <remarks>
/// <para>
/// Files are processed in lexicographic order for deterministic output across runs.
/// </para>
/// <para>
/// The <see cref="XmlDocument{T}.FileName"/> property contains only the file name
/// (not the full path), so downstream steps can derive semantic meaning from the
/// naming convention used when staging the files.
/// </para>
/// <para>
/// <strong>Read-only:</strong> This adapter cannot be written to. It represents an
/// immutable staged input layer.
/// </para>
/// </remarks>
public sealed class XmlDirectoryStorageAdapter<T> : ReadOnlyDirectoryStorageAdapter<XmlDocument<T>>
  where T : IStructuredSerializable
{
  private readonly XmlSerializer _serializer;

  /// <summary>Creates a new XML directory storage adapter.</summary>
  /// <param name="directoryPath">Path to the directory containing XML files.</param>
  public XmlDirectoryStorageAdapter(string directoryPath)
    : base(directoryPath, "*.xml", typeof(T).Name)
  {
    _serializer = new XmlSerializer(typeof(T));
  }

  /// <inheritdoc/>
  protected override async IAsyncEnumerable<XmlDocument<T>> LoadFile(
    string filePath,
    [EnumeratorCancellation] CancellationToken ct
  )
  {
    ct.ThrowIfCancellationRequested();
    await using var stream = File.OpenRead(filePath);
    var document =
      (T?)_serializer.Deserialize(stream)
      ?? throw new InvalidOperationException($"Failed to deserialize XML from '{filePath}'");
    yield return new XmlDocument<T>(Path.GetFileName(filePath), document);
  }

  /// <inheritdoc/>
  protected override async Task<ValidationResult> ValidateFileAsync(
    string filePath,
    int sampleSize,
    CancellationToken ct
  )
  {
    ct.ThrowIfCancellationRequested();
    try
    {
      await using var stream = File.OpenRead(filePath);
      _serializer.Deserialize(stream);
      return ValidationResult.Success();
    }
    catch (InvalidOperationException ex)
    {
      return ValidationResult.Failure(
        catalogKey: Path.GetFileName(filePath),
        errorType: ValidationErrorType.DeserializationError,
        message: $"Invalid XML in file: {filePath}",
        details: ex.InnerException?.Message ?? ex.Message
      );
    }
  }
}
