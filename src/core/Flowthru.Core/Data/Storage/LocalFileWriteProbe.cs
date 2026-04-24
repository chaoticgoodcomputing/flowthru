using Flowthru.Core.Data.Validation;

namespace Flowthru.Core.Data.Storage;

/// <summary>
/// Shared write-access probe for local filesystem paths.
/// </summary>
/// <remarks>
/// <para>
/// Used by all file-based storage adapters and media to implement
/// <c>InspectTarget()</c> consistently without duplication.
/// </para>
/// <para>
/// <strong>Semantics:</strong>
/// </para>
/// <para>
/// The probe intentionally does <em>not</em> require the destination directory to
/// already exist. All file-based <c>Save()</c> implementations call
/// <c>Directory.CreateDirectory()</c> at write time, so a missing directory is never
/// a pre-flight blocker — only a missing or inaccessible filesystem root is.
/// </para>
/// <para>
/// The probe walks up the directory tree until it finds the nearest ancestor that
/// exists, then writes and immediately deletes a zero-byte sentinel file there. A
/// <see cref="ValidationErrorType.WriteAccessDenied"/> failure is returned only when:
/// </para>
/// <list type="bullet">
/// <item>No existing ancestor can be found (e.g. a nonexistent drive or mount point)</item>
/// <item>The OS refuses the write at the nearest existing ancestor</item>
/// </list>
/// </remarks>
public static class LocalFileWriteProbe
{
  /// <summary>
  /// Probes write access for the directory that <paramref name="filePath"/> would be
  /// written to, walking up the tree to the nearest existing ancestor if needed.
  /// </summary>
  /// <param name="filePath">The intended destination file path (need not exist yet).</param>
  /// <param name="ct">Cancellation token.</param>
  public static async ValueTask<ValidationResult> ProbeAsync(string filePath, CancellationToken ct)
  {
    var fullPath = Path.GetFullPath(filePath);
    var directory = Path.GetDirectoryName(fullPath);

    if (string.IsNullOrEmpty(directory))
    {
      return ValidationResult.Failure(
        catalogKey: Path.GetFileName(filePath),
        errorType: ValidationErrorType.WriteAccessDenied,
        message: $"Cannot determine write destination directory for '{filePath}'",
        details: "Path has no parent directory component"
      );
    }

    // Walk up until we find an ancestor that already exists.
    // Save() will create intermediate directories at runtime; we only need to know
    // whether the filesystem root is accessible and permits writes.
    var probe = directory;
    while (!string.IsNullOrEmpty(probe) && !Directory.Exists(probe))
    {
      probe = Path.GetDirectoryName(probe);
    }

    if (string.IsNullOrEmpty(probe))
    {
      return ValidationResult.Failure(
        catalogKey: Path.GetFileName(filePath),
        errorType: ValidationErrorType.WriteAccessDenied,
        message: $"Write destination is unreachable: no accessible ancestor found for '{directory}'",
        details: "Verify that the drive or mount point exists and is accessible"
      );
    }

    // Write and immediately delete a zero-byte sentinel file to confirm write access.
    var probeFile = Path.Combine(probe, $".flowthru-probe-{Guid.NewGuid():N}");
    try
    {
      await File.WriteAllBytesAsync(probeFile, Array.Empty<byte>(), ct);
      return ValidationResult.Success();
    }
    catch (Exception ex)
    {
      return ValidationResult.Failure(
        catalogKey: Path.GetFileName(filePath),
        errorType: ValidationErrorType.WriteAccessDenied,
        message: $"Write access denied for destination path: {directory}",
        details: ex.Message
      );
    }
    finally
    {
      if (File.Exists(probeFile))
      {
        try
        {
          File.Delete(probeFile);
        }
        catch
        {
          // Probe cleanup failure is non-fatal; the sentinel file is identifiable by name.
        }
      }
    }
  }
}
