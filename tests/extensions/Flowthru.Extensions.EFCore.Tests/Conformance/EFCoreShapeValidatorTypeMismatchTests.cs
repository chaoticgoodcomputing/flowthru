using Flowthru.Core.Data.Storage;
using Flowthru.Core.Data.Validation;
using Flowthru.Extensions.EFCore.Tests.Backends;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Flowthru.Extensions.EFCore.Tests.Conformance;

/// <summary>
/// Pinned-to-Postgres regression tests for the <c>EFCoreShapeValidator</c> column-type
/// compatibility check (Phase E of the conformance-kits initiative).
/// </summary>
/// <remarks>
/// <para>
/// Motivating example: an internal pipeline bridged data between
/// <c>staging.poll_element</c> and <c>client.poll_element</c>. The staging table had
/// <c>element_type INTEGER</c>; the client table had <c>element_type client.poll_element_type</c>
/// (a native PostgreSQL enum). Both tables individually passed pre-flight (column names
/// matched, nullability matched). The bridge <c>INSERT … SELECT</c> failed at runtime with
/// PG error <c>42804: column type mismatch</c>. Pre-flight's invariant was violated.
/// </para>
/// <para>
/// Phase E added <c>IRelationalTypeMappingSource.FindMapping</c>-based type compatibility
/// to the validator. These tests pin the new check against PostgreSQL with simple type
/// mismatches that exercise the same validator code path the bug report described.
/// </para>
/// <para>
/// Tagged <c>Integration</c>: requires a Postgres container.
/// </para>
/// </remarks>
[TestFixture]
[Category("Integration")]
public class EFCoreShapeValidatorTypeMismatchTests
{
  private PostgresContainerBackend _backend = null!;
  private DbContextOptions<TypeMismatchDbContext> _options = null!;

  [OneTimeSetUp]
  public async Task SetUp()
  {
    _backend = new PostgresContainerBackend();
    var efOptions = await _backend.StartAsync();

    // PostgresContainerBackend hands back DbContextOptions<TestDbContext>; we want the
    // same connection but parameterized for our purpose-built TypeMismatchDbContext.
    var connection = new TestDbContext(efOptions).Database.GetDbConnection();
    _options = new DbContextOptionsBuilder<TypeMismatchDbContext>()
      .UseNpgsql(connection.ConnectionString)
      .Options;

    await using var context = new TypeMismatchDbContext(_options);
    // Drop EFCore's CREATE TABLE for these entities — we want to control column types
    // ourselves so the entity model intentionally diverges from the DB. Create the
    // tables via raw SQL with the "wrong" column types per scenario.
    await context.Database.OpenConnectionAsync();
    await context.Database.ExecuteSqlRawAsync(
      """
      CREATE TABLE entity_with_text_property (
        id INTEGER PRIMARY KEY,
        value INTEGER NOT NULL
      );
      CREATE TABLE entity_with_int_property (
        id INTEGER PRIMARY KEY,
        value TEXT NOT NULL
      );
      CREATE TABLE entity_with_text_property_matched (
        id INTEGER PRIMARY KEY,
        value TEXT NOT NULL
      );
      INSERT INTO entity_with_text_property (id, value) VALUES (1, 42);
      INSERT INTO entity_with_int_property (id, value) VALUES (1, 'hello');
      INSERT INTO entity_with_text_property_matched (id, value) VALUES (1, 'matched');
      """
    );
  }

  [OneTimeTearDown]
  public async Task TearDown()
  {
    await _backend.DisposeAsync();
  }

