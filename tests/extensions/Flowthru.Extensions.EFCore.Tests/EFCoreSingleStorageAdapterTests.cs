using Flowthru.Core.Data.Validation;
using Flowthru.Extensions.EFCore.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Flowthru.Extensions.EFCore.Tests;

[TestFixture]
public class EFCoreSingleStorageAdapterTests
{
  private SqliteConnection _connection = null!;
  private DbContextOptions<TestDbContext> _options = null!;

  [SetUp]
  public async Task SetUp()
  {
    _connection = new SqliteConnection("Data Source=:memory:");
    await _connection.OpenAsync();
    _options = new DbContextOptionsBuilder<TestDbContext>().UseSqlite(_connection).Options;

    await using var context = new TestDbContext(_options);
    await context.Database.EnsureCreatedAsync();
  }

  [TearDown]
  public async Task TearDown()
  {
    await _connection.DisposeAsync();
  }

  [Test]
  public async Task DefaultRoundTrip_SaveAndLoad_ReturnsSingleEntity()
  {
    var testEntity = new TestEntity { Id = 1, Name = "Alice" };

    var entry = EFCoreItemFactory.Single.EFCore<TestEntity>(
      "test",
      () => new TestDbContext(_options)
    );

    await entry.Save(testEntity).Run();
    var loaded = await entry.Load().Run();

    Assert.That(loaded.Id, Is.EqualTo(1));
    Assert.That(loaded.Name, Is.EqualTo("Alice"));
  }

  [Test]
  public async Task AllowEmptyData_False_FailsInspectionOnEmptyTable()
  {
    var entry = EFCoreItemFactory.Single.EFCore<TestEntity>(
      "test",
      () => new TestDbContext(_options),
      allowEmptyData: false
    );

    var result = await entry.InspectShallow(0).Run();

    Assert.That(result.IsValid, Is.False);
  }

  // ── InspectTarget ───────────────────────────────────────────────────────

  [Test]
  public async Task InspectTarget_MigratedDatabase_ReturnsSuccess()
  {
    var entry = EFCoreItemFactory.Single.EFCore<TestEntity>(
      "test",
      () => new TestDbContext(_options)
    );

    var result = await entry.InspectTarget().Run();

    Assert.That(result.IsValid, Is.True);
  }

  [Test]
  public async Task InspectTarget_UnmigratedDatabase_ReturnsFailure()
  {
    await using var bareConnection = new SqliteConnection("Data Source=:memory:");
    await bareConnection.OpenAsync();
    var bareOptions = new DbContextOptionsBuilder<TestDbContext>()
      .UseSqlite(bareConnection)
      .Options;

    var entry = EFCoreItemFactory.Single.EFCore<TestEntity>(
      "test",
      () => new TestDbContext(bareOptions)
    );

    var result = await entry.InspectTarget().Run();

    Assert.That(result.IsValid, Is.False);
  }

  [Test]
  public async Task InspectTarget_UnmigratedDatabase_ErrorDetailsContainContextTypeName()
  {
    await using var bareConnection = new SqliteConnection("Data Source=:memory:");
    await bareConnection.OpenAsync();
    var bareOptions = new DbContextOptionsBuilder<TestDbContext>()
      .UseSqlite(bareConnection)
      .Options;

    var entry = EFCoreItemFactory.Single.EFCore<TestEntity>(
      "test",
      () => new TestDbContext(bareOptions)
    );

    var result = await entry.InspectTarget().Run();

    Assert.That(
      result.Errors[0].Details,
      Does.Contain("TestDbContext"),
      "Error details must identify which context type produced the error"
    );
  }

  [Test]
  public async Task AllowEmptyData_True_PassesInspectionOnEmptyTable()
  {
    var entry = EFCoreItemFactory.Single.EFCore<TestEntity>(
      "test",
      () => new TestDbContext(_options),
      allowEmptyData: true
    );

    var result = await entry.InspectShallow(0).Run();

    Assert.That(result.IsValid, Is.True);
  }

  [Test]
  public void ArrayKeyEntity_ThrowsInvalidOperationException_OnConstruction()
  {
    var options = new DbContextOptionsBuilder<ArrayKeyDbContext>()
      .UseSqlite("Data Source=:memory:")
      .Options;

    Assert.Throws<InvalidOperationException>(
      () =>
        EFCoreItemFactory.Single.EFCore<ArrayKeyEntity>(
          "test",
          () => new ArrayKeyDbContext(options)
        )
    );
  }

  // ── Shape validation ────────────────────────────────────────────────────

  private static async Task<(SqliteConnection conn, DbContextOptions<TestDbContext> options)> CreateDriftedDatabaseAsync(
    string testEntitiesTableSql
  )
  {
    var conn = new SqliteConnection("Data Source=:memory:");
    await conn.OpenAsync();

    await using (var cmd = conn.CreateCommand())
    {
      cmd.CommandText = testEntitiesTableSql;
      await cmd.ExecuteNonQueryAsync();
    }

    var options = new DbContextOptionsBuilder<TestDbContext>().UseSqlite(conn).Options;
    return (conn, options);
  }

  [Test]
  public async Task InspectTarget_TableMissingColumn_ReturnsSchemaMismatch()
  {
    var (conn, options) = await CreateDriftedDatabaseAsync(
      "CREATE TABLE \"TestEntities\" (\"Id\" INTEGER PRIMARY KEY)"
    );
    await using (conn)
    {
      var entry = EFCoreItemFactory.Single.EFCore<TestEntity>(
        "test",
        () => new TestDbContext(options)
      );

      var result = await entry.InspectTarget().Run();

      Assert.That(result.IsValid, Is.False);
      Assert.That(result.Errors[0].ErrorType, Is.EqualTo(ValidationErrorType.SchemaMismatch));
      Assert.That(result.Errors[0].Message, Does.Contain("Name"));
    }
  }

  [Test]
  public async Task InspectShallow_TableMissingColumn_ReturnsSchemaMismatch()
  {
    var (conn, options) = await CreateDriftedDatabaseAsync(
      "CREATE TABLE \"TestEntities\" (\"Id\" INTEGER PRIMARY KEY)"
    );
    await using (conn)
    {
      // allowEmptyData: true so the empty-table check doesn't pre-empt the
      // shape check we're trying to exercise.
      var entry = EFCoreItemFactory.Single.EFCore<TestEntity>(
        "test",
        () => new TestDbContext(options),
        allowEmptyData: true
      );

      var result = await entry.InspectShallow(0).Run();

      Assert.That(result.IsValid, Is.False);
      Assert.That(result.Errors[0].ErrorType, Is.EqualTo(ValidationErrorType.SchemaMismatch));
      Assert.That(result.Errors[0].Message, Does.Contain("Name"));
    }
  }
}
