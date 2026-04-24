using Microsoft.EntityFrameworkCore;

namespace Flowthru.Extensions.EFCore.Bulk.Tests;

public class TestDbContextFactory : IDbContextFactory<TestDbContext>
{
  private readonly DbContextOptions<TestDbContext> _options;

  public TestDbContextFactory(DbContextOptions<TestDbContext> options)
  {
    _options = options;
  }

  public TestDbContext CreateDbContext() => new TestDbContext(_options);
}
