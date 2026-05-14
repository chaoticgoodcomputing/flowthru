using System.Security.Cryptography;
using System.Text;
using SysIO = System.IO;

namespace Flowthru.Data.Storage;

/// <summary>
/// Format-agnostic storage adapter for a <see cref="DirectoryOf{T}"/>
/// of same-schema files. Format concerns are externalised through
/// <c>perFileAdapter</c>: the directory owns enumeration, save
/// ordering, and target validation; the per-file adapter owns
/// serialisation for one path.
/// </summary>
/// <typeparam name="T">
/// Payload type for each file (e.g., <c>byte[]</c>,
/// <c>IEnumerable&lt;TRow&gt;</c>, <c>TDoc</c>).
/// </typeparam>
/// <remarks>
/// <para>
/// <strong>Save semantics:</strong> existing files matching the
/// adapter's <c>filePattern</c> are deleted before write, then each
/// entry is written via its per-file adapter. Re-runs are
/// deterministic — the directory state after Save matches the
/// <see cref="DirectoryOf{T}"/> that produced it.
/// </para>
/// <para>
/// <strong>Load semantics:</strong> files matching <c>filePattern</c>
/// are enumerated in ordinal-string order and loaded via per-file
/// adapters. Keys in the resulting <see cref="DirectoryOf{T}"/> are the
/// full file paths.
/// </para>
/// </remarks>
public sealed class DirectoryStorageAdapter<T>
  : IStorageAdapter<DirectoryOf<T>>, IHasEfficientCount, ISupportsFingerprint
{
  private readonly string _directoryPath;
  private readonly string _filePattern;
  private readonly Func<string, IStorageAdapter<T>> _perFileAdapter;
  private readonly StorageTraits _traits;

  /// <summary>
  /// Initialises the adapter.
  /// </summary>
  /// <param name="directoryPath">Path to the directory.</param>
  /// <param name="filePattern">Glob for matching files (e.g. <c>"*.json"</c>).</param>
  /// <param name="perFileAdapter">
  /// Factory that, given a file path, returns the storage adapter
  /// for one file. Called once per file on Load (with paths from
  /// the glob) and once per entry on Save (with paths from the
  /// <see cref="DirectoryOf{T}"/>'s keys).
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

    // Inherit traits (CanWrite, etc.) from a probe per-file adapter so
    // a directory of read-only files itself reports as read-only.
    var probeExt = filePattern.StartsWith('*') ? filePattern[1..] : string.Empty;
    var probePath = SysIO.Path.Combine(directoryPath, "_traits-probe" + probeExt);
    _traits = perFileAdapter(probePath).Traits;
  }

  /// <inheritdoc/>
  public StorageTraits Traits => _traits;

  /// <inheritdoc/>
  public FlowIO<DirectoryOf<T>> Load() =>
    FlowIO.Lift(() => SysIO.Directory.Exists(_directoryPath))
      .Bind(exists =>
        !exists
          ? FlowIO.Pure(DirectoryOf<T>.Empty)
          : FlowIO.Lift(() => EnumerateFiles().ToList())
              .Bind(LoadAllFiles)
      );

  /// <summary>
  /// Sequentially load each path through its per-file adapter and
  /// fold the results into a <see cref="DirectoryOf{T}"/>. Per-file
  /// failures propagate through <see cref="FlowIO{A}.Bind"/> as
  /// typed <see cref="RuntimeError"/> values — no throw-recapture
  /// at the directory boundary, so a downstream consumer can still
  /// pattern-match on the original error variant.
  /// </summary>
  private FlowIO<DirectoryOf<T>> LoadAllFiles(IReadOnlyList<string> paths)
  {
    var seed = FlowIO.Pure(new Dictionary<string, T>(StringComparer.Ordinal));
    var folded = paths.Aggregate(seed, (accIO, path) =>
      from acc in accIO
      from value in _perFileAdapter(path).Load()
      select WithEntry(acc, path, value)
    );
    return folded.Map(dict => new DirectoryOf<T>(dict));
  }

  private static Dictionary<string, T> WithEntry(
    Dictionary<string, T> dict, string key, T value
  )
  {
    // Local mutation contained to the fold; the dictionary is built
    // up once and only observed at the end via Map.
    dict[key] = value;
    return dict;
  }

  /// <inheritdoc/>
  public FlowIO<FlowUnit> Save(DirectoryOf<T> data)
  {
    // Pre-IO setup (existence check, CanWrite probe, hard-delete of
    // existing matching files) runs in a single Lift block; per-entry
    // saves chain through Bind so each per-file Failure surfaces as
    // a typed RuntimeError, not a wrapped IOException.
    return FlowIO.Lift<FlowUnit>(() =>
      {
        if (!_traits.CanWrite)
        {
          throw new NotSupportedException(
            $"DirectoryStorageAdapter at '{_directoryPath}' is read-only — its per-file "
            + "adapter declares CanWrite = false."
          );
        }
        SysIO.Directory.CreateDirectory(_directoryPath);
        foreach (var existing in SysIO.Directory.EnumerateFiles(_directoryPath, _filePattern))
        {
          SysIO.File.Delete(existing);
        }
        return FlowUnit.Default;
      },
      source: $"DirectoryStorageAdapter.Save.PreIO[{_directoryPath}]"
    ).Bind(_ => SaveAllEntries(data));
  }

  /// <summary>
  /// Sequentially save each (path, payload) pair through its
  /// per-file adapter. Per-file failures short-circuit the chain
  /// and propagate the typed inner error verbatim.
  /// </summary>
  private FlowIO<FlowUnit> SaveAllEntries(DirectoryOf<T> data) =>
    data.Aggregate(
      FlowIO.Pure(FlowUnit.Default),
      (accIO, kvp) =>
        from _ in accIO
        from saved in SaveOneEntry(kvp.Key, kvp.Value)
        select FlowUnit.Default
    );

  private FlowIO<FlowUnit> SaveOneEntry(string path, T payload)
  {
    // Allow bare keys (foo.json) or full paths (/abs/dir/foo.json);
    // both resolve into the configured directory. Parent-directory
    // creation is sync IO done in Lift before delegating to the
    // per-file adapter's Save.
    var resolvedPath = SysIO.Path.IsPathRooted(path)
      ? path
      : SysIO.Path.Combine(_directoryPath, path);

    return FlowIO.Lift(
      () =>
      {
        var parent = SysIO.Path.GetDirectoryName(resolvedPath);
        if (!string.IsNullOrEmpty(parent)) SysIO.Directory.CreateDirectory(parent);
        return resolvedPath;
      },
      source: $"DirectoryStorageAdapter.Save.MkParent[{resolvedPath}]"
    ).Bind(p => _perFileAdapter(p).Save(payload));
  }

  /// <inheritdoc/>
  public FlowIO<bool> Exists() => FlowIO.Lift(() => SysIO.Directory.Exists(_directoryPath));

  /// <inheritdoc/>
  public FlowIO<ValidationResult> InspectShallow(int sampleSize = 100) =>
    FlowIO.LiftAsync(async ct =>
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
        var probeIO = _perFileAdapter(path).InspectShallow(sampleSize);
        var probe = await probeIO.Run(ct).ConfigureAwait(false);
        if (probe is EffResult<ValidationResult>.Success ok && !ok.Value.IsValid) return ok.Value;
      }
      return ValidationResult.Success();
    });

  /// <inheritdoc/>
  public FlowIO<ValidationResult> InspectDeep() =>
    FlowIO.LiftAsync(async ct =>
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
        var probe = await _perFileAdapter(path).InspectDeep().Run(ct).ConfigureAwait(false);
        if (probe is EffResult<ValidationResult>.Success ok && !ok.Value.IsValid) return ok.Value;
      }
      return ValidationResult.Success();
    });

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

  /// <inheritdoc/>
  /// <remarks>
  /// <para>
  /// Composes per-file fingerprints (mtime+size) under a single
  /// SHA-256 digest. The fingerprint changes when any file is added,
  /// removed, renamed, or modified.
  /// </para>
  /// <para>
  /// When the directory is missing, the fingerprint surfaces a
  /// FlowIO failure so the cache plan records "fingerprint unknown"
  /// for the dependent step. An empty directory still produces a
  /// stable fingerprint (the empty-set hash).
  /// </para>
  /// </remarks>
  public FlowIO<string> Fingerprint() =>
    FlowIO.Lift(
      () =>
      {
        if (!SysIO.Directory.Exists(_directoryPath))
        {
          throw new SysIO.DirectoryNotFoundException(
            $"Cannot fingerprint directory '{_directoryPath}': directory does not exist."
          );
        }
        var builder = new StringBuilder();
        foreach (var path in EnumerateFiles())
        {
          var info = new SysIO.FileInfo(path);
          builder.Append(SysIO.Path.GetFileName(path));
          builder.Append(':');
          builder.Append(info.LastWriteTimeUtc.Ticks);
          builder.Append(':');
          builder.Append(info.Length);
          builder.Append('\n');
        }
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(builder.ToString()));
        return Convert.ToHexString(bytes).ToLowerInvariant();
      },
      source: $"DirectoryStorageAdapter.Fingerprint[{_directoryPath}]"
    );
}
