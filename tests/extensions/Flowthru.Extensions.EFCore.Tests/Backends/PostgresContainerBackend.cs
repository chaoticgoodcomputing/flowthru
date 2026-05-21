using System.Collections.Concurrent;
using Flowthru.Data.Storage.EFCore;
using Flowthru.Extensions.EFCore.Tests.Fixtures;
using Flowthru.Prelude;
using Flowthru.Tests.Kits.Prelude;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

namespace Flowthru.Extensions.EFCore.Tests.Backends;

/// <summary>
/// Backend for <see cref="EFCoreResourceLaws{TBackend}"/> targeting a
/// per-fixture <c>Testcontainers</c>-managed PostgreSQL container.
/// Each <see cref="CreateResource"/> call provisions a fresh schema
/// inside the shared container via
/// <see cref="EFCoreLifecycleExtensions.EphemeralSchema"/>; the container
/// itself is started in <see cref="InitializeAsync"/> (after the
/// capability gate clears) and disposed at fixture teardown.
/// </summary>
/// <remarks>
/// <para>
/// Declares <see cref="TestCapabilities.Docker"/> as a required
/// capability — when Docker is unavailable the laws kit's
/// <c>OneTimeSetUp</c> yields Inconclusive and the container is never
/// started.
/// </para>
/// <para>
/// Tagged <c>[Category("RequiresDocker")]</c> so CI matrices that
/// run only the Docker-equipped tier can target this backend
/// explicitly. The category is informational; the
/// <see cref="RequiredCapabilities"/> gate is the load-bearing check.
/// </para>
/// </remarks>
[Category("RequiresDocker")]
public sealed class PostgresContainerBackend : IEphemeralResourceBackend<BackendScope<DbScope>>
{
  private PostgreSqlContainer? _container;
  private readonly ConcurrentBag<string> _schemasToDrop = new();

  private string? _pendingSeed;
  private string? _lastSchema;

  public IReadOnlyList<TestCapability> RequiredCapabilities { get; } = [TestCapabilities.Docker];

  public string ExternalStateIdentifier(BackendScope<DbScope> scope) => scope.ExternalStateId;

  public async Task InitializeAsync()
  {
    var c = new PostgreSqlBuilder()
      .WithImage("postgres:16-alpine")
      .Build();
    await c.StartAsync();
    _container = c;
  }

  public FlowResource<BackendScope<DbScope>> CreateResource(bool preserveOnFailure)
  {
    if (_container is null)
    {
      throw new InvalidOperationException(
        "PostgresContainerBackend.CreateResource() called before InitializeAsync(). "
          + "The laws kit's OneTimeSetUp wires this automatically — invocations from "
          + "outside the kit must call InitializeAsync() first."
      );
    }

    var schema = Interlocked.Exchange(ref _pendingSeed, null) ?? GenerateFreshSchemaName();
    Interlocked.Exchange(ref _lastSchema, schema);
    _schemasToDrop.Add(schema);

    var factory = BuildFactory(_container);
    var inner = factory.EphemeralSchema(schema, opts =>
    {
      opts.PreserveOnFailure = preserveOnFailure;
      // The laws verify schema acquire/release lifecycle, not table
      // contents. Replace the model DDL with a no-op so the model's
      // tables don't pollute the connection's default schema across
      // calls. Empty string would crash Npgsql's ExecuteNonQuery; a
      // SELECT keeps the round trip valid.
      opts.DdlFilter = _ => "SELECT 1;";
    });

    return FlowResource.Make<BackendScope<DbScope>>(
      acquire: inner.Acquire.Map(scope => new BackendScope<DbScope>(scope, schema)),
      release: (wrapped, err) => inner.Release(wrapped.Inner, err)
    );
  }

  public async Task<bool> ResourceExists()
  {
    var schema = _lastSchema;
    if (schema is null || _container is null) return false;
    return await SchemaExistsAsync(_container, schema);
  }

