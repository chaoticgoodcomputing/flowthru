using Flowthru.Core.Effects;

namespace Flowthru.Core.Validation;

/// <summary>
/// Filesystem-shaped <see cref="FlowValidation"/> helpers, available for any
/// catalog or extension that touches local files. Cross-platform; lives in
/// core so multiple extensions can share them without taking a dependency on
/// a specific format extension.
/// </summary>
public static class FsValidations
{
  /// <summary>
  /// Validates that <paramref name="directoryPath"/> exists and is writable.
  /// Probes by writing and immediately deleting a sentinel file — the same
  /// pattern <c>InspectTarget</c> uses for filesystem-backed items.
  /// </summary>
  public static FlowValidation IsWritable(string directoryPath)
  {
    if (string.IsNullOrWhiteSpace(directoryPath))
    {
      return FlowValidation.Fail(
        source: nameof(IsWritable),
        message: "Directory path is null or empty."
      );
    }

    if (!Directory.Exists(directoryPath))
    {
      return FlowValidation.Fail(
        source: directoryPath,
        message: $"Directory does not exist: {directoryPath}"
      );
    }

    var probe = Path.Combine(directoryPath, $".flowthru-probe-{Guid.NewGuid():N}");
    try
    {
      File.WriteAllText(probe, string.Empty);
      File.Delete(probe);
      return FlowValidation.Pass;
    }
    catch (Exception ex)
    {
      return FlowValidation.Fail(
        source: directoryPath,
        message: $"Directory is not writable: {ex.Message}",
        exception: ex
      );
    }
  }

  /// <summary>
  /// Validates that <paramref name="path"/> exists as either a file or a
  /// directory.
  /// </summary>
  public static FlowValidation Exists(string path)
  {
    if (string.IsNullOrWhiteSpace(path))
    {
      return FlowValidation.Fail(
        source: nameof(Exists),
        message: "Path is null or empty."
      );
    }

    if (File.Exists(path) || Directory.Exists(path))
    {
      return FlowValidation.Pass;
    }

    return FlowValidation.Fail(source: path, message: $"Path does not exist: {path}");
  }
}
