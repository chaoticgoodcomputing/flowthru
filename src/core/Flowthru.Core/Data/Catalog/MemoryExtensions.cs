using Flowthru.Data.Storage;

namespace Flowthru.Data.Catalog;

/// <summary>
/// In-memory item-builder extensions on <see cref="ItemAnchor{T}"/>.
/// Memory items work for any <c>T</c> — single value
/// or collection alike — so a single overload suffices.
/// </summary>
public static class MemoryExtensions
{
  /// <summary>
  /// Build an in-memory catalog item carrying a value of type
  /// <typeparamref name="T"/>. No path, no IO — the adapter owns the
  /// value lifetime in process memory.
  /// </summary>
  public static MemoryBuilder<T> Memory<T>(this ItemAnchor<T> anchor) where T : notnull =>
    new(anchor.Label);
}

/// <summary>Tier-1 builder for an in-memory catalog item.</summary>
public sealed class MemoryBuilder<T> where T : notnull
{
  private readonly string _label;

  internal MemoryBuilder(string label)
  {
    _label = label;
  }

  /// <summary>Materialise the in-memory <see cref="IItem{T}"/>.</summary>
  public IItem<T> Build() =>
    new Item<T>(_label, new MemoryStorageAdapter<T>());
}
