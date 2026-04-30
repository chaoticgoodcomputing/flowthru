using Flowthru.Core.Data.Capabilities;
using Flowthru.Core.Data.Validation;
using Flowthru.Core.Effects;
using SysIO = System.IO;

namespace Flowthru.Core.Data.Storage;

/// <summary>
/// Single, format-agnostic storage adapter for a <see cref="Directory{T}"/> of same-schema
/// files. Format concerns are externalised through <c>perFileAdapter</c>: the directory
/// owns enumeration, save ordering, and target validation; the per-file adapter owns
/// serialisation for one path.
/// </summary>
/// <typeparam name="T">Payload type for each file (e.g. <c>byte[]</c>, <c>IEnumerable&lt;TRow&gt;</c>).</typeparam>
/// <remarks>
/// <para>
/// <strong>Save semantics:</strong> existing files matching the adapter's
/// <c>filePattern</c> are deleted before write, then each entry is written via its
/// per-file adapter. This guarantees the directory state after Save matches the
/// <see cref="Directory{T}"/> that produced it — re-runs are deterministic.
/// </para>
/// <para>
/// <strong>Load semantics:</strong> files matching <c>filePattern</c> are enumerated in
/// ordinal-string order and loaded via per-file adapters. Keys in the resulting
/// <see cref="Directory{T}"/> are the full file paths.
/// </para>
/// <para>
/// <strong>Not a partitioning primitive.</strong> See <see cref="Directory{T}"/>'s remarks.
/// </para>
/// </remarks>
public sealed class DirectoryStorageAdapter<T> : IStorageAdapter<Directory<T>>, IHasEfficientCount
{
  private readonly string _directoryPath;
  private readonly string _filePattern;
  private readonly Func<string, IStorageAdapter<T>> _perFileAdapter;
  private readonly StorageTraits _traits;

  /// <summary>
  /// Initializes the adapter.
  /// </summary>
  /// <param name="directoryPath">Path to the directory.</param>
  /// <param name="filePattern">Glob for matching files (e.g. <c>"*.csv"</c>, <c>"*.png"</c>).</param>
  /// <param name="perFileAdapter">
  /// Factory that, given a file path, returns the storage adapter that handles a single
  /// file at that path. Called once per file on Load (with paths discovered via the glob)
  /// and once per entry on Save (with paths from the <see cref="Directory{T}"/>'s keys).
  /// </param>
  public DirectoryStorageAdapter(
    string directoryPath,
    string filePattern,
    Func<string, IStorageAdapter<T>> perFileAdapter
  )
  {
    if (string.IsNullOrWhiteSpace(directoryPath))
      throw new ArgumentException("Directory path required.", nameof(directoryPath));
    if (string.IsNullOrWhiteSpace(filePattern))
      throw new ArgumentException("File pattern required.", nameof(filePattern));

    _directoryPath = directoryPath;
    _filePattern = filePattern;
    _perFileAdapter = perFileAdapter ?? throw new ArgumentNullException(nameof(perFileAdapter));

    // Inherit CanWrite (and other format-level traits) from a probe per-file adapter.
    // Read-only formats (Excel via ExcelDataReader, HTTP GET endpoints) propagate through
    // so consumers see "directory of read-only files" as itself read-only.
    var probeExt = filePattern.StartsWith('*') ? filePattern[1..] : string.Empty;
    var probePath = SysIO.Path.Combine(directoryPath, "_traits-probe" + probeExt);
    _traits = perFileAdapter(probePath).Traits;
  }

  /// <inheritdoc/>
  public StorageTraits Traits => _traits;

  /// <inheritdoc/>
  public FlowIO<Directory<T>> Load() =>
    FlowIO.LiftAsync(
      async (CancellationToken ct) =>
      {
        if (!SysIO.Directory.Exists(_directoryPath))
          return Directory<T>.Empty;

        var entries = new Dictionary<string, T>(StringComparer.Ordinal);
        foreach (var path in EnumerateFiles())
        {
          ct.ThrowIfCancellationRequested();
          var payload = await _perFileAdapter(path).Load().Run(ct);
          entries[path] = payload;
        }
        return new Directory<T>(entries);
      }
    );

