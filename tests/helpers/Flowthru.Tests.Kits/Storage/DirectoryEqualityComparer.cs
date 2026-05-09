using Flowthru.Core.Data;

namespace Flowthru.Tests.Kits.Storage;

/// <summary>
/// Equality comparer for <see cref="DirectoryOf{T}"/> that normalises keys to filename-only
/// before comparing entries. Round-trips through the directory adapter typically write with
/// short keys (e.g. <c>"foo.csv"</c>) and read back with absolute paths
/// (<c>"/abs/dir/foo.csv"</c>); the comparer treats those as equivalent so subclasses can
/// pass a fixture <c>DirectoryOf{T}</c> through <c>SaveAndLoad_RoundTrips</c> unchanged.
/// </summary>
/// <typeparam name="TInner">The per-file payload type — compared element-wise via
/// <paramref name="innerComparer"/> when supplied, otherwise via the type's default
/// equality.</typeparam>
public sealed class DirectoryEqualityComparer<TInner> : IEqualityComparer<DirectoryOf<TInner>>
{
  private readonly IEqualityComparer<TInner> _innerComparer;

  public DirectoryEqualityComparer(IEqualityComparer<TInner>? innerComparer = null)
  {
    _innerComparer = innerComparer ?? EqualityComparer<TInner>.Default;
  }

  public bool Equals(DirectoryOf<TInner>? x, DirectoryOf<TInner>? y)
  {
    if (x is null || y is null)
      return ReferenceEquals(x, y);
    if (x.Count != y.Count)
      return false;

    var yByFileName = new Dictionary<string, TInner>(StringComparer.Ordinal);
    foreach (var kvp in y)
      yByFileName[Path.GetFileName(kvp.Key)] = kvp.Value;

    foreach (var kvp in x)
    {
      var fileName = Path.GetFileName(kvp.Key);
      if (!yByFileName.TryGetValue(fileName, out var rhs))
        return false;
      if (!_innerComparer.Equals(kvp.Value, rhs))
        return false;
    }
    return true;
  }

  public int GetHashCode(DirectoryOf<TInner> obj) => obj.Count;
}
