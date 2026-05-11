namespace Flowthru.Data.Storage;

/// <summary>
/// Storage medium — abstracts <em>where</em> bytes live (filesystem,
/// memory, network, database). Composes with <see cref="IFormatSerializer{TRow}"/>
/// (HOW bytes serialize) and <see cref="IContainerAdapter{TContainer, TRow}"/>
/// (WHAT in-memory representation) to form a complete
/// <see cref="IStorageAdapter{T}"/>.
/// </summary>
/// <remarks>
/// <para>
/// All operations return <see cref="FlowIO{A}"/> effects — lazy, async,
/// cancellable, and failure-as-value.
/// </para>
/// <para>
/// Mediums that target databases, HTTP endpoints, or other "non-bytestream"
/// sources may not fit this interface; those extensions implement
/// <see cref="IStorageAdapter{T}"/> directly (see EFCore in §1.2).
/// </para>
/// </remarks>
public interface IStorageMedium
{
  /// <summary>
  /// Capability matrix for this medium. Composed with format and container
  /// traits to derive the adapter-level <see cref="StorageTraits"/>.
  /// </summary>
  StorageTraits Traits { get; }

  /// <summary>
  /// Reads raw bytes from storage as a stream. The returned stream is
  /// positioned at the beginning; the caller disposes it.
  /// </summary>
  FlowIO<Stream> ReadStream();

  /// <summary>
  /// Writes raw bytes to storage from the supplied stream. Implementations
  /// should strive for atomic writes (write to temp, then rename) to avoid
  /// partial writes on failure.
  /// </summary>
  FlowIO<FlowUnit> WriteStream(Stream stream);

  /// <summary>
  /// True if data exists at this storage location. Used to distinguish
  /// "seed" inputs from items produced by the pipeline.
  /// </summary>
  FlowIO<bool> Exists();

  /// <summary>
  /// Probes whether this medium is accessible as a write destination.
  /// Default: success. Override for mediums that can meaningfully probe
  /// write access ahead of execution (e.g., filesystem path checks).
  /// </summary>
  FlowIO<ValidationResult> InspectTarget() =>
    FlowIO<ValidationResult>.Pure(ValidationResult.Success());
}
