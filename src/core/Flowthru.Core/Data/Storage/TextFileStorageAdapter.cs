using Flowthru.Prelude;

namespace Flowthru.Data.Storage;

/// <summary>
/// Storage adapter for plain text files with <see cref="string"/>
/// content. Direct <see cref="IStorageAdapter{T}"/> implementation —
/// the medium × format × container composition would be overkill for
/// the trivial "load file → string, save string → file" path.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Use cases.</strong> Markdown reports, configuration files,
/// plain-text logs, ad-hoc string artefacts where structuring the
/// payload through a schema record would be ceremony for no gain.
/// </para>
/// <para>
/// <strong>Inspection semantics.</strong>
/// <list type="bullet">
///   <item><see cref="InspectShallow"/> — file exists and opens.</item>
///   <item><see cref="InspectDeep"/> — file reads cleanly end-to-end.</item>
///   <item><see cref="InspectTarget"/> — write path is reachable
///     (probes the nearest existing ancestor directory for write
///     permission via <see cref="LocalFileWriteProbe"/>).</item>
/// </list>
/// </para>
/// </remarks>
public sealed class TextFileStorageAdapter : IStorageAdapter<string>, ISupportsFingerprint
{
  private readonly string _filePath;
  private readonly FileStorageMedium _fingerprintProbe;

  public TextFileStorageAdapter(string filePath)
  {
    _filePath = filePath ?? throw new ArgumentNullException(nameof(filePath));
    _fingerprintProbe = new FileStorageMedium(filePath);
  }

  /// <inheritdoc/>
  public StorageTraits Traits => new();

  /// <inheritdoc/>
  public FlowIO<string> Load() =>
    FlowIO.LiftAsync(async ct =>
    {
      if (!File.Exists(_filePath))
        throw new FileNotFoundException($"Text file not found: {_filePath}");
      return await File.ReadAllTextAsync(_filePath, ct).ConfigureAwait(false);
    }, source: $"TextFileStorageAdapter.Load[{_filePath}]");

  /// <inheritdoc/>
  public FlowIO<FlowUnit> Save(string data) =>
    FlowIO.LiftAsync(async ct =>
    {
      var directory = Path.GetDirectoryName(_filePath);
      if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);
      await File.WriteAllTextAsync(_filePath, data, ct).ConfigureAwait(false);
      return FlowUnit.Default;
    }, source: $"TextFileStorageAdapter.Save[{_filePath}]");

  /// <inheritdoc/>
  public FlowIO<bool> Exists() => FlowIO.Lift(() => File.Exists(_filePath));

  /// <inheritdoc/>
  public FlowIO<ValidationResult> InspectShallow(int sampleSize) =>
    FlowIO.LiftAsync(async ct =>
    {
      if (!File.Exists(_filePath))
        return ValidationResult.Failure(
          catalogKey: Path.GetFileName(_filePath),
          errorType: ValidationErrorType.NotFound,
          message: $"Text file not found: {_filePath}",
          details: "File does not exist or is not accessible"
        );

      try
      {
        await using var stream = File.OpenRead(_filePath);
        return ValidationResult.Success();
      }
      catch (Exception ex)
      {
        return ValidationResult.Failure(
          catalogKey: Path.GetFileName(_filePath),
          errorType: ValidationErrorType.NotFound,
          message: $"Text file is not accessible: {_filePath}",
          details: ex.Message
        );
      }
    }, source: $"TextFileStorageAdapter.InspectShallow[{_filePath}]");

  /// <inheritdoc/>
  public FlowIO<ValidationResult> InspectDeep() =>
    FlowIO.LiftAsync(async ct =>
    {
      if (!File.Exists(_filePath))
        return ValidationResult.Failure(
          catalogKey: Path.GetFileName(_filePath),
          errorType: ValidationErrorType.NotFound,
          message: $"Text file not found: {_filePath}"
        );

      try
      {
        await File.ReadAllTextAsync(_filePath, ct).ConfigureAwait(false);
        return ValidationResult.Success();
      }
      catch (Exception ex)
      {
        return ValidationResult.Failure(
          catalogKey: Path.GetFileName(_filePath),
          errorType: ValidationErrorType.DeserializationError,
          message: $"Failed to read text file: {_filePath}",
          details: ex.Message
        );
      }
    }, source: $"TextFileStorageAdapter.InspectDeep[{_filePath}]");

  /// <inheritdoc/>
  public FlowIO<ValidationResult> InspectTarget() =>
    FlowIO.LiftAsync(ct => LocalFileWriteProbe.ProbeAsync(_filePath, ct),
      source: $"TextFileStorageAdapter.InspectTarget[{_filePath}]");

  /// <inheritdoc/>
  /// <remarks>
  /// File-backed adapter — fingerprint reuses the same mtime+size
  /// derivation as <see cref="FileStorageMedium.Fingerprint"/>.
  /// </remarks>
  public FlowIO<string> Fingerprint() => _fingerprintProbe.Fingerprint();
}
