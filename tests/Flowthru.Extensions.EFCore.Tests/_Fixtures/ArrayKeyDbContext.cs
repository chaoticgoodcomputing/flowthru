using Microsoft.EntityFrameworkCore;

namespace Flowthru.Extensions.EFCore.Tests;

public class ArrayKeyDbContext : DbContext
{
    public ArrayKeyDbContext(DbContextOptions<ArrayKeyDbContext> options)
      : base(options) { }

    public DbSet<ArrayKeyEntity> ArrayKeyEntities => Set<ArrayKeyEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ArrayKeyEntity>().HasKey(e => e.Id);
    }
}
