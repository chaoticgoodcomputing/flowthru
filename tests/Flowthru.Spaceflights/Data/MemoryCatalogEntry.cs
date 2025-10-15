namespace Flowthru.Data;

/// <summary>
/// In-memory catalog entry for transient data storage.
/// Used for intermediate datasets that don't need persistence.
/// 
/// <para><strong>Thread Safety:</strong></para>
/// <para>
/// Not thread-safe. Use external synchronization if accessed from multiple threads.
/// </para>
/// </summary>
/// <typeparam name="T">The data type stored in memory</typeparam>
public class MemoryCatalogEntry<T> : ICatalogEntry<T>
{
  private T? _data;
  private bool _hasData;

  public string Key { get; }

  public MemoryCatalogEntry(string key)
  {
    Key = key;
  }

  public Task<T> Load()
  {
    if (!_hasData)
    {
      throw new InvalidOperationException($"Catalog entry '{Key}' has no data. Ensure upstream nodes have executed.");
    }

    return Task.FromResult(_data!);
  }

  public Task Save(T data)
  {
    _data = data;
    _hasData = true;
    return Task.CompletedTask;
  }

  public Task<bool> Exists()
  {
    return Task.FromResult(_hasData);
  }
}
