using Flowthru.Data.Storage.EFCore.Internal;
using Flowthru.Prelude;
using Flowthru.Validation.Runtime;
using Microsoft.EntityFrameworkCore;

namespace Flowthru.Data.Storage.EFCore;

/// <summary>
/// Storage adapter for an Entity Framework Core entity that lives in a
/// table holding exactly one row — trained models, configuration
/// snapshots, aggregated metrics, etc. Implements
/// <see cref="IStorageAdapter{T}"/> directly, mirroring the shape of
/// <see cref="EFCoreStorageAdapter{T}"/> but on a single
/// <typeparamref name="T"/> rather than a collection.
/// </summary>
/// <typeparam name="T">Entity type — must be a class configured in the supplied <see cref="DbContext"/>.</typeparam>
/// <remarks>
/// <para>
/// <strong>Save semantics.</strong> Default: <em>replace</em> — the
/// adapter removes every existing row and inserts the supplied entity,
/// leaving exactly one row in the table.
/// </para>
/// <para>
/// <strong>Inspection.</strong> Pre-flight asserts the table contains
/// exactly one row (or zero when <c>allowEmptyData</c> is set).
/// Multiple rows surface as a <see cref="ValidationErrorType.DeserializationError"/>
/// — the table's invariant has been violated by some other writer.
/// </para>
/// </remarks>
public sealed class EFCoreSingleStorageAdapter<T> : IStorageAdapter<T>, IHasServiceDependencies
  where T : class
{
  private readonly DbContext? _injectedContext;
  private readonly Func<DbContext>? _contextFactory;
  private readonly bool _ownsContext;
  private readonly bool _allowEmptyData;
  private readonly Func<IQueryable<T>, IQueryable<T>>? _queryCustomizer;
  private readonly Func<DbContext, T, CancellationToken, Task>? _saveFunc;
  private readonly IReadOnlyList<ServiceDependency> _serviceDependencies;

  /// <summary>Adapter with an injected DbContext (caller owns lifetime).</summary>
  public EFCoreSingleStorageAdapter(
    DbContext context,
    bool ownsContext = false,
    bool allowEmptyData = false,
    Func<IQueryable<T>, IQueryable<T>>? queryCustomizer = null,
    Func<DbContext, T, CancellationToken, Task>? saveFunc = null
  )
  {
    _injectedContext = context ?? throw new ArgumentNullException(nameof(context));
    _ownsContext = ownsContext;
    _allowEmptyData = allowEmptyData;
    _queryCustomizer = queryCustomizer;
    _saveFunc = saveFunc;
    var conflict = EFCoreConflictProfile.Probe(context);
    Traits = new StorageTraits
    {
      IsTransactional = true,
      CanStream = true,
      WriteCapacity = conflict.WriteCapacity,
      ReadCapacity = conflict.ReadCapacity,
    };
    _serviceDependencies = new[] { conflict.Dependency };
    EFCoreEntityValidation.Validate<T>(context);
  }

  /// <summary>Adapter with a DbContext factory (adapter creates + disposes per operation).</summary>
  public EFCoreSingleStorageAdapter(
    Func<DbContext> contextFactory,
    bool allowEmptyData = false,
    Func<IQueryable<T>, IQueryable<T>>? queryCustomizer = null,
    Func<DbContext, T, CancellationToken, Task>? saveFunc = null
  )
  {
    _contextFactory = contextFactory ?? throw new ArgumentNullException(nameof(contextFactory));
    _ownsContext = true;
    _allowEmptyData = allowEmptyData;
    _queryCustomizer = queryCustomizer;
    _saveFunc = saveFunc;

    using var context = contextFactory();
    var conflict = EFCoreConflictProfile.Probe(context);
    Traits = new StorageTraits
    {
      IsTransactional = true,
      CanStream = true,
      WriteCapacity = conflict.WriteCapacity,
      ReadCapacity = conflict.ReadCapacity,
    };
    _serviceDependencies = new[] { conflict.Dependency };
    EFCoreEntityValidation.Validate<T>(context);
  }

  /// <inheritdoc/>
  public StorageTraits Traits { get; }

  /// <inheritdoc/>
  public IReadOnlyList<ServiceDependency> ServiceDependencies => _serviceDependencies;

  /// <inheritdoc/>
  public FlowIO<T> Load() =>
    FlowIO.LiftAsync(async ct =>
    {
      var context = GetContext();
      try
      {
        var dbSet = context.Set<T>();
        var query = _queryCustomizer is not null ? _queryCustomizer(dbSet) : dbSet.AsQueryable();
        var entity = await query.SingleAsync(ct).ConfigureAwait(false);
        // Detach so the caller can mutate without disturbing the context's
        // change tracker (and so the entity survives DbContext disposal).
        context.Entry(entity).State = EntityState.Detached;
        return entity;
      }
      finally
      {
        if (_ownsContext && _contextFactory is not null) await context.DisposeAsync().ConfigureAwait(false);
      }
    }, source: $"EFCoreSingleStorageAdapter.Load[{typeof(T).Name}]");

  /// <inheritdoc/>
  public FlowIO<FlowUnit> Save(T data)
  {
    if (!Traits.CanWrite)
    {
      return FlowIO.Fail<FlowUnit>(new RuntimeError.External(
        $"EFCoreSingleStorageAdapter.Save[{typeof(T).Name}]",
        new InvalidOperationException(
          "Cannot write to a read-only EFCore single adapter. Verify "
          + "StorageTraits.CanWrite before calling Save() — typically the catalog "
          + "item was Constrain()'d."
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
        if (_ownsContext && _contextFactory is not null) await context.DisposeAsync().ConfigureAwait(false);
      }
    }, source: $"EFCoreSingleStorageAdapter.Save[{typeof(T).Name}]");
  }

  /// <inheritdoc/>
  public FlowIO<bool> Exists() =>
    FlowIO.LiftAsync(async ct =>
    {
      var context = GetContext();
      try
      {
        return await context.Set<T>().CountAsync(ct).ConfigureAwait(false) == 1;
      }
      finally
      {
        if (_ownsContext && _contextFactory is not null) await context.DisposeAsync().ConfigureAwait(false);
      }
    }, source: $"EFCoreSingleStorageAdapter.Exists[{typeof(T).Name}]");

  /// <inheritdoc/>
  public FlowIO<ValidationResult> InspectShallow(int sampleSize) =>
    FlowIO.LiftAsync(async ct => await InspectInternal(ct).ConfigureAwait(false),
      source: $"EFCoreSingleStorageAdapter.InspectShallow[{typeof(T).Name}]");

  /// <inheritdoc/>
  public FlowIO<ValidationResult> InspectDeep() =>
    // Single-entity storage holds at most one row — deep == shallow.
    InspectShallow(sampleSize: 0);

  /// <inheritdoc/>
  public FlowIO<ValidationResult> InspectTarget() =>
    FlowIO.LiftAsync(async ct =>
    {
      var context = GetContext();
      try
      {
        try
        {
          await context.Set<T>().AnyAsync(ct).ConfigureAwait(false);
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
        if (_ownsContext && _contextFactory is not null) await context.DisposeAsync().ConfigureAwait(false);
      }
    }, source: $"EFCoreSingleStorageAdapter.InspectTarget[{typeof(T).Name}]");

  /// <summary>
  /// Default save: <c>RemoveRange(existing) + Add(new) + SaveChanges</c>.
  /// Reference this explicitly when wrapping it in a custom save delegate.
  /// </summary>
  public static async Task DefaultSave(DbContext context, T data, CancellationToken ct)
  {
    var dbSet = context.Set<T>();
    var existing = await dbSet.ToListAsync(ct).ConfigureAwait(false);
    if (existing.Count > 0) dbSet.RemoveRange(existing);
    await dbSet.AddAsync(data, ct).ConfigureAwait(false);
    await context.SaveChangesAsync(ct).ConfigureAwait(false);
  }

  // ── Internals ─────────────────────────────────────────────────────────

  private async Task<ValidationResult> InspectInternal(CancellationToken ct)
  {
    var context = GetContext();
    try
    {
      var dbSet = context.Set<T>();

      int count;
      try
      {
        count = await dbSet.CountAsync(ct).ConfigureAwait(false);
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

      var shapeResult = await EFCoreShapeValidator.ValidateAsync(
        context, typeof(T), typeof(T).Name, ct
      ).ConfigureAwait(false);
      if (shapeResult.HasErrors) return shapeResult;

      if (count == 0 && !_allowEmptyData)
      {
        return ValidationResult.Failure(
          catalogKey: typeof(T).Name,
          errorType: ValidationErrorType.EmptyDataset,
          message: $"Table '{typeof(T).Name}' is empty",
          details: "Single-entity storage requires exactly one row."
        );
      }

      if (count > 1)
      {
        return ValidationResult.Failure(
          catalogKey: typeof(T).Name,
          errorType: ValidationErrorType.DeserializationError,
          message: $"Table '{typeof(T).Name}' contains {count} rows",
          details: "Single-entity storage requires exactly one row."
        );
      }

      if (count == 0) return ValidationResult.Success();

      try
      {
        var entity = await dbSet.SingleAsync(ct).ConfigureAwait(false);
        context.Entry(entity).State = EntityState.Detached;
      }
      catch (Exception ex)
      {
        return ValidationResult.Failure(
          catalogKey: typeof(T).Name,
          errorType: ValidationErrorType.DeserializationError,
          message: $"Failed to load entity from table '{typeof(T).Name}'",
          details: $"Via {context.GetType().Name} on {GetConnectionDescription(context)}: {ex.Message}"
        );
      }

      return ValidationResult.Success();
    }
    finally
    {
      if (_ownsContext && _contextFactory is not null) await context.DisposeAsync().ConfigureAwait(false);
    }
  }

  private DbContext GetContext()
  {
    if (_injectedContext is not null) return _injectedContext;
    if (_contextFactory is not null) return _contextFactory();
    throw new InvalidOperationException("EFCoreSingleStorageAdapter has no DbContext source.");
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

/// <summary>
/// Shared entity-configuration validation used by both EFCore
/// adapters. Lifted out of the per-adapter implementations so the
/// "ensure the entity is registered, has a key, and the key isn't an
/// array" check is one source of truth.
/// </summary>
internal static class EFCoreEntityValidation
{
  public static void Validate<T>(DbContext context) where T : class
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
}
