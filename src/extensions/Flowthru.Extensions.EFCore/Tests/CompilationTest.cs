using Flowthru.Data;
using Flowthru.Extensions.EFCore.Data;
using Microsoft.EntityFrameworkCore;

namespace Flowthru.Extensions.EFCore.Tests;

/// <summary>
/// Minimal compilation test to verify extension pattern works.
/// </summary>
public class CompilationTest
{
  public void PartialClassExtensionWorks()
  {
    // This code should compile if extension pattern is working
    DbContext? context = null;

    // Extension method from Flowthru.Extensions.EFCore
    var entry = EFCoreCatalogEntries.Enumerable.EFCore<TestEntity>("test", context!);

    // Verify it returns the correct type
    var _ = entry as ICatalogEntry<IEnumerable<TestEntity>>;
  }

  private class TestEntity
  {
    public int Id { get; set; }
  }
}
