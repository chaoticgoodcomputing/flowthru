namespace Flowthru.Data.Storage;

/// <summary>
/// Shared write-access probe for local filesystem paths. Used by
/// file-based storage adapters and media to implement
/// <c>InspectTarget()</c> consistently.
/// </summary>
/// <remarks>
/// <para>
/// The probe walks up the directory tree until it finds the nearest
/// existing ancestor, then writes and immediately deletes a zero-byte
/// sentinel file there. A missing intermediate directory is *not* a
/// pre-flight blocker — every file-based <c>Save()</c> calls
/// <c>Directory.CreateDirectory()</c> at write time. Only an inaccessible
/// filesystem root or refused-write at the nearest existing ancestor
/// produces a <see cref="ValidationErrorType.WriteAccessDenied"/> result.
/// </para>
/// </remarks>
public static class LocalFileWriteProbe
{
  /// <summary>
  /// Probes write access for the directory <paramref name="filePath"/>
  /// would land in, walking up the tree to the nearest existing ancestor
  /// if needed.
  /// </summary>
  /// <remarks>
  /// Every observable failure mode — including null/empty/whitespace
  /// inputs — surfaces as a <see cref="ValidationResult.Failure"/>
  /// (fail-as-value). The probe does not throw. This is the contract
  /// every <c>InspectTarget()</c>-shaped probe in Flowthru holds: pre-flight
  /// aggregates findings into FT3xxx diagnostics, so a thrown
  /// <see cref="ArgumentException"/> would bypass the aggregation surface
  /// and reach the user as a stack trace instead of an actionable
  /// validation error.
  /// </remarks>
  public static async Task<ValidationResult> ProbeAsync(string filePath, CancellationToken ct)
  {
    if (string.IsNullOrWhiteSpace(filePath))
    {
      return ValidationResult.Failure(
        catalogKey: string.Empty,
        errorType: ValidationErrorType.WriteAccessDenied,
        message: "Write destination path is empty",
        details: "An empty or whitespace path cannot be probed. "
          + "Adapter configuration must supply a non-empty destination path."
      );
    }

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

    // Walk up until we find an ancestor that already exists. Save() will
    // create intermediate directories at runtime; we only need to know
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
          // Probe cleanup failure is non-fatal.
        }
      }
    }
  }
}
