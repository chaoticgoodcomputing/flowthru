using Flowthru.Data.Capabilities;
using Flowthru.Effects;
using Microsoft.EntityFrameworkCore;

namespace Flowthru.Data.Storage;

/// <summary>
/// Storage adapter for Entity Framework Core database access.
/// </summary>
/// <typeparam name="T">Entity type (must be a class configured in DbContext)</typeparam>
/// <remarks>
/// <para>
/// <strong>Design Rationale:</strong>
/// </para>
/// <para>
/// This is a <em>specialized adapter</em> that directly implements IStorageAdapter&lt;T&gt;
/// rather than using the Medium→Format→Container composition pattern. This design choice
/// reflects that EFCore inherently couples:
/// </para>
/// <list type="bullet">
/// <item><strong>WHERE:</strong> Connection string + database engine</item>
/// <item><strong>HOW:</strong> Entity mapping + LINQ-to-SQL translation</item>
/// <item><strong>WHAT:</strong> DbSet&lt;T&gt; query interface</item>
/// </list>
/// <para>
/// Attempting to decompose these concerns would fight EFCore's architecture.
/// </para>
/// <para>
/// <strong>DbContext Lifecycle:</strong>
/// </para>
/// <para>
/// Supports two modes:
/// </para>
/// <list type="bullet">
/// <item><strong>Injected:</strong> DbContext provided by caller (e.g., from DI container).
/// Caller owns lifecycle, adapter does NOT dispose.</item>
/// <item><strong>Factory:</strong> DbContext created via factory function on each operation.
/// Adapter owns lifecycle, disposes after operation.</item>
/// </list>
/// <para>
/// <strong>Capabilities:</strong>
/// </para>
/// <list type="bullet">
/// <item>ISeedable: true if table exists and contains data</item>
/// <item>IReadOnly: configurable (default: false)</item>
/// </list>
/// <para>
/// <strong>Pre-flight Validation:</strong>
/// </para>
/// <para>
/// The Exists() operation checks table existence. For auto-migration scenarios,
/// consider running migrations in a dedicated pipeline setup step before data processing.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Injected DbContext (from DI container)
/// var adapter = new EFCoreStorageAdapter&lt;Company&gt;(dbContext);
/// var entry = new CatalogEntry&lt;IEnumerable&lt;Company&gt;&gt;("companies", adapter);
///
/// // Factory-based DbContext (created per operation)
/// var adapter = new EFCoreStorageAdapter&lt;Company&gt;(() => new AppDbContext(options));
/// var entry = new CatalogEntry&lt;IEnumerable&lt;Company&gt;&gt;("companies", adapter);
///
/// // Read-only mode
/// var adapter = new EFCoreStorageAdapter&lt;Company&gt;(dbContext, readOnly: true);
/// </code>
/// </example>
public sealed class EFCoreStorageAdapter<T> : IStorageAdapter<IEnumerable<T>>, ISeedable, IReadOnly
  where T : class
{
  private readonly DbContext? _injectedContext;
  private readonly Func<DbContext>? _contextFactory;
  private readonly bool _ownsContext;
  private readonly bool _readOnly;

  /// <summary>
  /// Creates an adapter with an injected DbContext.
  /// </summary>
  /// <param name="context">DbContext instance (caller owns lifecycle)</param>
  /// <param name="readOnly">If true, Save operations will fail</param>
  public EFCoreStorageAdapter(DbContext context, bool readOnly = false)
  {
    _injectedContext = context ?? throw new ArgumentNullException(nameof(context));
    _contextFactory = null;
    _ownsContext = false;
    _readOnly = readOnly;

    // Validate entity configuration eagerly (pre-flight phase)
    ValidateEntityConfiguration(context);
  }

  /// <summary>
  /// Creates an adapter with a DbContext factory.
  /// </summary>
  /// <param name="contextFactory">Factory function to create DbContext instances</param>
  /// <param name="readOnly">If true, Save operations will fail</param>
  public EFCoreStorageAdapter(Func<DbContext> contextFactory, bool readOnly = false)
  {
    _contextFactory = contextFactory ?? throw new ArgumentNullException(nameof(contextFactory));
    _injectedContext = null;
    _ownsContext = true;
    _readOnly = readOnly;

    // Validate entity configuration eagerly using factory-created context
    using var context = contextFactory();
    ValidateEntityConfiguration(context);
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

  /// <inheritdoc/>
  public bool IsReadOnly => _readOnly;

  /// <inheritdoc/>
  public bool CanBeSeed
  {
    get
    {
      // A database can be a seed if the table exists
      // Synchronous property - we check this during pipeline construction
      try
      {
        var context = GetContext();
        try
        {
          var dbSet = context.Set<T>();
          // Check if table exists by attempting to query metadata
          // This is a heuristic - may need refinement based on provider
          var query = dbSet.AsQueryable();
          return true;
        }
        finally
        {
          if (_ownsContext && context != null)
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
  public FlowIO<IEnumerable<T>> Load()
  {
    return FlowIO.LiftAsync(async () =>
    {
      var context = GetContext();
      try
      {
        var dbSet = context.Set<T>();

        // Materialize the query into a list to detach from DbContext
        // This ensures the data survives DbContext disposal
        var data = await dbSet.ToListAsync();

        return (IEnumerable<T>)data;
      }
      finally
      {
        // Only dispose if we created the context
        if (_ownsContext && context != null)
        {
          await context.DisposeAsync();
        }
      }
    });
  }

  /// <inheritdoc/>
  public FlowIO<FlowUnit> Save(IEnumerable<T> data)
  {
    if (_readOnly)
    {
      return FlowIO.Fail<FlowUnit>(
        new InvalidOperationException(
          "Cannot write to read-only EFCore adapter. Check IReadOnly.IsReadOnly before attempting Save()."
        )
      );
    }

    return FlowIO.LiftAsync(async () =>
    {
      var context = GetContext();
      try
      {
        var dbSet = context.Set<T>();

        // Strategy: Replace all existing data with new data
        // This matches the semantics of file-based storage (overwrite)
        // For append/upsert semantics, use a specialized adapter or node logic

        // Clear existing data
        var existing = await dbSet.ToListAsync();
        dbSet.RemoveRange(existing);

        // Add new data
        await dbSet.AddRangeAsync(data);

        // Commit transaction
        await context.SaveChangesAsync();

        return FlowUnit.Default;
      }
      finally
      {
        if (_ownsContext && context != null)
        {
          await context.DisposeAsync();
        }
      }
    });
  }

  /// <inheritdoc/>
  public FlowIO<bool> Exists()
  {
    return FlowIO.LiftAsync(async () =>
    {
      var context = GetContext();
      try
      {
        var dbSet = context.Set<T>();

        // Check if table exists and has data
        // Database existence = table exists in schema
        // We use Any() to check both existence and non-empty
        // For empty-table-as-seed scenarios, this may need refinement
        return await dbSet.AnyAsync();
      }
      catch (Exception)
      {
        // Table doesn't exist or query failed
        return false;
      }
      finally
      {
        if (_ownsContext && context != null)
        {
          await context.DisposeAsync();
        }
      }
    });
  }

  private DbContext GetContext()
  {
    if (_injectedContext != null)
    {
      return _injectedContext;
    }

    if (_contextFactory != null)
    {
      return _contextFactory();
    }

    throw new InvalidOperationException(
      "EFCoreStorageAdapter has no DbContext. This should never happen."
    );
  }
}