  /// <inheritdoc/>
  public FlowIO<FlowUnit> Save(Directory<T> data) =>
    FlowIO.LiftAsync(
      async (CancellationToken ct) =>
      {
        if (!_traits.CanWrite)
          throw new NotSupportedException(
            $"DirectoryStorageAdapter at '{_directoryPath}' is read-only — its per-file "
              + "adapter declares CanWrite = false. The format itself does not support "
              + "writing (e.g., Excel via ExcelDataReader)."
          );

        SysIO.Directory.CreateDirectory(_directoryPath);

        // Hard-delete existing matching files so the post-Save state matches `data` exactly.
        foreach (var existing in SysIO.Directory.EnumerateFiles(_directoryPath, _filePattern))
        {
          SysIO.File.Delete(existing);
        }

        foreach (var (path, payload) in data)
        {
          ct.ThrowIfCancellationRequested();

          // Allow callers to provide either bare keys (foo.csv) or full paths
          // (/abs/dir/foo.csv); both resolve into the directory.
          var resolvedPath = SysIO.Path.IsPathRooted(path)
            ? path
            : SysIO.Path.Combine(_directoryPath, path);

          var parent = SysIO.Path.GetDirectoryName(resolvedPath);
          if (!string.IsNullOrEmpty(parent))
            SysIO.Directory.CreateDirectory(parent);

          await _perFileAdapter(resolvedPath).Save(payload).Run(ct);
        }

        return FlowUnit.Default;
      }
    );

  /// <inheritdoc/>
  public FlowIO<bool> Exists() => FlowIO.Lift(() => SysIO.Directory.Exists(_directoryPath));

  /// <inheritdoc/>
  public FlowIO<ValidationResult> InspectShallow(int sampleSize) =>
    FlowIO.LiftAsync(
      async (CancellationToken ct) =>
      {
        if (!SysIO.Directory.Exists(_directoryPath))
        {
          return ValidationResult.Failure(
            catalogKey: SysIO.Path.GetFileName(_directoryPath),
            errorType: ValidationErrorType.NotFound,
            message: $"Directory not found: {_directoryPath}"
          );
        }

        foreach (var path in EnumerateFiles())
        {
          ct.ThrowIfCancellationRequested();
          var probe = await _perFileAdapter(path).InspectShallow(sampleSize).Run(ct);
          if (!probe.IsValid)
            return probe;
        }
        return ValidationResult.Success();
      }
    );

  /// <inheritdoc/>
  public FlowIO<ValidationResult> InspectDeep() =>
    FlowIO.LiftAsync(
      async (CancellationToken ct) =>
      {
        if (!SysIO.Directory.Exists(_directoryPath))
        {
          return ValidationResult.Failure(
            catalogKey: SysIO.Path.GetFileName(_directoryPath),
            errorType: ValidationErrorType.NotFound,
            message: $"Directory not found: {_directoryPath}"
          );
        }

        foreach (var path in EnumerateFiles())
        {
          ct.ThrowIfCancellationRequested();
          var probe = await _perFileAdapter(path).InspectDeep().Run(ct);
          if (!probe.IsValid)
            return probe;
        }
        return ValidationResult.Success();
      }
    );

  /// <inheritdoc/>
  public FlowIO<ValidationResult> InspectTarget() =>
    FlowIO.LiftAsync(ct =>
      LocalFileWriteProbe.ProbeAsync(SysIO.Path.Combine(_directoryPath, "_probe"), ct)
    );

  /// <inheritdoc/>
  public FlowIO<int> GetCountAsync() =>
    FlowIO.Lift(() =>
      SysIO.Directory.Exists(_directoryPath)
        ? SysIO.Directory.EnumerateFiles(_directoryPath, _filePattern).Count()
        : 0
    );

  private IEnumerable<string> EnumerateFiles() =>
    SysIO.Directory
      .EnumerateFiles(_directoryPath, _filePattern)
      .OrderBy(p => p, StringComparer.Ordinal);
}
