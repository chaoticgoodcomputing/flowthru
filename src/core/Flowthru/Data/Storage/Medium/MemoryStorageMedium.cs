using Flowthru.Data.Capabilities;
using Flowthru.Effects;

namespace Flowthru.Data.Storage.Medium;

/// <summary>
/// Storage medium for in-memory byte storage.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Responsibility:</strong> Store data in memory without any file I/O.
/// </para>
/// <para>
/// <strong>Use Cases:</strong>
/// </para>
/// <list type="bullet">
/// <item>Testing without file system dependencies</item>
/// <item>Transient pipeline intermediates that don't need persistence</item>
/// <item>Fast prototyping and experimentation</item>
/// <item>In-memory caching of computed results</item>
/// </list>
/// <para>
/// <strong>Characteristics:</strong>
/// </para>
/// <list type="bullet">
/// <item>Never a seed (CanBeSeed = false) - always produced by pipeline</item>
/// <item>Data lost when process exits</item>
/// <item>Very fast - no I/O overhead</item>
/// <item>Memory-bound - not suitable for large datasets</item>
/// </list>
/// <para>
/// <strong>Thread Safety:</strong>
/// </para>
/// <para>
/// This class uses locking to ensure thread-safe access to the internal buffer.
/// Multiple threads can safely read/write concurrently.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var medium = new MemoryStorageMedium();
///
/// // Write some data
/// using var writeStream = new MemoryStream(Encoding.UTF8.GetBytes("Hello, World!"));
/// await medium.WriteStream(writeStream).Run();
///
/// // Read it back
/// var readResult = await medium.ReadStream().Run();
/// readResult.Match(
///     Succ: stream =>
///     {
///         using var reader = new StreamReader(stream);
///         var content = reader.ReadToEnd();
///         Console.WriteLine(content); // "Hello, World!"
///     },
///     Fail: error => Console.WriteLine($"Read failed: {error}")
/// );
/// </code>
/// </example>
public sealed class MemoryStorageMedium : IStorageMedium, ISeedable
{
  private readonly object _lock = new();
  private byte[]? _buffer;

  /// <summary>
  /// Creates a new memory storage medium with no initial data.
  /// </summary>
  public MemoryStorageMedium()
  {
    _buffer = null;
  }

  /// <summary>
  /// Creates a new memory storage medium with initial data.
  /// </summary>
  /// <param name="initialData">Initial byte buffer</param>
  /// <exception cref="ArgumentNullException">Thrown if initialData is null</exception>
  public MemoryStorageMedium(byte[] initialData)
  {
    _buffer = initialData ?? throw new ArgumentNullException(nameof(initialData));
  }

  /// <inheritdoc/>
  public FlowIO<Stream> ReadStream()
  {
    return FlowIO.Lift(() =>
    {
      lock (_lock)
      {
        if (_buffer == null)
        {
          throw new InvalidOperationException(
            "No data available in memory storage. " + "Data must be written before it can be read."
          );
        }

        // Create a new MemoryStream with a copy of the buffer
        // This ensures the caller can dispose the stream without affecting our buffer
        var bufferCopy = new byte[_buffer.Length];
        System.Array.Copy(_buffer, bufferCopy, _buffer.Length);

        return (Stream)new MemoryStream(bufferCopy, writable: false);
      }
    });
  }

  /// <inheritdoc/>
  public FlowIO<FlowUnit> WriteStream(Stream stream)
  {
    return FlowIO.LiftAsync(
      async (CancellationToken ct) =>
      {
        if (stream == null)
        {
          throw new ArgumentNullException(nameof(stream));
        }

        // Read stream into memory buffer
        using var memoryStream = new MemoryStream();
        await stream.CopyToAsync(memoryStream, ct);

        lock (_lock)
        {
          _buffer = memoryStream.ToArray();
        }

        return FlowUnit.Default;
      }
    );
  }

  /// <inheritdoc/>
  public FlowIO<bool> Exists()
  {
    return FlowIO.Lift(() =>
    {
      lock (_lock)
      {
        return _buffer != null;
      }
    });
  }

  /// <inheritdoc/>
  /// <remarks>
  /// Always returns false because memory storage is never a seed.
  /// Memory storage represents transient data produced by the pipeline,
  /// not external data that exists before pipeline execution.
  /// </remarks>
  public bool CanBeSeed => false;

  /// <summary>
  /// Gets the current buffer size in bytes, or null if no data is stored.
  /// </summary>
  public int? BufferSize
  {
    get
    {
      lock (_lock)
      {
        return _buffer?.Length;
      }
    }
  }

  /// <summary>
  /// Clears the internal buffer, freeing memory.
  /// </summary>
  public void Clear()
  {
    lock (_lock)
    {
      _buffer = null;
    }
  }
}
