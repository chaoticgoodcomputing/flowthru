using Flowthru.Core.Data.Capabilities;
using Flowthru.Core.Data.Validation;
using Flowthru.Core.Effects;

namespace Flowthru.Core.Data.Storage;

/// <summary>
/// Abstract base for read-only storage adapters that aggregate all files of a given
/// pattern within a directory into a single item sequence.
/// </summary>
/// <typeparam name="TItem">The item type yielded per file (or per row within a file).</typeparam>
/// <remarks>
/// <para>
/// Owns all directory-as-medium concerns: existence checks, lexicographic file
/// enumeration, <see cref="Save"/> refusal, and pre-flight validation scaffolding.
/// Subclasses implement two abstract members that encode the format-specific behavior:
/// </para>
/// <list type="bullet">
/// <item>
///   <see cref="LoadFile"/> — deserialize one file into an async stream of
///   <typeparamref name="TItem"/> values.
/// </item>
/// <item>
///   <see cref="ValidateFileAsync"/> — probe one file at a given sample depth and
///   return a <see cref="ValidationResult"/>.
/// </item>
/// </list>
/// <para>
/// <strong>Inspection semantics:</strong>
/// <see cref="InspectShallow"/> applies a per-file sample to <em>every</em> file in
/// the directory, returning the first failure encountered.
/// <see cref="InspectDeep"/> applies an unbounded scan to every file.
/// </para>
/// </remarks>
public abstract class ReadOnlyDirectoryStorageAdapter<TItem> : IStorageAdapter<IEnumerable<TItem>>
{
  private readonly string _catalogKey;

  /// <summary>The path to the directory managed by this adapter.</summary>
  protected readonly string DirectoryPath;

  /// <summary>The glob pattern used to select eligible files (e.g. <c>*.csv</c>).</summary>
  protected readonly string FilePattern;

  /// <summary>Initializes the adapter.</summary>
  /// <param name="directoryPath">Path to the directory.</param>
  /// <param name="filePattern">Glob pattern for eligible files (e.g. <c>"*.csv"</c>).</param>
  /// <param name="catalogKey">Key used in directory-level validation error reports.</param>
  protected ReadOnlyDirectoryStorageAdapter(
    string directoryPath,
    string filePattern,
    string catalogKey
  )
  {
    if (string.IsNullOrWhiteSpace(directoryPath))
    {
      throw new ArgumentException(
        "Directory path cannot be null or whitespace.",
        nameof(directoryPath)
      );
    }

    DirectoryPath = directoryPath;
    FilePattern = filePattern;
    _catalogKey = catalogKey;
  }

  /// <inheritdoc/>
  public virtual StorageTraits Traits => new StorageTraits { CanWrite = false };

  /// <inheritdoc/>
  public FlowIO<IEnumerable<TItem>> Load()
  {
    return FlowIO.LiftAsync(
      async (CancellationToken ct) =>
      {
        var allItems = new List<TItem>();

        foreach (var filePath in GetFiles())
        {
          ct.ThrowIfCancellationRequested();

          await foreach (var item in LoadFile(filePath, ct))
          {
            allItems.Add(item);
          }
        }

        return (IEnumerable<TItem>)allItems;
      }
    );
  }

  /// <inheritdoc/>
  /// <remarks>Always fails — this adapter is read-only.</remarks>
  public FlowIO<FlowUnit> Save(IEnumerable<TItem> data) =>
    FlowIO.Fail<FlowUnit>(
      new NotSupportedException(
        $"{GetType().Name} is read-only. "
          + $"Directory '{DirectoryPath}' cannot be written to via this catalog entry."
      )
    );

  /// <inheritdoc/>
  public FlowIO<bool> Exists() =>
    FlowIO.Lift(() => Directory.Exists(DirectoryPath) && GetFiles().Any());

