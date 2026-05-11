using Flowthru.Data.Storage.EFCore.Internal;
using Flowthru.Prelude;
using Flowthru.Validation.PreFlight;
using Flowthru.Validation.Runtime;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Flowthru.Hosting;

/// <summary>
/// Extension methods on <see cref="IFlowthruBuilder"/> that register
/// EFCore-specific <see cref="IRegistrationValidationHook"/>
/// implementations. Each registers a Core registration-validation
/// hook that runs at the first <c>IFlowthruService.RunAsync</c>
/// (or eagerly via
/// <see cref="IFlowthruService.ValidateRegistrationAsync"/>) — so
/// host misconfiguration surfaces at host startup, not at first
/// flow execution.
/// </summary>
public static class EFCoreFlowthruBuilderExtensions
{
  /// <summary>
  /// Register a hook that opens a connection probe for
  /// <typeparamref name="TContext"/> at host startup. Catches
  /// "connection string is malformed" and "the database engine is
  /// unreachable" before any flow runs.
  /// </summary>
  /// <typeparam name="TContext">The configured EF Core context type.</typeparam>
  /// <param name="builder">The Flowthru builder.</param>
  /// <param name="hookId">
  /// Optional explicit hook id. Defaults to
  /// <c>"EFCore.Connection[TContext]"</c> so failures are
  /// attributable in the host's diagnostic surface.
  /// </param>
  public static IFlowthruBuilder VerifyEFCoreConnection<TContext>(
    this IFlowthruBuilder builder,
    string? hookId = null
  )
    where TContext : DbContext
  {
    if (builder is null) throw new ArgumentNullException(nameof(builder));
    var id = hookId ?? $"EFCore.Connection[{typeof(TContext).Name}]";

    return builder.RegisterValidationHook(id, services =>
      FlowIO.LiftAsync<Validated<PreFlightError, FlowUnit>>(async ct =>
      {
        var factory = services.GetService<IDbContextFactory<TContext>>();
        if (factory is null)
        {
          return Validated<PreFlightError, FlowUnit>.Fail(
            new PreFlightError.RegistrationCheckFailed(
              HookId: id,
              CheckMessage: $"IDbContextFactory<{typeof(TContext).Name}> is not registered in DI",
              Details: "Call services.AddDbContextFactory<...>(...) before AddFlowthru."
            )
          );
        }

        try
        {
          await using var ctx = await factory.CreateDbContextAsync(ct).ConfigureAwait(false);
          // CanConnectAsync is the lightest connection probe EF Core
          // exposes — opens a connection, runs a trivial query, closes.
          var canConnect = await ctx.Database.CanConnectAsync(ct).ConfigureAwait(false);
          if (!canConnect)
          {
            return Validated<PreFlightError, FlowUnit>.Fail(
              new PreFlightError.RegistrationCheckFailed(
                HookId: id,
                CheckMessage: $"DbContext '{typeof(TContext).Name}' could not connect to its database",
                Details: GetConnectionDescription(ctx)
              )
            );
          }
          return Validated<PreFlightError, FlowUnit>.Pure(FlowUnit.Default);
        }
        catch (Exception ex)
        {
          return Validated<PreFlightError, FlowUnit>.Fail(
            new PreFlightError.RegistrationCheckFailed(
              HookId: id,
              CheckMessage: $"Connection probe for '{typeof(TContext).Name}' threw: {ex.Message}",
              Details: ex.GetType().Name
            )
          );
        }
      }, source: id)
    );
  }

