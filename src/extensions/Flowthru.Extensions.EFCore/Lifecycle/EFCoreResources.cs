using Flowthru.Core.Effects;
using Flowthru.Extensions.EFCore.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;

namespace Flowthru.Extensions.EFCore.Lifecycle;

/// <summary>
/// Catalog-attachable <see cref="FlowResource{TScope}"/> factories for EF Core
/// databases. Use these from a <c>CatalogAbstract.Resource</c> override to
/// declare an ephemeral database lifecycle managed by the Flowthru framework.
/// </summary>
/// <remarks>
/// <para>
/// Provider-agnostic by design — acquisition uses EF Core's
/// <c>Database.EnsureCreatedAsync</c> and release uses
/// <c>Database.EnsureDeletedAsync</c>.
/// For SQLite this drops/creates the database file; for PostgreSQL or SQL
/// Server it drops/creates the database. The mechanism follows the provider.
/// </para>
/// <para>
/// Acquire is idempotent: any existing database state from a previous failed
/// run is dropped before a fresh schema is created. This makes the
/// <see cref="EphemeralDatabaseOptions.PreserveOnFailure"/> debugging path
/// safe — preserved state is wiped at the start of the next successful run.
/// </para>
/// </remarks>
public static class EFCoreResources
{
  /// <summary>
  /// Builds a <see cref="FlowResource{DbScope}"/> that provisions an ephemeral
  /// database via <typeparamref name="TContext"/> and tears it down on flow
  /// exit. The returned scope is suitable for use with
  /// <c>EFCoreItemFactory.Query.EFCore</c> on the same factory instance —
  /// catalog items will share the resource's database.
  /// </summary>
  /// <typeparam name="TContext">EF Core context type.</typeparam>
  /// <param name="contextFactory">
  /// Factory used to create contexts for both lifecycle operations. The
  /// factory's identity also keys the <see cref="DbScope.Inferred(object)"/>
  /// returned to consumers.
  /// </param>
  /// <param name="dbPath">
  /// Display path for log messages. Not used for actual deletion (EF Core's
  /// provider handles that); included so log output identifies the database
  /// being managed.
  /// </param>
  /// <param name="configure">Optional configurator for <see cref="EphemeralDatabaseOptions"/>.</param>
  public static FlowResource<DbScope> EphemeralDatabase<TContext>(
    IDbContextFactory<TContext> contextFactory,
    string dbPath,
    Action<EphemeralDatabaseOptions>? configure = null
  )
    where TContext : DbContext
  {
    ArgumentNullException.ThrowIfNull(contextFactory);
    ArgumentNullException.ThrowIfNull(dbPath);

    var options = new EphemeralDatabaseOptions();
    configure?.Invoke(options);

    return FlowResource.Make<DbScope>(
      acquire: FlowIO.LiftAsync<DbScope>(async ct =>
      {
        await using var ctx = await contextFactory
          .CreateDbContextAsync(ct)
          .ConfigureAwait(false);

        // Idempotent fresh start — safe even if the previous run preserved
        // a debugging copy. EF Core's provider handles connection pooling,
        // file deletion, or DROP DATABASE depending on the backend.
        await ctx.Database.EnsureDeletedAsync(ct).ConfigureAwait(false);
        await ctx.Database.EnsureCreatedAsync(ct).ConfigureAwait(false);

        return DbScope.Inferred(contextFactory);
      }),
      release: (_, bodyException) =>
        FlowIO.LiftAsync<FlowUnit>(async _ =>
        {
          // PreserveOnFailure: if the body threw and the user opted in, keep
          // the database for inspection. Successful runs always drop.
          if (bodyException is not null && options.PreserveOnFailure)
          {
            return FlowUnit.Default;
          }

          // Pass CancellationToken.None so the cleanup runs even when the
          // caller cancelled the flow — that's the bracket guarantee.
          await using var ctx = await contextFactory
            .CreateDbContextAsync(CancellationToken.None)
            .ConfigureAwait(false);
          await ctx.Database.EnsureDeletedAsync(CancellationToken.None).ConfigureAwait(false);
          return FlowUnit.Default;
        })
    );
  }

