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
    FlowIO.LiftAsync(async () =>
    {
      if (!File.Exists(_filePath))
      {
        throw new FileNotFoundException($"Binary file not found: {_filePath}");
      }

      return await File.ReadAllBytesAsync(_filePath);
    });

  /// <inheritdoc/>
  public FlowIO<FlowUnit> Save(byte[] data) =>
    FlowIO.LiftAsync(async () =>
    {
      var directory = Path.GetDirectoryName(_filePath);
      if (!string.IsNullOrEmpty(directory))
      {
        Directory.CreateDirectory(directory);
      }

      await File.WriteAllBytesAsync(_filePath, data);
      return FlowUnit.Default;
    });

  /// <inheritdoc/>
  public FlowIO<bool> Exists() =>
    FlowIO.LiftAsync(async () =>
    {
      return File.Exists(_filePath);
    });
}