  /// <summary>
  /// Register a hook that builds the EF Core model for
  /// <typeparamref name="TContext"/> and validates entity
  /// configuration — every <c>DbSet</c> has a key, no array keys, no
  /// orphaned mappings. Catches model-build errors at startup rather
  /// than at first adapter construction, which can be deep inside
  /// catalog wire-up.
  /// </summary>
  public static IFlowthruBuilder VerifyEFCoreConfiguration<TContext>(
    this IFlowthruBuilder builder,
    string? hookId = null
  )
    where TContext : DbContext
  {
    if (builder is null) throw new ArgumentNullException(nameof(builder));
    var id = hookId ?? $"EFCore.Configuration[{typeof(TContext).Name}]";

    return builder.RegisterValidationHook(id, services =>
      FlowIO.LiftAsync<Validated<PreFlightError, FlowUnit>>(async ct =>
      {
        var factory = services.GetService<IDbContextFactory<TContext>>();
        if (factory is null)
        {
          return Validated<PreFlightError, FlowUnit>.Fail(
            new PreFlightError.RegistrationCheckFailed(
              HookId: id,
              CheckMessage: $"IDbContextFactory<{typeof(TContext).Name}> is not registered in DI"
            )
          );
        }

        try
        {
          await using var ctx = await factory.CreateDbContextAsync(ct).ConfigureAwait(false);
          var findings = new List<PreFlightError>();

          // Force the model to build — surfaces orphaned mappings,
          // configuration mismatches, conflicting fluent API calls.
          var model = ctx.Model;

          foreach (var entityType in model.GetEntityTypes())
          {
            // Skip owned entities — they don't declare their own
            // key independent of the owner.
            if (entityType.IsOwned())
            {
              continue;
            }

            var primaryKey = entityType.FindPrimaryKey();
            if (primaryKey is null)
            {
              findings.Add(new PreFlightError.RegistrationCheckFailed(
                HookId: id,
                CheckMessage: $"Entity '{entityType.ClrType.Name}' has no primary key configured"
              ));
              continue;
            }

            var arrayKey = primaryKey.Properties.FirstOrDefault(p => p.ClrType.IsArray);
            if (arrayKey is not null)
            {
              findings.Add(new PreFlightError.RegistrationCheckFailed(
                HookId: id,
                CheckMessage: $"Entity '{entityType.ClrType.Name}' uses array property "
                  + $"'{arrayKey.Name}' as a key — arrays use reference equality and break "
                  + "EF Core change tracking. Use a primitive key or a composite of primitives."
              ));
            }
          }

          return findings.Count == 0
            ? Validated<PreFlightError, FlowUnit>.Pure(FlowUnit.Default)
            : Validated<PreFlightError, FlowUnit>.Fail(findings);
        }
        catch (Exception ex)
        {
          return Validated<PreFlightError, FlowUnit>.Fail(
            new PreFlightError.RegistrationCheckFailed(
              HookId: id,
              CheckMessage: $"Model build for '{typeof(TContext).Name}' threw: {ex.Message}",
              Details: ex.GetType().Name
            )
          );
        }
      }, source: id)
    );
  }

  /// <summary>
  /// Register a hook that runs the shape validator for every
  /// configured entity in <typeparamref name="TContext"/> against the
  /// live database — catches column drift (missing columns, NULL on
  /// NOT-NULL, type mismatches) at startup. More expensive than
  /// <see cref="VerifyEFCoreConnection{TContext}"/> /
  /// <see cref="VerifyEFCoreConfiguration{TContext}"/>; opt in for
  /// production / CI runs where catching schema drift before any
  /// flow runs is worth the connection cost.
  /// </summary>
  public static IFlowthruBuilder VerifyEFCoreSchema<TContext>(
    this IFlowthruBuilder builder,
    string? hookId = null
  )
    where TContext : DbContext
  {
    if (builder is null) throw new ArgumentNullException(nameof(builder));
    var id = hookId ?? $"EFCore.Schema[{typeof(TContext).Name}]";

    return builder.RegisterValidationHook(id, services =>
      FlowIO.LiftAsync<Validated<PreFlightError, FlowUnit>>(async ct =>
      {
        var factory = services.GetService<IDbContextFactory<TContext>>();
        if (factory is null)
        {
          return Validated<PreFlightError, FlowUnit>.Fail(
            new PreFlightError.RegistrationCheckFailed(
              HookId: id,
              CheckMessage: $"IDbContextFactory<{typeof(TContext).Name}> is not registered in DI"
            )
          );
        }

        try
        {
          await using var ctx = await factory.CreateDbContextAsync(ct).ConfigureAwait(false);
          var findings = new List<PreFlightError>();

          foreach (var entityType in ctx.Model.GetEntityTypes())
          {
            if (entityType.IsOwned() || entityType.FindPrimaryKey() is null)
            {
              continue;
            }
            if (string.IsNullOrEmpty(entityType.GetTableName()))
            {
              continue;
            }

            var shapeResult = await EFCoreShapeValidator
              .ValidateAsync(ctx, entityType.ClrType, entityType.ClrType.Name, ct)
              .ConfigureAwait(false);

            if (!shapeResult.IsValid)
            {
              foreach (var error in shapeResult.Errors)
              {
                findings.Add(new PreFlightError.RegistrationCheckFailed(
                  HookId: id,
                  CheckMessage: $"{entityType.ClrType.Name}: {error.Message}",
                  Details: error.Details
                ));
              }
            }
          }

          return findings.Count == 0
            ? Validated<PreFlightError, FlowUnit>.Pure(FlowUnit.Default)
            : Validated<PreFlightError, FlowUnit>.Fail(findings);
        }
        catch (Exception ex)
        {
          return Validated<PreFlightError, FlowUnit>.Fail(
            new PreFlightError.RegistrationCheckFailed(
              HookId: id,
              CheckMessage: $"Schema probe for '{typeof(TContext).Name}' threw: {ex.Message}",
              Details: ex.GetType().Name
            )
          );
        }
      }, source: id)
    );
  }

  private static string GetConnectionDescription(DbContext context)
  {
    try
    {
      var conn = context.Database.GetDbConnection();
      var dataSource = conn.DataSource;
      var database = conn.Database;
      return string.IsNullOrEmpty(dataSource) ? database : $"{dataSource}/{database}";
    }
    catch
    {
      return "(connection info unavailable)";
    }
  }
}
