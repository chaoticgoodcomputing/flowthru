using Flowthru.Data.Storage.EFCore.Internal;
using Flowthru.Prelude;
using Flowthru.Validation.Runtime;
using Microsoft.EntityFrameworkCore;

namespace Flowthru.Data.Storage.EFCore;

/// <summary>
/// Storage adapter for an Entity Framework Core entity collection,
/// exposed as <see cref="IEnumerable{T}"/>. Implements
/// <see cref="IStorageAdapter{T}"/> directly rather than going through
/// the Medium → Format → Container composition pattern: EF Core
/// inherently couples connection (where), entity mapping (how), and
/// the <see cref="DbSet{TEntity}"/> query interface (what), so a
/// specialised adapter is the right grain.
/// </summary>
/// <typeparam name="T">Entity type — must be a class configured in the supplied <see cref="DbContext"/>.</typeparam>
/// <remarks>
/// <para>
/// <strong>DbContext lifecycle.</strong> Two construction modes:
/// </para>
/// <list type="bullet">
/// <item><strong>Injected</strong> — caller supplies the
/// <see cref="DbContext"/> and owns its lifetime. The adapter does not
/// dispose.</item>
/// <item><strong>Factory</strong> — caller supplies a factory; the
/// adapter creates a fresh context per Load/Save, then disposes it.
/// Use this for the concurrent-pipeline pattern with
/// <see cref="IDbContextFactory{TContext}"/>.</item>
/// </list>
/// <para>
/// <strong>Save semantics.</strong> Default: <em>replace</em> — the
/// adapter removes every existing row before inserting the supplied
/// data. Override via the <c>saveFunc</c> constructor parameter for
/// upsert / append / TRUNCATE-then-bulk-load strategies; the typed
/// context flows through to the delegate.
/// </para>
/// <para>
/// <strong>Inspection.</strong> <see cref="InspectShallow"/> /
/// <see cref="InspectDeep"/> / <see cref="InspectTarget"/> all run the
/// shape validator (<see cref="EFCoreShapeValidator"/>) before
/// touching row data — a missing column or nullability mismatch is
/// the actionable root cause and would otherwise surface as a
/// confusing materialisation error mid-flow.
/// </para>
/// </remarks>
public sealed class EFCoreStorageAdapter<T>
  : IStorageAdapter<IEnumerable<T>>, IHasEfficientCount
  where T : class
{
  private readonly DbContext? _injectedContext;
  private readonly Func<DbContext>? _contextFactory;
  private readonly bool _ownsContext;
  private readonly bool _allowEmptyData;
  private readonly Func<IQueryable<T>, IQueryable<T>>? _queryCustomizer;
  private readonly Func<DbContext, IEnumerable<T>, CancellationToken, Task>? _saveFunc;

  /// <summary>Adapter with an injected DbContext (caller owns lifetime).</summary>
  public EFCoreStorageAdapter(
    DbContext context,
    bool allowEmptyData = false,
    Func<IQueryable<T>, IQueryable<T>>? queryCustomizer = null,
    Func<DbContext, IEnumerable<T>, CancellationToken, Task>? saveFunc = null
  )
  {
    _injectedContext = context ?? throw new ArgumentNullException(nameof(context));
    _ownsContext = false;
    _allowEmptyData = allowEmptyData;
    _queryCustomizer = queryCustomizer;
    _saveFunc = saveFunc;

    // EF Core ports into the new traits surface as: read+write capable,
    // persistent (the DB outlives the run), transactional (EF Core
    // wraps SaveChanges in a transaction). Streaming reflects EF Core's
    // IAsyncEnumerable cursor support; we always materialise on Load
    // for now but the trait stays honest about the underlying capability.
    Traits = new StorageTraits
    {
      IsTransactional = true,
      CanStream = true,
    };

    ValidateEntityConfiguration(context);
  }

  /// <summary>Adapter with a DbContext factory (adapter creates + disposes per operation).</summary>
  public EFCoreStorageAdapter(
    Func<DbContext> contextFactory,
    bool allowEmptyData = false,
    Func<IQueryable<T>, IQueryable<T>>? queryCustomizer = null,
    Func<DbContext, IEnumerable<T>, CancellationToken, Task>? saveFunc = null
  )
  {
    _contextFactory = contextFactory ?? throw new ArgumentNullException(nameof(contextFactory));
    _ownsContext = true;
    _allowEmptyData = allowEmptyData;
    _queryCustomizer = queryCustomizer;
    _saveFunc = saveFunc;

    Traits = new StorageTraits
    {
      IsTransactional = true,
      CanStream = true,
    };

    using var context = contextFactory();
    ValidateEntityConfiguration(context);
  }

  /// <inheritdoc/>
  public StorageTraits Traits { get; }

  /// <inheritdoc/>
  public FlowIO<IEnumerable<T>> Load() =>
    FlowIO.LiftAsync<IEnumerable<T>>(async ct =>
    {
      var context = GetContext();
      try
      {
        var dbSet = context.Set<T>();
        var query = _queryCustomizer is not null ? _queryCustomizer(dbSet) : dbSet.AsQueryable();
        var data = await query.ToListAsync(ct).ConfigureAwait(false);
        return data;
      }
      finally
      {
        if (_ownsContext) await context.DisposeAsync().ConfigureAwait(false);
      }
    }, source: $"EFCoreStorageAdapter.Load[{typeof(T).Name}]");

  /// <inheritdoc/>
  public FlowIO<FlowUnit> Save(IEnumerable<T> data)
  {
    if (!Traits.CanWrite)
    {
      return FlowIO.Fail<FlowUnit>(new RuntimeError.External(
        $"EFCoreStorageAdapter.Save[{typeof(T).Name}]",
        new InvalidOperationException(
          "Cannot write to a read-only EFCore adapter. Verify StorageTraits.CanWrite "
          + "before calling Save() — typically the catalog item was Constrain()'d."
        )
      ));
    }

    return FlowIO.LiftAsync(async ct =>
    {
      var context = GetContext();
      try
      {
        await (_saveFunc ?? DefaultSave)(context, data, ct).ConfigureAwait(false);
        return FlowUnit.Default;
      }
      finally
      {
        if (_ownsContext) await context.DisposeAsync().ConfigureAwait(false);
      }
    }, source: $"EFCoreStorageAdapter.Save[{typeof(T).Name}]");
  }

  /// <inheritdoc/>
  public FlowIO<bool> Exists() =>
    FlowIO.LiftAsync(async ct =>
    {
      var context = GetContext();
      try
      {
        try
        {
          return await context.Set<T>().AnyAsync(ct).ConfigureAwait(false);
        }
        catch
        {
          // Table doesn't exist or query failed.
          return false;
        }
      }
      finally
      {
        if (_ownsContext) await context.DisposeAsync().ConfigureAwait(false);
      }
    }, source: $"EFCoreStorageAdapter.Exists[{typeof(T).Name}]");

  /// <inheritdoc/>
  public FlowIO<ValidationResult> InspectShallow(int sampleSize) =>
    FlowIO.LiftAsync(async ct => await InspectInternal(sampleSize, full: false, ct).ConfigureAwait(false),
      source: $"EFCoreStorageAdapter.InspectShallow[{typeof(T).Name}]");

  /// <inheritdoc/>
  public FlowIO<ValidationResult> InspectDeep() =>
    FlowIO.LiftAsync(async ct => await InspectInternal(sampleSize: 0, full: true, ct).ConfigureAwait(false),
      source: $"EFCoreStorageAdapter.InspectDeep[{typeof(T).Name}]");

  /// <inheritdoc/>
  public FlowIO<ValidationResult> InspectTarget() =>
    FlowIO.LiftAsync(async ct =>
    {
      var context = GetContext();
      try
      {
        var dbSet = context.Set<T>();
        try
        {
          await dbSet.AnyAsync(ct).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
          return ValidationResult.Failure(
            catalogKey: typeof(T).Name,
            errorType: ValidationErrorType.NotFound,
            message: $"Write target '{typeof(T).Name}' does not exist or is not accessible",
            details: $"Via {context.GetType().Name} on {GetConnectionDescription(context)}: {ex.Message}"
          );
        }

        return await EFCoreShapeValidator.ValidateAsync(
          context, typeof(T), typeof(T).Name, ct
        ).ConfigureAwait(false);
      }
      finally
      {
        if (_ownsContext) await context.DisposeAsync().ConfigureAwait(false);
      }
    }, source: $"EFCoreStorageAdapter.InspectTarget[{typeof(T).Name}]");

  /// <inheritdoc/>
  FlowIO<int> IHasEfficientCount.GetCountAsync() =>
    FlowIO.LiftAsync(async ct =>
    {
      var context = GetContext();
      try
      {
        return await context.Set<T>().CountAsync(ct).ConfigureAwait(false);
      }
      finally
      {
        if (_ownsContext) await context.DisposeAsync().ConfigureAwait(false);
      }
    }, source: $"EFCoreStorageAdapter.GetCountAsync[{typeof(T).Name}]");

  /// <summary>
  /// Default save: <c>RemoveRange(existing) + AddRange(new)</c>. Reference
  /// this explicitly when composing a custom save delegate that wraps the
  /// default ("custom pre-step + default save").
  /// </summary>
  public static async Task DefaultSave(DbContext context, IEnumerable<T> data, CancellationToken ct)
  {
    var dbSet = context.Set<T>();
    var existing = await dbSet.ToListAsync(ct).ConfigureAwait(false);
    dbSet.RemoveRange(existing);
    await dbSet.AddRangeAsync(data, ct).ConfigureAwait(false);
    await context.SaveChangesAsync(ct).ConfigureAwait(false);
  }

  // ── Internals ─────────────────────────────────────────────────────────

  private async Task<ValidationResult> InspectInternal(int sampleSize, bool full, CancellationToken ct)
  {
    var context = GetContext();
    try
    {
      var dbSet = context.Set<T>();

      bool hasData;
      try
      {
        hasData = await dbSet.AnyAsync(ct).ConfigureAwait(false);
      }
      catch (Exception ex)
      {
        return ValidationResult.Failure(
          catalogKey: typeof(T).Name,
          errorType: ValidationErrorType.NotFound,
          message: $"Table '{typeof(T).Name}' does not exist or is not accessible",
          details: $"Via {context.GetType().Name} on {GetConnectionDescription(context)}: {ex.Message}"
        );
      }

      // Shape validation runs ahead of emptiness/sampling — a column
      // mismatch is the actionable root cause; emptiness or sample
      // failures downstream of a bad shape are noise.
      var shapeResult = await EFCoreShapeValidator.ValidateAsync(
        context, typeof(T), typeof(T).Name, ct
      ).ConfigureAwait(false);
      if (shapeResult.HasErrors) return shapeResult;

      if (!hasData && !_allowEmptyData)
      {
        return ValidationResult.Failure(
          catalogKey: typeof(T).Name,
          errorType: ValidationErrorType.EmptyDataset,
          message: $"Table '{typeof(T).Name}' is empty and empty data is not allowed",
          details: "Set allowEmptyData: true on the catalog entry if empty tables are valid here."
        );
      }

      if (hasData && (full || sampleSize > 0))
      {
        try
        {
          IQueryable<T> probe = dbSet.AsNoTracking();
          if (!full) probe = probe.Take(sampleSize);
          await probe.ToListAsync(ct).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
          return ValidationResult.Failure(
            catalogKey: typeof(T).Name,
            errorType: ValidationErrorType.DeserializationError,
            message: $"Failed to read {(full ? "all" : "sample")} rows from table '{typeof(T).Name}'",
            details: $"Via {context.GetType().Name} on {GetConnectionDescription(context)}: {ex.Message}"
          );
        }
      }

      return ValidationResult.Success();
    }
    finally
    {
      if (_ownsContext) await context.DisposeAsync().ConfigureAwait(false);
    }
  }

  private DbContext GetContext()
  {
    if (_injectedContext is not null) return _injectedContext;
    if (_contextFactory is not null) return _contextFactory();
    throw new InvalidOperationException("EFCoreStorageAdapter has no DbContext source.");
  }

  /// <summary>
  /// Validate that <typeparamref name="T"/> is properly configured in
  /// the DbContext model. Forces EF Core's model build to run, so
  /// configuration errors (missing entity registration, missing key,
  /// array key) surface at adapter-construction time rather than at
  /// first Load/Save.
  /// </summary>
  private static void ValidateEntityConfiguration(DbContext context)
  {
    var entityType = context.Model.FindEntityType(typeof(T));
    if (entityType is null)
    {
      throw new InvalidOperationException(
        $"Entity type '{typeof(T).Name}' is not configured in DbContext '{context.GetType().Name}'. "
        + "Ensure the entity is added to the DbContext model."
      );
    }

    var primaryKey = entityType.FindPrimaryKey();
    if (primaryKey is null)
    {
      throw new InvalidOperationException(
        $"Entity type '{typeof(T).Name}' in DbContext '{context.GetType().Name}' has no primary key. "
        + "Configure one via HasKey() in OnModelCreating."
      );
    }

    var arrayProperty = primaryKey.Properties.FirstOrDefault(p => p.ClrType.IsArray);
    if (arrayProperty is not null)
    {
      throw new InvalidOperationException(
        $"Property '{arrayProperty.Name}' on entity '{typeof(T).Name}' uses array type '{arrayProperty.ClrType.Name}', "
        + "which cannot be used as an EF Core entity key (arrays use reference equality, "
        + "breaking change-tracking). Use a primitive key or a composite of primitives."
      );
    }
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
