using Flowthru.Core.Effects;
using Flowthru.Extensions.EFCore.Data;
using Microsoft.EntityFrameworkCore;

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
}
