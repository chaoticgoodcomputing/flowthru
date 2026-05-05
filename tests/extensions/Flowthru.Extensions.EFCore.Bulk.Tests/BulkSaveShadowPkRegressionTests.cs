using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Flowthru.Extensions.EFCore.Bulk.Tests;

/// <summary>
/// Regression coverage for the shadow-PK incompatibility we hit while wiring
/// <c>BulkSave.Insert</c> through the SpaceflightsStagingSchema example.
/// </summary>
/// <remarks>
/// <para>
/// <c>EFCore.BulkExtensions</c> uses entity-property-bag lookups for the
/// primary key and does not handle EF Core's shadow-property convention
/// (<c>entity.Property&lt;int&gt;("Id")</c> + <c>entity.HasKey("Id")</c>).
/// Calling <c>BulkInsertAsync</c> on an entity whose PK is a shadow property
/// throws <see cref="KeyNotFoundException"/> with message <c>"The given key
/// 'Id' was not present in the dictionary."</c>.
/// </para>
/// <para>
/// This test pins that failure mode so future Flowthru users hit a
/// recognisable error if they regress into shadow-PK territory. The
/// resolution is to declare an explicit <c>int Id { get; init; }</c> on the
/// CLR type and let EF's convention-based PK detection take over.
/// </para>
/// </remarks>
[TestFixture]
[Category("EFCore")]
public class BulkSaveShadowPkRegressionTests
{
  private SqliteConnection _connection = null!;
  private DbContextOptions<ShadowPkContext> _options = null!;

  [SetUp]
  public async Task SetUp()
  {
    _connection = new SqliteConnection("Data Source=:memory:");
    await _connection.OpenAsync();
    _options = new DbContextOptionsBuilder<ShadowPkContext>().UseSqlite(_connection).Options;

    await using var ctx = new ShadowPkContext(_options);
    await ctx.Database.EnsureCreatedAsync();
  }

  [TearDown]
  public async Task TearDown()
  {
    await _connection.DisposeAsync();
  }

  [Test]
  public void BulkInsert_OnShadowPkEntity_FailsWithKeyNotFound()
  {
    var factory = new ShadowPkContextFactory(_options);
    var saveFunc = BulkSave.Insert<ShadowPkEntity, ShadowPkContext>();

    Assert.ThrowsAsync<KeyNotFoundException>(async () =>
    {
      await using var ctx = factory.CreateDbContext();
      await saveFunc(
        ctx,
        new[]
        {
          new ShadowPkEntity { Value = "a" },
          new ShadowPkEntity { Value = "b" },
        },
        CancellationToken.None
      );
    });
  }

  /// <summary>Entity whose PK is a shadow property — incompatible with bulk extensions.</summary>
  public class ShadowPkEntity
  {
    public string Value { get; init; } = string.Empty;
  }

  public class ShadowPkContext : DbContext
  {
    public ShadowPkContext(DbContextOptions<ShadowPkContext> options)
      : base(options) { }

    public DbSet<ShadowPkEntity> ShadowPkEntities => Set<ShadowPkEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
      modelBuilder.Entity<ShadowPkEntity>(e =>
      {
        e.Property<int>("Id");
        e.HasKey("Id");
      });
    }
  }

  public class ShadowPkContextFactory : IDbContextFactory<ShadowPkContext>
  {
    private readonly DbContextOptions<ShadowPkContext> _options;

    public ShadowPkContextFactory(DbContextOptions<ShadowPkContext> options) =>
      _options = options;

    public ShadowPkContext CreateDbContext() => new(_options);
  }
}
