using Flowthru.Core.Abstractions;

namespace Flowthru.Core.Data.Storage.Strategies;

/// <summary>
/// In-memory storage strategy for unit tests.
/// </summary>
/// <remarks>
/// <para>
/// Stores all data in memory for:
/// </para>
/// <list type="bullet">
/// <item>Fast test execution (no I/O)</item>
/// <item>Test isolation (no shared state between tests)</item>
/// <item>Simple setup (no files or databases)</item>
/// </list>
/// <para>
/// <strong>Usage in Tests:</strong>
/// </para>
/// <code>
/// [Test]
/// public async Task MyTest()
/// {
///     var storage = new MemoryStorageEntryFactory();
///     var catalog = new MyCatalog(storage);
///
///     // All data stays in memory - no files created
///     await catalog.Companies.Save(companies).Run();
///     var result = await catalog.Companies.Load().Run();
/// }
/// </code>
/// </remarks>
public sealed class MemoryStorageEntryFactory : IStorageEntryFactory
{
  /// <inheritdoc />
  public IItem<IEnumerable<T>> CreateEnumerable<T>(string label, StorageOptions? options = null)
    where T : notnull, IFlatSchema, ITextSerializable
  {
    return ItemFactory.Enumerable.Memory<T>(label);
  }

  /// <inheritdoc />
  public IItem<T> CreateSingle<T>(string label, StorageOptions? options = null)
    where T : IStructuredSerializable
  {
    return ItemFactory.Single.Memory<T>(label);
  }
}