  public async Task SeedLeftoverState()
  {
    if (_container is null) return;
    var schema = GenerateFreshSchemaName();
    await using (var conn = new Npgsql.NpgsqlConnection(_container.GetConnectionString()))
    {
      await conn.OpenAsync();
      await using var cmd = conn.CreateCommand();
      cmd.CommandText = $"CREATE SCHEMA IF NOT EXISTS \"{schema}\";";
      await cmd.ExecuteNonQueryAsync();
    }
    _schemasToDrop.Add(schema);
    Interlocked.Exchange(ref _lastSchema, schema);
    Interlocked.Exchange(ref _pendingSeed, schema);
  }

  public async Task<IPeerStateProbe?> CreatePeerState()
  {
    if (_container is null) return null;
    var peerSchema = GenerateFreshSchemaName("peer");
    await using (var conn = new Npgsql.NpgsqlConnection(_container.GetConnectionString()))
    {
      await conn.OpenAsync();
      await using var cmd = conn.CreateCommand();
      cmd.CommandText = $"CREATE SCHEMA IF NOT EXISTS \"{peerSchema}\";";
      await cmd.ExecuteNonQueryAsync();
    }
    return new PgPeerProbe(_container, peerSchema);
  }

  public async Task Cleanup()
  {
    if (_container is not null)
    {
      foreach (var schema in _schemasToDrop)
      {
        try
        {
          await using var conn = new Npgsql.NpgsqlConnection(_container.GetConnectionString());
          await conn.OpenAsync();
          await using var cmd = conn.CreateCommand();
          cmd.CommandText = $"DROP SCHEMA IF EXISTS \"{schema}\" CASCADE;";
          await cmd.ExecuteNonQueryAsync();
        }
        catch { /* best effort */ }
      }
      await _container.DisposeAsync();
      _container = null;
    }
    _lastSchema = null;
    _pendingSeed = null;
  }

  private static string GenerateFreshSchemaName(string suffix = "main") =>
    $"laws_{suffix}_{Guid.NewGuid():N}";

  private static IDbContextFactory<TestDbContext> BuildFactory(PostgreSqlContainer container)
  {
    var options = new DbContextOptionsBuilder<TestDbContext>()
      .UseNpgsql(container.GetConnectionString())
      .Options;
    return new TestPostgresFactory(options);
  }

  private static async Task<bool> SchemaExistsAsync(PostgreSqlContainer container, string schema)
  {
    await using var conn = new Npgsql.NpgsqlConnection(container.GetConnectionString());
    await conn.OpenAsync();
    await using var cmd = conn.CreateCommand();
    cmd.CommandText = "SELECT 1 FROM information_schema.schemata WHERE schema_name = @s;";
    var p = cmd.CreateParameter();
    p.ParameterName = "s";
    p.Value = schema;
    cmd.Parameters.Add(p);
    var result = await cmd.ExecuteScalarAsync();
    return result is not null;
  }

  /// <summary>
  /// Minimal <see cref="IDbContextFactory{TContext}"/> over the shared
  /// container's connection. EphemeralSchema's DDL is stripped via
  /// <see cref="EphemeralSchemaOptions.DdlFilter"/>, so this factory
  /// doesn't need a per-schema model override.
  /// </summary>
  private sealed class TestPostgresFactory(DbContextOptions<TestDbContext> options)
    : IDbContextFactory<TestDbContext>
  {
    public TestDbContext CreateDbContext() => new(options);
  }

  private sealed class PgPeerProbe(PostgreSqlContainer container, string schema) : IPeerStateProbe
  {
    public async Task<bool> StillExists() => await SchemaExistsAsync(container, schema);

    public async ValueTask DisposeAsync()
    {
      try
      {
        await using var conn = new Npgsql.NpgsqlConnection(container.GetConnectionString());
        await conn.OpenAsync();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = $"DROP SCHEMA IF EXISTS \"{schema}\" CASCADE;";
        await cmd.ExecuteNonQueryAsync();
      }
      catch { /* best effort */ }
    }
  }
}
