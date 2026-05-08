using Flowthru.Data.Storage;
using Flowthru.Tests.Kits.Storage;

namespace Flowthru.Core.Tests.Storage;

/// <summary>
/// Exercises <see cref="IStorageAdapterLaws{T}"/> against
/// <see cref="ComposedStorageAdapter{TContainer, TRow}"/> with the
/// canonical Core composition: <see cref="FileStorageMedium"/> +
/// <see cref="JsonFormatSerializer{TRow}"/> +
/// <see cref="EnumerableContainerAdapter{T}"/>.
/// </summary>
[TestFixture]
public class ComposedStorageAdapterLaws : IStorageAdapterLaws<IEnumerable<TestRow>>
{
  private string _tempDir = null!;

  [SetUp]
  public void SetUp()
  {
    _tempDir = Path.Combine(Path.GetTempPath(), $"flowthru-adapterlaws-{Guid.NewGuid():N}");
    Directory.CreateDirectory(_tempDir);
  }

  [TearDown]
  public void TearDown()
  {
    if (Directory.Exists(_tempDir))
    {
      try
      {
        Directory.Delete(_tempDir, recursive: true);
      }
      catch
      {
        // Best-effort.
      }
    }
  }

  protected override IStorageAdapter<IEnumerable<TestRow>> CreateWellFormed()
  {
    var path = Path.Combine(_tempDir, $"adapter-{Guid.NewGuid():N}.json");
    return new ComposedStorageAdapter<IEnumerable<TestRow>, TestRow>(
      new FileStorageMedium(path),
      new JsonFormatSerializer<TestRow>(),
      new EnumerableContainerAdapter<TestRow>()
    );
  }

  protected override IStorageAdapter<IEnumerable<TestRow>> CreateMissingSource()
  {
    var path = Path.Combine(_tempDir, $"missing-{Guid.NewGuid():N}.json");
    return new ComposedStorageAdapter<IEnumerable<TestRow>, TestRow>(
      new FileStorageMedium(path),
      new JsonFormatSerializer<TestRow>(),
      new EnumerableContainerAdapter<TestRow>()
    );
  }

  protected override IEnumerable<TestRow> SampleData =>
    new[]
    {
      new TestRow { Id = 1, Name = "alpha" },
      new TestRow { Id = 2, Name = "beta" },
    };

  protected override IEqualityComparer<IEnumerable<TestRow>>? Comparer =>
    new TestRowEnumerableComparer();

  private sealed class TestRowEnumerableComparer : IEqualityComparer<IEnumerable<TestRow>>
  {
    public bool Equals(IEnumerable<TestRow>? x, IEnumerable<TestRow>? y)
    {
      if (x is null || y is null)
      {
        return x is null && y is null;
      }
      var lx = x.ToList();
      var ly = y.ToList();
      if (lx.Count != ly.Count)
      {
        return false;
      }
      for (int i = 0; i < lx.Count; i++)
      {
        if (lx[i].Id != ly[i].Id || lx[i].Name != ly[i].Name)
        {
          return false;
        }
      }
      return true;
    }

    public int GetHashCode(IEnumerable<TestRow> obj) => obj?.Count() ?? 0;
  }
}
