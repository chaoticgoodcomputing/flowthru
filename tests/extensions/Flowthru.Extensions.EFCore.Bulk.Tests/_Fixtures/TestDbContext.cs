using Microsoft.EntityFrameworkCore;

namespace Flowthru.Extensions.EFCore.Bulk.Tests;

public class TestDbContext : DbContext
{
  public TestDbContext(DbContextOptions<TestDbContext> options)
    : base(options) { }

  public DbSet<TestEntity> TestEntities => Set<TestEntity>();

  protected override void OnModelCreating(ModelBuilder modelBuilder)
  {
    modelBuilder.Entity<TestEntity>().HasKey(e => e.Id);
  }
}
