using Flowthru.Prelude;
using Flowthru.Validation.Runtime;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;

namespace Flowthru.Data.Storage.EFCore;

/// <summary>
/// Extension methods on <see cref="IDbContextFactory{TContext}"/> that
/// build catalog-attachable <see cref="FlowResource{TScope}"/> values
/// for EF Core lifecycle scenarios — provisioning an ephemeral
/// database or schema for the duration of a flow run.
/// </summary>
/// <remarks>
/// <para>
/// Catalogs override <c>Resource</c> to declare a lifecycle:
/// </para>
/// <code>
/// public override IFlowResource Resource =&gt;
///   _factory.EphemeralDatabase("staging.db");
/// </code>
/// <para>
/// Provider-agnostic: acquisition runs <c>EnsureDeletedAsync</c>
/// followed by <c>EnsureCreatedAsync</c> (database mode) or
/// <c>DROP / CREATE SCHEMA</c> followed by the model's CREATE script
/// (schema mode). The release signature receives the body's primary
/// <see cref="RuntimeError"/> so <c>PreserveOnFailure</c> can keep
/// state for inspection on a failed run.
/// </para>
/// </remarks>
public static class EFCoreLifecycleExtensions
{
  /// <summary>
  /// Build a <see cref="FlowResource{TScope}"/> that provisions an
  /// ephemeral database via <typeparamref name="TContext"/> on
  /// acquire and drops it on release. Idempotent acquire — any
  /// leftover state from a previous failed run is wiped before the
  /// fresh schema is created.
  /// </summary>
  /// <typeparam name="TContext">EF Core context type.</typeparam>
  /// <param name="contextFactory">
  /// Factory used for both lifecycle operations. The factory's
  /// reference identity also keys the <see cref="DbScope.Inferred"/>
  /// returned to the body.
  /// </param>
  /// <param name="dbPath">
  /// Display path for log messages. Not used for actual deletion (EF
  /// Core's provider handles that); included so log output identifies
  /// the database under management.
  /// </param>
  /// <param name="configure">Optional configurator for <see cref="EphemeralDatabaseOptions"/>.</param>
  public static FlowResource<DbScope> EphemeralDatabase<TContext>(
    this IDbContextFactory<TContext> contextFactory,
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
          .CreateDbContextAsync(ct).ConfigureAwait(false);

        // Idempotent fresh start. Safe even if a previous run preserved
        // a debugging copy — EF Core's provider handles connection
        // pooling, file deletion, or DROP DATABASE depending on the
        // backend.
        await ctx.Database.EnsureDeletedAsync(ct).ConfigureAwait(false);
        await ctx.Database.EnsureCreatedAsync(ct).ConfigureAwait(false);

        return DbScope.Inferred(contextFactory);
      }, source: $"EphemeralDatabase[{typeof(TContext).Name}].acquire"),
      release: (_, bodyError) =>
        FlowIO.LiftAsync<FlowUnit>(async _ =>
        {
          // PreserveOnFailure: if the body produced an error and the
          // user opted in, keep the database for inspection.
          // Successful runs always drop.
          if (bodyError is not null && options.PreserveOnFailure)
          {
            return FlowUnit.Default;
          }

          // CancellationToken.None on cleanup: the bracket guarantee
          // says release runs even if the body cancelled.
          await using var ctx = await contextFactory
            .CreateDbContextAsync(CancellationToken.None).ConfigureAwait(false);
          await ctx.Database.EnsureDeletedAsync(CancellationToken.None).ConfigureAwait(false);
          return FlowUnit.Default;
        }, source: $"EphemeralDatabase[{typeof(TContext).Name}].release")
    );
  }

  /// <summary>
  /// Build a <see cref="FlowResource{TScope}"/> that provisions an
  /// ephemeral <em>schema</em> within an existing database. Use when
  /// staging and production share a single database (e.g. PostgreSQL
  /// with <c>staging</c> and <c>public</c> schemas) — keeping them
  /// under one connection unlocks server-side
  /// <c>INSERT-FROM-SELECT</c> on cross-schema promotion.
  /// </summary>
  /// <typeparam name="TContext">
  /// EF Core context configured for <paramref name="schemaName"/>
  /// (typically via <c>modelBuilder.HasDefaultSchema(schemaName)</c>).
  /// </typeparam>
  /// <param name="contextFactory">Factory producing contexts on the shared database.</param>
  /// <param name="schemaName">Name of the schema to provision and drop.</param>
  /// <param name="configure">Optional configurator for <see cref="EphemeralSchemaOptions"/>.</param>
  /// <remarks>
  /// <para>
  /// <strong>Acquire</strong> runs <c>DROP SCHEMA IF EXISTS … CASCADE;
  /// CREATE SCHEMA …;</c> then applies the model's DDL via
  /// <c>RelationalDatabaseCreator.GenerateCreateScript()</c>. The
  /// DROP makes acquire idempotent — preserved state from a previous
  /// failed run is wiped before a new run begins.
  /// </para>
  /// <para>
  /// <strong>Provider support</strong>: requires a relational provider
  /// that honours <c>CREATE SCHEMA</c> — PostgreSQL, SQL Server,
  /// Oracle. SQLite does not support schemas; use
  /// <see cref="EphemeralDatabase"/> for SQLite-backed catalogs.
  /// </para>
  /// </remarks>
  public static FlowResource<DbScope> EphemeralSchema<TContext>(
    this IDbContextFactory<TContext> contextFactory,
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
          .CreateDbContextAsync(ct).ConfigureAwait(false);
        var conn = ctx.Database.GetDbConnection();
        await conn.OpenAsync(ct).ConfigureAwait(false);

        // 1) Reset the schema. CASCADE drops any tables left from a
        //    prior PreserveOnFailure run before the model's DDL fires.
        await using (var resetCmd = conn.CreateCommand())
        {
          resetCmd.CommandText =
            $"DROP SCHEMA IF EXISTS \"{schemaName}\" CASCADE; "
            + $"CREATE SCHEMA \"{schemaName}\";";
          await resetCmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
        }

        // 2) Materialise the model's tables inside the new schema.
        //    GenerateCreateScript bypasses EnsureCreatedAsync's
        //    short-circuit when *any* table exists in the database
        //    (the normal state when production schemas already exist).
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
      }, source: $"EphemeralSchema[{typeof(TContext).Name},{schemaName}].acquire"),
      release: (_, bodyError) =>
        FlowIO.LiftAsync<FlowUnit>(async _ =>
        {
          if (bodyError is not null && options.PreserveOnFailure)
          {
            return FlowUnit.Default;
          }

          await using var ctx = await contextFactory
            .CreateDbContextAsync(CancellationToken.None).ConfigureAwait(false);
          var conn = ctx.Database.GetDbConnection();
          await conn.OpenAsync(CancellationToken.None).ConfigureAwait(false);
          await using var cmd = conn.CreateCommand();
          cmd.CommandText = $"DROP SCHEMA IF EXISTS \"{schemaName}\" CASCADE;";
          await cmd.ExecuteNonQueryAsync(CancellationToken.None).ConfigureAwait(false);
          return FlowUnit.Default;
        }, source: $"EphemeralSchema[{typeof(TContext).Name},{schemaName}].release")
    );
  }
}