  /// <inheritdoc/>
  /// <remarks>
  /// Validates the first <paramref name="sampleSize"/> items from <em>every</em> file in
  /// the directory. Returns the first failure encountered across all files.
  /// </remarks>
  public FlowIO<ValidationResult> InspectShallow(int sampleSize)
  {
    return FlowIO.LiftAsync(
      async (CancellationToken ct) =>
      {
        var directoryCheck = ValidateDirectoryNotEmpty();
        if (!directoryCheck.IsValid)
        {
          return directoryCheck;
        }

        foreach (var filePath in GetFiles())
        {
          ct.ThrowIfCancellationRequested();
          var result = await ValidateFileAsync(filePath, sampleSize, ct);
          if (!result.IsValid)
          {
            return result;
          }
        }

        return ValidationResult.Success();
      }
    );
  }

  /// <inheritdoc/>
  /// <remarks>
  /// Validates all items in every file. Equivalent to <see cref="InspectShallow"/>
  /// with an unbounded sample depth.
  /// </remarks>
  public FlowIO<ValidationResult> InspectDeep()
  {
    return FlowIO.LiftAsync(
      async (CancellationToken ct) =>
      {
        var directoryCheck = ValidateDirectoryNotEmpty();
        if (!directoryCheck.IsValid)
        {
          return directoryCheck;
        }

        foreach (var filePath in GetFiles())
        {
          ct.ThrowIfCancellationRequested();
          var result = await ValidateFileAsync(filePath, sampleSize: 0, ct);
          if (!result.IsValid)
          {
            return result;
          }
        }

        return ValidationResult.Success();
      }
    );
  }

  /// <inheritdoc/>
  /// <remarks>This adapter is read-only; there is no write destination to validate.</remarks>
  public FlowIO<ValidationResult> InspectTarget() => FlowIO.Pure(ValidationResult.Success());

  // ── Abstract members ──────────────────────────────────────────────────────

  /// <summary>
  /// Deserializes one file into an async stream of <typeparamref name="TItem"/> values.
  /// </summary>
  /// <param name="filePath">Absolute or relative path to the file.</param>
  /// <param name="ct">Cancellation token.</param>
  protected abstract IAsyncEnumerable<TItem> LoadFile(string filePath, CancellationToken ct);

  /// <summary>
  /// Validates one file at the given sample depth.
  /// </summary>
  /// <param name="filePath">The file to validate.</param>
  /// <param name="sampleSize">
  /// Maximum items to read; <c>0</c> means read all items (used by
  /// <see cref="InspectDeep"/>).
  /// </param>
  /// <param name="ct">Cancellation token.</param>
  protected abstract Task<ValidationResult> ValidateFileAsync(
    string filePath,
    int sampleSize,
    CancellationToken ct
  );

  // ── Protected helpers ─────────────────────────────────────────────────────

  /// <summary>
  /// Enumerates eligible files in the directory in lexicographic order.
  /// Returns an empty sequence if the directory does not exist.
  /// </summary>
  protected IEnumerable<string> GetFiles() =>
    Directory.Exists(DirectoryPath)
      ? Directory
        .EnumerateFiles(DirectoryPath, FilePattern, SearchOption.TopDirectoryOnly)
        .OrderBy(f => f, StringComparer.OrdinalIgnoreCase)
      : Enumerable.Empty<string>();

  // ── Private helpers ───────────────────────────────────────────────────────

  private ValidationResult ValidateDirectoryNotEmpty()
  {
    if (!Directory.Exists(DirectoryPath))
    {
      return ValidationResult.Failure(
        catalogKey: _catalogKey,
        errorType: ValidationErrorType.NotFound,
        message: $"Directory '{DirectoryPath}' does not exist.",
        details: "Ensure the directory is present before running the pipeline."
      );
    }

    if (!GetFiles().Any())
    {
      return ValidationResult.Failure(
        catalogKey: _catalogKey,
        errorType: ValidationErrorType.NotFound,
        message: $"No {FilePattern} files found in '{DirectoryPath}'.",
        details: $"Directory exists but contains no matching {FilePattern} files."
      );
    }

    return ValidationResult.Success();
  }
}
