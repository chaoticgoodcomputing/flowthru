using Flowthru.Core.Data.Storage;

namespace Flowthru.Tests.Kits.Storage;

/// <summary>
/// Cross-check that an <see cref="IStorageAdapter{T}"/>'s declared
/// <see cref="StorageTraits"/> match observed behavior. Subclasses inherit this in addition
/// to (not instead of) <see cref="StorageAdapterConformance{T}"/>.
/// </summary>
/// <typeparam name="T">The adapter's container type.</typeparam>
/// <remarks>
/// Subclasses follow the same <c>[TestFixtureSource]</c> + constructor pattern as
/// <see cref="StorageAdapterConformance{T}"/>. Typically a subclass uses the same fixture
/// list for both bases.
/// </remarks>
public abstract class StorageAdapterTraitsConformance<T>
{
  protected string FixturePath { get; }

  protected T FixtureData { get; private set; } = default!;

  protected StorageAdapterTraitsConformance(string fixturePath)
  {
    FixturePath = fixturePath;
  }

  [OneTimeSetUp]
  public void LoadFixtureData()
  {
    FixtureData = LoadFixture(FixturePath);
  }

  /// <summary>Builds a well-formed adapter against the fixture data.</summary>
  protected abstract IStorageAdapter<T> CreateAdapter(T data);

  /// <summary>Loads a JSON fixture into the adapter's container type.</summary>
  protected abstract T LoadFixture(string fixturePath);

  [Test]
  public async Task Traits_CanWrite_TrueImpliesSaveSucceeds()
  {
    var adapter = CreateAdapter(FixtureData);

    if (!adapter.Traits.CanWrite)
    {
      Assert.Pass(
        "Adapter declares CanWrite = false; save behavior is covered by "
          + nameof(Traits_CanWrite_FalseImpliesInspectTargetTriviallySucceeds)
          + "."
      );
    }

    await adapter.Save(FixtureData).Run();
  }

  [Test]
  public async Task Traits_CanWrite_FalseImpliesInspectTargetTriviallySucceeds()
  {
    var adapter = CreateAdapter(FixtureData);

    if (adapter.Traits.CanWrite)
    {
      Assert.Pass(
        "Adapter declares CanWrite = true; covered by "
          + nameof(Traits_CanWrite_TrueImpliesSaveSucceeds)
          + "."
      );
    }

    var result = await adapter.InspectTarget().Run();
    Assert.That(
      result.IsValid,
      Is.True,
      "Read-only adapter (CanWrite=false) should report InspectTarget as trivially valid, "
        + $"but got {result.ErrorCount} error(s)."
    );
  }

  [Test]
  public async Task Traits_CanRead_TrueImpliesLoadSucceeds()
  {
    var adapter = CreateAdapter(FixtureData);

    if (!adapter.Traits.CanRead)
    {
      Assert.Pass("Adapter declares CanRead = false; load behavior is not exercised here.");
    }

    _ = await adapter.Load().Run();
  }
}
