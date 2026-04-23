using Microsoft.EntityFrameworkCore;

namespace Flowthru.Extensions.EFCore.Tests;

public class TestDbContext : DbContext
{
  public TestDbContext(DbContextOptions<TestDbContext> options)
    : base(options) { }

  public DbSet<TestEntity> TestEntities => Set<TestEntity>();
  public DbSet<SourceEntity> SourceEntities => Set<SourceEntity>();

  protected override void OnModelCreating(ModelBuilder modelBuilder)
  {
    modelBuilder.Entity<TestEntity>().HasKey(e => e.Id);
    modelBuilder.Entity<SourceEntity>().HasKey(e => e.Id);
  }
}
