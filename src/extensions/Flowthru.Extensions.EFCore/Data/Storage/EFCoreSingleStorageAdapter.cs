using Flowthru.Core.Data.Capabilities;
using Flowthru.Core.Effects;
using Microsoft.EntityFrameworkCore;

namespace Flowthru.Core.Data.Storage;

/// <summary>
/// Storage adapter for single Entity Framework Core entities.
/// Stores exactly one row in a database table.
/// </summary>
/// <typeparam name="T">Entity type (must be a class type configured as EF entity)</typeparam>
/// <remarks>
/// <para>
/// <strong>Save Semantics:</strong> Replace - removes all existing rows and inserts the new entity.
/// Ensures table contains exactly one row after save.
/// </para>
/// <para>
/// <strong>Load Semantics:</strong> Returns the single row from the table.
/// Throws if table contains zero or more than one row.
/// </para>
/// <para>
/// <strong>Exists Semantics:</strong> Returns true if table has exactly one row.
/// </para>
/// </remarks>
public sealed class EFCoreSingleStorageAdapter<T> : IStorageAdapter<T>
  where T : class
{
  private readonly DbContext? _context;
  private readonly Func<DbContext>? _contextFactory;
  private readonly bool _ownsContext;
  private readonly bool _allowEmptyData;
  private readonly Func<IQueryable<T>, IQueryable<T>>? _queryCustomizer;
  private readonly Func<DbContext, T, CancellationToken, Task>? _saveFunc;

  /// <summary>
  /// Creates an adapter with an injected DbContext instance.
  /// </summary>
  /// <param name="context">DbContext to use for operations</param>
  /// <param name="ownsContext">If true, adapter disposes context after operations</param>
  /// <remarks>
  /// To create a read-only catalog entry, use <c>.Constrain(traits => traits with { CanWrite = false })</c>
  /// on the catalog entry after construction.
  /// </remarks>
  public EFCoreSingleStorageAdapter(
    DbContext context,
    bool ownsContext,
    bool allowEmptyData = false,
    Func<IQueryable<T>, IQueryable<T>>? queryCustomizer = null,
    Func<DbContext, T, CancellationToken, Task>? saveFunc = null
  )
  {
    _context = context ?? throw new ArgumentNullException(nameof(context));
    _ownsContext = ownsContext;
    _allowEmptyData = allowEmptyData;
    _queryCustomizer = queryCustomizer;
    _saveFunc = saveFunc;
    Traits = new StorageTraits
    {
      RequiresNetwork = true,
      IsTransactional = true,
      CanStream = true,
    };

    // Validate entity configuration eagerly (pre-flight phase)
    ValidateEntityConfiguration(context);
  }

  /// <summary>
  /// Creates an adapter with a DbContext factory.
  /// </summary>
  /// <param name="contextFactory">Factory to create DbContext instances</param>
  /// <remarks>
  /// To create a read-only catalog entry, use <c>.Constrain(traits => traits with { CanWrite = false })</c>
  /// on the catalog entry after construction.
  /// </remarks>
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
    Traits = new StorageTraits
    {
      RequiresNetwork = true,
      IsTransactional = true,
      CanStream = true,
    };

    // Validate entity configuration eagerly using factory-created context
    using var context = contextFactory();
    ValidateEntityConfiguration(context);
  }

  /// <inheritdoc/>
  public StorageTraits Traits { get; }

  /// <inheritdoc/>
  public FlowIO<T> Load() =>
    FlowIO.LiftAsync(
      async (ct) =>
      {
        var context = GetContext();
        try
        {
          var dbSet = context.Set<T>();
          var query = _queryCustomizer != null ? _queryCustomizer(dbSet) : dbSet.AsQueryable();
          var entity = await query.SingleAsync(ct);

          // Detach to prevent tracking issues if context is reused
          context.Entry(entity).State = EntityState.Detached;

          return entity;
        }
        finally
        {
          if (_ownsContext && _contextFactory != null)
          {
            await context.DisposeAsync();
          }
        }
      }
    );

  /// <inheritdoc/>
  public FlowIO<FlowUnit> Save(T data)
  {
    if (!Traits.CanWrite)
    {
      return FlowIO.Fail<FlowUnit>(
        new InvalidOperationException(
          "Cannot save to read-only EFCore adapter. Check StorageTraits.CanWrite before attempting Save()."
        )
      );
    }

    return FlowIO.LiftAsync(
      async (ct) =>
      {
        var context = GetContext();
        try
        {
          // Delegate to custom save func, or fall back to default replace-all semantics
          await (_saveFunc ?? DefaultSave)(context, data, ct);

          return FlowUnit.Default;
        }
        finally
        {
          if (_ownsContext && _contextFactory != null)
          {
            await context.DisposeAsync();
          }
        }
      }
    );
  }

  /// <inheritdoc/>
  public FlowIO<bool> Exists() =>
    FlowIO.LiftAsync(
      async (ct) =>
      {
        var context = GetContext();
        try
        {
          var dbSet = context.Set<T>();
          var count = await dbSet.CountAsync(ct);
          return count == 1;
        }
        finally
        {
          if (_ownsContext && _contextFactory != null)
          {
            await context.DisposeAsync();
          }
        }
      }
    );

  /// <inheritdoc/>
  public FlowIO<Data.Validation.ValidationResult> InspectShallow(int sampleSize)
  {
    return FlowIO.LiftAsync(
      async (ct) =>
      {
        var context = GetContext();
        try
        {
          var dbSet = context.Set<T>();

          // Check if table exists by attempting a query
          int count;
          try
          {
            count = await dbSet.CountAsync(ct);
          }
          catch (Exception ex)
          {
            return Data.Validation.ValidationResult.Failure(
              catalogKey: typeof(T).Name,
              errorType: Data.Validation.ValidationErrorType.NotFound,
              message: $"Table '{typeof(T).Name}' does not exist or is not accessible",
              details: ex.Message
            );
          }

          // Single entity storage requires exactly one row (unless allowEmptyData)
          if (count == 0 && !_allowEmptyData)
          {
            return Data.Validation.ValidationResult.Failure(
              catalogKey: typeof(T).Name,
              errorType: Data.Validation.ValidationErrorType.EmptyDataset,
              message: $"Table '{typeof(T).Name}' is empty",
              details: "Single entity storage requires exactly one row"
            );
          }

          if (count > 1)
          {
            return Data.Validation.ValidationResult.Failure(
              catalogKey: typeof(T).Name,
              errorType: Data.Validation.ValidationErrorType.DeserializationError,
              message: $"Table '{typeof(T).Name}' contains {count} rows",
              details: "Single entity storage requires exactly one row"
            );
          }

          // Empty table with allowEmptyData — nothing to load, inspection passes
          if (count == 0)
          {
            return Data.Validation.ValidationResult.Success();
          }

          // Attempt to load the entity to validate it's readable
          try
          {
            var entity = await dbSet.SingleAsync(ct);
            context.Entry(entity).State = EntityState.Detached;
          }
          catch (Exception ex)
          {
            return Data.Validation.ValidationResult.Failure(
              catalogKey: typeof(T).Name,
              errorType: Data.Validation.ValidationErrorType.DeserializationError,
              message: $"Failed to load entity from table '{typeof(T).Name}'",
              details: ex.Message
            );
          }

          return Data.Validation.ValidationResult.Success();
        }
        finally
        {
          if (_ownsContext && _contextFactory != null)
          {
            await context.DisposeAsync();
          }
        }
      }
    );
  }

  /// <inheritdoc/>
  public FlowIO<Data.Validation.ValidationResult> InspectDeep()
  {
    // For single entity storage, deep inspection is equivalent to shallow
    // since there's only one entity to validate
    return InspectShallow(sampleSize: 0);
  }

  /// <summary>
  /// Gets a DbContext from either the injected instance or factory.
  /// </summary>
  /// <summary>
  /// Default save strategy: replaces the single row with the new entity.
  /// Reference this explicitly when composing with a custom save delegate.
  /// </summary>
  public static async Task DefaultSave(DbContext context, T data, CancellationToken ct)
  {
    var dbSet = context.Set<T>();
    var existing = await dbSet.ToListAsync(ct);
    if (existing.Count > 0)
    {
      dbSet.RemoveRange(existing);
    }

    await dbSet.AddAsync(data, ct);
    await context.SaveChangesAsync(ct);
  }

  private DbContext GetContext()
  {
    if (_context != null)
    {
      return _context;
    }

    if (_contextFactory != null)
    {
      return _contextFactory();
    }

    throw new InvalidOperationException("No DbContext available");
  }

  /// <summary>
  /// Validates that the entity type T is properly configured in the DbContext.
  /// Forces EF Core model building to catch configuration errors during catalog initialization
  /// (pre-flight phase) rather than at runtime during step execution.
  /// </summary>
  /// <param name="context">DbContext to validate against</param>
  /// <exception cref="InvalidOperationException">
  /// Thrown when entity is not registered or has invalid configuration (e.g., array keys)
  /// </exception>
  private static void ValidateEntityConfiguration(DbContext context)
  {
    try
    {
      var entityType = context.Model.FindEntityType(typeof(T));
      if (entityType == null)
      {
        throw new InvalidOperationException(
          $"Entity type '{typeof(T).Name}' is not configured in DbContext '{context.GetType().Name}'. "
            + $"Ensure the entity is added to the DbContext model."
        );
      }

      var primaryKey = entityType.FindPrimaryKey();
      if (primaryKey == null)
      {
        throw new InvalidOperationException(
          $"Entity type '{typeof(T).Name}' in DbContext '{context.GetType().Name}' does not have a primary key configured. "
            + $"Configure a primary key using HasKey() in OnModelCreating."
        );
      }

      // Force identity map factory creation to validate key comparers
      if (primaryKey is Microsoft.EntityFrameworkCore.Metadata.Internal.IRuntimeKey runtimeKey)
      {
        _ = runtimeKey.GetIdentityMapFactory();
      }
    }
    catch (System.Reflection.TargetInvocationException ex)
    {
      // Unwrap nested TargetInvocationExceptions from reflection chain
      var innerMost = ex;
      while (innerMost.InnerException is System.Reflection.TargetInvocationException nested)
      {
        innerMost = nested;
      }

      if (innerMost.InnerException is InvalidCastException castEx)
      {
        throw new InvalidOperationException(
          $"Invalid key configuration for '{typeof(T).Name}' in DbContext '{context.GetType().Name}': "
            + $"{castEx.Message}. Arrays and certain collection types cannot be used as entity keys. "
            + $"Consider using a primitive type (int, Guid, string) or composite key of primitives.",
          castEx
        );
      }

      throw;
    }
    catch (InvalidCastException castEx)
    {
      throw new InvalidOperationException(
        $"Invalid key configuration for '{typeof(T).Name}' in DbContext '{context.GetType().Name}': "
          + $"{castEx.Message}. This typically indicates an unsupported key type configuration.",
        castEx
      );
    }
  }
}