  [Test]
  public async Task EntityExpectsText_DatabaseHasInteger_FailsWithSchemaMismatch()
  {
    // The entity declares `Value` as a string mapped to `text`. The live column is
    // `INTEGER`. Pre-Phase-E this passed pre-flight (name match, NOT NULL match).
    // Post-Phase-E: SchemaMismatch on column-type comparison.
    var adapter = new EFCoreStorageAdapter<EntityWithTextProperty>(
      () => new TypeMismatchDbContext(_options)
    );

    var result = await adapter.InspectShallow(sampleSize: 10).Run();

    Assert.That(result.IsValid, Is.False, "Expected pre-flight to detect type mismatch.");
    Assert.That(
      result.Errors,
      Has.Some.Matches<ValidationError>(e =>
        e.ErrorType == ValidationErrorType.SchemaMismatch
        && e.Message.Contains("column-type mismatch", StringComparison.OrdinalIgnoreCase)
      ),
      "Expected SchemaMismatch error mentioning column-type mismatch. Got: "
        + string.Join(
          " | ",
          result.Errors.Select(e => $"[{e.ErrorType}] {e.Message}")
        )
    );
  }

  [Test]
  public async Task EntityExpectsInteger_DatabaseHasText_FailsWithSchemaMismatch()
  {
    // The reverse mismatch: entity expects `integer`, live column is `TEXT`.
    var adapter = new EFCoreStorageAdapter<EntityWithIntProperty>(
      () => new TypeMismatchDbContext(_options)
    );

    var result = await adapter.InspectShallow(sampleSize: 10).Run();

    Assert.That(result.IsValid, Is.False);
    Assert.That(
      result.Errors,
      Has.Some.Matches<ValidationError>(e =>
        e.ErrorType == ValidationErrorType.SchemaMismatch
        && e.Message.Contains("column-type mismatch", StringComparison.OrdinalIgnoreCase)
      )
    );
  }

  [Test]
  public async Task EntityAndDatabaseAgreeOnTypes_PassesPreFlight()
  {
    // Sanity: when entity store type and live column type resolve to the same canonical
    // mapping, no SchemaMismatch is raised. Guards against false positives.
    var adapter = new EFCoreStorageAdapter<EntityWithTextPropertyMatched>(
      () => new TypeMismatchDbContext(_options)
    );

    var result = await adapter.InspectShallow(sampleSize: 10).Run();

    Assert.That(
      result.IsValid,
      Is.True,
      "Matching types should pass pre-flight. Errors: "
        + string.Join(
          " | ",
          result.Errors.Select(e => $"[{e.ErrorType}] {e.Message}")
        )
    );
  }

  // ── Test entities + DbContext (purpose-built — separate from TestDbContext) ──

  public class EntityWithTextProperty
  {
    public int Id { get; set; }
    public string Value { get; set; } = string.Empty;
  }

  public class EntityWithIntProperty
  {
    public int Id { get; set; }
    public int Value { get; set; }
  }

  public class EntityWithTextPropertyMatched
  {
    public int Id { get; set; }
    public string Value { get; set; } = string.Empty;
  }

  public class TypeMismatchDbContext : DbContext
  {
    public TypeMismatchDbContext(DbContextOptions<TypeMismatchDbContext> options)
      : base(options) { }

    public DbSet<EntityWithTextProperty> EntitiesWithText => Set<EntityWithTextProperty>();
    public DbSet<EntityWithIntProperty> EntitiesWithInt => Set<EntityWithIntProperty>();
    public DbSet<EntityWithTextPropertyMatched> EntitiesMatched =>
      Set<EntityWithTextPropertyMatched>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
      ConfigureEntity<EntityWithTextProperty>(modelBuilder, "entity_with_text_property", "text");
      ConfigureEntity<EntityWithIntProperty>(modelBuilder, "entity_with_int_property", "integer");
      ConfigureEntity<EntityWithTextPropertyMatched>(
        modelBuilder,
        "entity_with_text_property_matched",
        "text"
      );
    }

    private static void ConfigureEntity<T>(
      ModelBuilder modelBuilder,
      string tableName,
      string valueStoreType
    )
      where T : class
    {
      modelBuilder.Entity<T>(b =>
      {
        b.ToTable(tableName);
        b.HasKey("Id");
        b.Property("Id").HasColumnName("id").HasColumnType("integer");
        b.Property("Value").HasColumnName("value").HasColumnType(valueStoreType);
      });
    }
  }
}
