using Flowthru.Data.Capabilities;
using Flowthru.Effects;

namespace Flowthru.Data.Storage;

/// <summary>
/// Storage adapter for plain text files with string content.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Use Cases:</strong> Markdown reports, configuration files, plain text logs
/// </para>
/// <para>
/// <strong>Capabilities:</strong>
/// </para>
/// <list type="bullet">
/// <item>ISeedable: true (file can exist before pipeline runs)</item>
/// <item>IReadOnly: false</item>
/// </list>
/// </remarks>
public sealed class TextFileStorageAdapter : IStorageAdapter<string>, ISeedable
{
  private readonly string _filePath;

  public TextFileStorageAdapter(string filePath)
  {
    _filePath = filePath;
  }

  /// <inheritdoc/>
  public bool CanBeSeed => File.Exists(_filePath);

  /// <inheritdoc/>
  public FlowIO<string> Load() =>
    FlowIO.LiftAsync(async () =>
    {
      if (!File.Exists(_filePath))
      {
        throw new FileNotFoundException($"Text file not found: {_filePath}");
      }

      return await File.ReadAllTextAsync(_filePath);
    });

  /// <inheritdoc/>
  public FlowIO<FlowUnit> Save(string data) =>
    FlowIO.LiftAsync(async () =>
    {
      var directory = Path.GetDirectoryName(_filePath);
      if (!string.IsNullOrEmpty(directory))
      {
        Directory.CreateDirectory(directory);
      }

      await File.WriteAllTextAsync(_filePath, data);
      return FlowUnit.Default;
    });

  /// <inheritdoc/>
  public FlowIO<bool> Exists() =>
    FlowIO.LiftAsync(async () =>
    {
      return File.Exists(_filePath);
    });
}
