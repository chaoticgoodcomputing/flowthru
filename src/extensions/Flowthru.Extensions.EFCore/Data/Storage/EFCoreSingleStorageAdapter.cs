using Flowthru.Data.Capabilities;
using Flowthru.Effects;
using Microsoft.EntityFrameworkCore;

namespace Flowthru.Data.Storage;

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
/// <para>
/// <strong>CanBeSeed:</strong> Returns true if table exists and contains exactly one row (synchronous check).
/// May return false negative if table check requires async database query.
/// </para>
/// </remarks>
public sealed class EFCoreSingleStorageAdapter<T> : IStorageAdapter<T>, ISeedable, IReadOnly
  where T : class
{
  private readonly DbContext? _context;
  private readonly Func<DbContext>? _contextFactory;
  private readonly bool _ownsContext;
  private readonly bool _readOnly;

  /// <summary>
  /// Creates an adapter with an injected DbContext instance.
  /// </summary>
  /// <param name="context">DbContext to use for operations</param>
  /// <param name="ownsContext">If true, adapter disposes context after operations</param>
  /// <param name="readOnly">If true, Save operations throw</param>
  public EFCoreSingleStorageAdapter(DbContext context, bool ownsContext, bool readOnly)
  {
    _context = context ?? throw new ArgumentNullException(nameof(context));
    _ownsContext = ownsContext;
    _readOnly = readOnly;

    // Validate entity configuration eagerly (pre-flight phase)
    ValidateEntityConfiguration(context);
  }

  /// <summary>
  /// Creates an adapter with a DbContext factory.
  /// </summary>
  /// <param name="contextFactory">Factory to create DbContext instances</param>
  /// <param name="readOnly">If true, Save operations throw</param>
  public EFCoreSingleStorageAdapter(Func<DbContext> contextFactory, bool readOnly)
  {
    _contextFactory = contextFactory ?? throw new ArgumentNullException(nameof(contextFactory));
    _ownsContext = true;
    _readOnly = readOnly;

    // Validate entity configuration eagerly using factory-created context
    using var context = contextFactory();
    ValidateEntityConfiguration(context);
  }

  /// <inheritdoc/>
  public bool CanBeSeed
  {
    get
    {
      try
      {
        var context = GetContext();
        try
        {
          var dbSet = context.Set<T>();
          // Synchronous check - may not work for all providers
          return dbSet.Local.Count == 1 || dbSet.Any();
        }
        finally
        {
          if (_ownsContext && _contextFactory != null)
          {
            context.Dispose();
          }
        }
      }
      catch
      {
        return false;
      }
    }
  }

  /// <inheritdoc/>
  public bool IsReadOnly => _readOnly;

  /// <inheritdoc/>
  public FlowIO<T> Load() =>
    FlowIO.LiftAsync(
      async (ct) =>
      {
        var context = GetContext();
        try
        {
          var dbSet = context.Set<T>();
          var entity = await dbSet.SingleAsync(ct);

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
    if (_readOnly)
    {
      return FlowIO.Fail<FlowUnit>(
        new InvalidOperationException("Cannot save to read-only EFCore catalog entry")
      );
    }

    return FlowIO.LiftAsync(
      async (ct) =>
      {
        var context = GetContext();
        try
        {
          var dbSet = context.Set<T>();

          // Replace semantics: remove all existing rows
          var existing = await dbSet.ToListAsync(ct);
          if (existing.Count > 0)
          {
            dbSet.RemoveRange(existing);
          }

          // Add new entity
          await dbSet.AddAsync(data, ct);
          await context.SaveChangesAsync(ct);

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

  /// <summary>
  /// Gets a DbContext from either the injected instance or factory.
  /// </summary>
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
  /// (pre-flight phase) rather than at runtime during node execution.
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