  /// <summary>
  /// Builds a <see cref="FlowResource{DbScope}"/> that provisions an ephemeral
  /// <em>schema</em> within an existing database. Use this when staging and
  /// production share a single database (e.g., PostgreSQL with
  /// <c>staging</c> and <c>public</c> schemas) — keeping them under one
  /// connection unlocks server-side <c>INSERT-FROM-SELECT</c> on
  /// cross-schema promotion via the same <see cref="DbScope"/>.
  /// </summary>
  /// <typeparam name="TContext">
  /// EF Core context type whose model is configured for
  /// <paramref name="schemaName"/> (typically via
  /// <c>modelBuilder.HasDefaultSchema(schemaName)</c>).
  /// </typeparam>
  /// <param name="contextFactory">Factory producing contexts on the shared database.</param>
  /// <param name="schemaName">Name of the schema to provision and drop.</param>
  /// <param name="configure">Optional configurator for <see cref="EphemeralSchemaOptions"/>.</param>
  /// <remarks>
  /// <para>
  /// <strong>Acquire</strong>: <c>DROP SCHEMA IF EXISTS … CASCADE; CREATE SCHEMA …;</c>
  /// then applies the model's DDL (via
  /// <c>RelationalDatabaseCreator.GenerateCreateScript()</c>) so tables
  /// declared by <typeparamref name="TContext"/> land inside the freshly
  /// created schema. The DROP makes acquire idempotent — preserved state from
  /// a previous failed run is wiped before a new run begins.
  /// </para>
  /// <para>
  /// <strong>Release</strong>: <c>DROP SCHEMA … CASCADE</c>, unconditional
  /// unless <see cref="EphemeralSchemaOptions.PreserveOnFailure"/> is set and
  /// the body threw.
  /// </para>
  /// <para>
  /// <strong>Provider support</strong>: requires a relational provider that
  /// honors <c>CREATE SCHEMA</c> — PostgreSQL, SQL Server, Oracle. SQLite
  /// does not support schemas; use <see cref="EphemeralDatabase{TContext}"/>
  /// for SQLite-backed catalogs.
  /// </para>
  /// </remarks>
  public static FlowResource<DbScope> EphemeralSchema<TContext>(
    IDbContextFactory<TContext> contextFactory,
    string schemaName,
    Action<EphemeralSchemaOptions>? configure = null
  )
    where TContext : DbContext
  {
    ArgumentNullException.ThrowIfNull(contextFactory);
    if (string.IsNullOrWhiteSpace(schemaName))
    {
      throw new ArgumentException("Schema name must be non-empty.", nameof(schemaName));
    }

    var options = new EphemeralSchemaOptions();
    configure?.Invoke(options);

    return FlowResource.Make<DbScope>(
      acquire: FlowIO.LiftAsync<DbScope>(async ct =>
      {
        await using var ctx = await contextFactory
          .CreateDbContextAsync(ct)
          .ConfigureAwait(false);
        var conn = ctx.Database.GetDbConnection();
        await conn.OpenAsync(ct).ConfigureAwait(false);

        // 1) Reset the schema. CASCADE drops any tables left from a prior
        //    PreserveOnFailure run before the model's DDL fires below.
        await using (var resetCmd = conn.CreateCommand())
        {
          resetCmd.CommandText =
            $"DROP SCHEMA IF EXISTS \"{schemaName}\" CASCADE; "
            + $"CREATE SCHEMA \"{schemaName}\";";
          await resetCmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
        }

        // 2) Materialize the model's tables inside the new schema. We use
        //    GenerateCreateScript explicitly rather than EnsureCreatedAsync
        //    because EnsureCreated short-circuits when *any* table exists in
        //    the database — which is the normal state when production
        //    schemas already exist.
        var creator = (RelationalDatabaseCreator)ctx.GetService<IDatabaseCreator>();
        var ddl = creator.GenerateCreateScript();
        if (options.DdlFilter is not null)
        {
          ddl = options.DdlFilter(ddl);
        }

        await using (var ddlCmd = conn.CreateCommand())
        {
          ddlCmd.CommandText = ddl;
          await ddlCmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
        }

        return DbScope.Inferred(contextFactory);
      }),
      release: (_, bodyException) =>
        FlowIO.LiftAsync<FlowUnit>(async _ =>
        {
          if (bodyException is not null && options.PreserveOnFailure)
          {
            return FlowUnit.Default;
          }

          await using var ctx = await contextFactory
            .CreateDbContextAsync(CancellationToken.None)
            .ConfigureAwait(false);
          var conn = ctx.Database.GetDbConnection();
          await conn.OpenAsync(CancellationToken.None).ConfigureAwait(false);
          await using var cmd = conn.CreateCommand();
          cmd.CommandText = $"DROP SCHEMA IF EXISTS \"{schemaName}\" CASCADE;";
          await cmd.ExecuteNonQueryAsync(CancellationToken.None).ConfigureAwait(false);
          return FlowUnit.Default;
        })
    );
  }
}
