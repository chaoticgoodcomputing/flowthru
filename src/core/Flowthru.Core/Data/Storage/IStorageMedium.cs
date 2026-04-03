using Flowthru.Data.Capabilities;
using Flowthru.Effects;

namespace Flowthru.Data.Storage;

/// <summary>
/// Interface for storage medium - handles raw byte stream I/O.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Responsibility:</strong> Abstract WHERE data is stored (file system, memory, network, database).
/// </para>
/// <para>
/// <strong>Separation of Concerns:</strong>
/// </para>
/// <para>
/// The storage medium layer is isolated from:
/// - Format serialization (CSV, JSON, Parquet) - handled by <see cref="IFormatSerializer{TRow}"/>
/// - Container representation (IEnumerable, IDataView) - handled by <see cref="IContainerAdapter{TContainer, TRow}"/>
/// </para>
/// <para>
/// <strong>Design Pattern:</strong>
/// </para>
/// <para>
/// This is the lowest layer in the composition pattern:
/// </para>
/// <code>
/// Medium (bytes) → Format (rows) → Container (in-memory)
/// File/Memory    → CSV/JSON      → IEnumerable/IDataView
/// </code>
/// <para>
/// <strong>Effect Types:</strong>
/// </para>
/// <para>
/// All operations return <see cref="IO{A}"/> effects to represent:
/// - I/O operations that can fail
/// - Async execution
/// - Cancellation support
/// - Functional composition
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // File storage medium
/// var fileMedium = new FileStorageMedium("data/file.csv");
/// var readResult = await fileMedium.ReadStream().Run();
///
/// // Memory storage medium
/// var memoryMedium = new MemoryStorageMedium();
/// var writeResult = await memoryMedium.WriteStream(stream).Run();
/// </code>
/// </example>
public interface IStorageMedium
{
  /// <summary>
  /// Structural constraints and capabilities of this storage medium.
  /// </summary>
  /// <remarks>
  /// Medium traits focus on WHERE data is stored and the access patterns it supports.
  /// For composed adapters, these traits are merged with format and container traits.
  /// </remarks>
  StorageTraits Traits { get; }

  /// <summary>
  /// Reads raw bytes from storage as a stream.
  /// </summary>
  /// <returns>Effect that produces a readable stream on success</returns>
  /// <remarks>
  /// <para>
  /// The returned stream should be positioned at the beginning and ready to read.
  /// The caller is responsible for disposing the stream.
  /// </para>
  /// <para>
  /// <strong>Error Conditions:</strong>
  /// </para>
  /// <list type="bullet">
  /// <item>Storage location does not exist</item>
  /// <item>Access denied (permissions)</item>
  /// <item>Network failure (for remote storage)</item>
  /// <item>I/O error</item>
  /// </list>
  /// </remarks>
  FlowIO<Stream> ReadStream();

  /// <summary>
  /// Writes raw bytes to storage from a stream.
  /// </summary>
  /// <param name="stream">Stream containing data to write</param>
  /// <returns>Effect that completes on successful write</returns>
  /// <remarks>
  /// <para>
  /// The stream will be read from its current position to the end.
  /// The implementation should handle creating parent directories if needed.
  /// </para>
  /// <para>
  /// <strong>Atomicity:</strong>
  /// </para>
  /// <para>
  /// Implementations should strive for atomic writes (write to temp, then rename)
  /// to avoid partial writes on failure.
  /// </para>
  /// <para>
  /// <strong>Error Conditions:</strong>
  /// </para>
  /// <list type="bullet">
  /// <item>Insufficient disk space</item>
  /// <item>Access denied (permissions)</item>
  /// <item>Network failure (for remote storage)</item>
  /// <item>I/O error</item>
  /// </list>
  /// </remarks>
  FlowIO<FlowUnit> WriteStream(Stream stream);

  /// <summary>
  /// Checks if data exists at this storage location.
  /// </summary>
  /// <returns>Effect that produces true if data exists, false otherwise</returns>
  /// <remarks>
  /// <para>
  /// This is used to determine if a catalog entry is a "seed" (Layer 0 input)
  /// or if it's produced by a step in the pipeline.
  /// </para>
  /// </remarks>
  FlowIO<bool> Exists();
}
