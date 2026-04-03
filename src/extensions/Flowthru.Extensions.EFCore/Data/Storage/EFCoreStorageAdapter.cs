using Flowthru.Data.Capabilities;
using Flowthru.Data.Validation;
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
/// <strong>Storage Traits:</strong>
/// </para>
/// <list type="bullet">
/// <item>RequiresNetwork: true (database access requires network/connection)</item>
/// <item>IsTransactional: true (supports rollback via EF Core transactions)</item>
/// <item>CanStream: true (supports streaming queries via IAsyncEnumerable)</item>
/// <item>CanWrite: true by default; constrain at catalog level for read-only entries</item>
/// </list>
/// <para>
/// <strong>Empty Data Validation:</strong>
/// </para>
/// <para>
/// By default, empty tables are considered invalid during pre-flight validation.
/// Set <c>allowEmptyData: true</c> when creating the catalog entry to allow empty tables.
/// This is useful for scenarios where a table may legitimately be empty (e.g., audit logs,
/// optional lookups, or incremental data pipelines).
/// </para>
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
/// // Read-only mode (apply constraint at catalog level)
/// var entry = new CatalogEntry&lt;IEnumerable&lt;Company&gt;&gt;("companies", adapter)
///   .Constrain(traits => traits with { CanWrite = false });
///
/// // Allow empty tables during validation
/// var adapter = new EFCoreStorageAdapter<Company>(dbContext, allowEmptyData: true);
/// </code>
/// </example>
public sealed class EFCoreStorageAdapter<T> : IStorageAdapter<IEnumerable<T>>
  where T : class
{
  private readonly DbContext? _injectedContext;
  private readonly Func<DbContext>? _contextFactory;
  private readonly bool _ownsContext;
  private readonly bool _allowEmptyData;
  private readonly Func<IQueryable<T>, IQueryable<T>>? _queryCustomizer;
  private readonly Func<DbContext, IEnumerable<T>, CancellationToken, Task>? _saveFunc;

  /// <summary>
  /// Creates an adapter with an injected DbContext.
  /// </summary>
  /// <param name="context">DbContext instance (caller owns lifecycle)</param>
  /// <param name="allowEmptyData">If true, empty tables are considered valid during validation</param>
  /// <remarks>
  /// To create a read-only catalog entry, use <c>.Constrain(traits => traits with { CanWrite = false })</c>
  /// on the catalog entry after construction.
  /// </remarks>
  public EFCoreStorageAdapter(
    DbContext context,
    bool allowEmptyData = false,
    Func<IQueryable<T>, IQueryable<T>>? queryCustomizer = null,
    Func<DbContext, IEnumerable<T>, CancellationToken, Task>? saveFunc = null
  )
  {
    _injectedContext = context ?? throw new ArgumentNullException(nameof(context));
    _contextFactory = null;
    _ownsContext = false;
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
  /// <param name="contextFactory">Factory function to create DbContext instances</param>
  /// <param name="allowEmptyData">If true, empty tables are considered valid during validation</param>
  /// <remarks>
  /// To create a read-only catalog entry, use <c>.Constrain(traits => traits with { CanWrite = false })</c>
  /// on the catalog entry after construction.
  /// </remarks>
  public EFCoreStorageAdapter(
    Func<DbContext> contextFactory,
    bool allowEmptyData = false,
    Func<IQueryable<T>, IQueryable<T>>? queryCustomizer = null,
    Func<DbContext, IEnumerable<T>, CancellationToken, Task>? saveFunc = null
  )
  {
    _contextFactory = contextFactory ?? throw new ArgumentNullException(nameof(contextFactory));
    _injectedContext = null;
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

  /// <inheritdoc/>
  public StorageTraits Traits { get; }

  /// <inheritdoc/>
  public FlowIO<IEnumerable<T>> Load()
  {
    return FlowIO.LiftAsync(
      async (ct) =>
      {
        var context = GetContext();
        try
        {
          var dbSet = context.Set<T>();

          // Apply query customizer (e.g. Include, Where, OrderBy) before materializing
          var query = _queryCustomizer != null ? _queryCustomizer(dbSet) : dbSet.AsQueryable();

          // Materialize the query into a list to detach from DbContext
          // This ensures the data survives DbContext disposal
          var data = await query.ToListAsync(ct);

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
      }
    );
  }

  /// <inheritdoc/>
  public FlowIO<FlowUnit> Save(IEnumerable<T> data)
  {
    if (!Traits.CanWrite)
    {
      return FlowIO.Fail<FlowUnit>(
        new InvalidOperationException(
          "Cannot write to read-only EFCore adapter. Check StorageTraits.CanWrite before attempting Save()."
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
          if (_ownsContext && context != null)
          {
            await context.DisposeAsync();
          }
        }
      }
    );
  }

  /// <inheritdoc/>
  public FlowIO<bool> Exists()
  {
    return FlowIO.LiftAsync(
      async (ct) =>
      {
        var context = GetContext();
        try
        {
          var dbSet = context.Set<T>();

          // Check if table exists and has data
          // Database existence = table exists in schema
          // We use Any() to check both existence and non-empty
          // For empty-table-as-seed scenarios, this may need refinement
          return await dbSet.AnyAsync(ct);
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
      }
    );
  }

  /// <inheritdoc/>
  public FlowIO<ValidationResult> InspectShallow(int sampleSize)
  {
    return FlowIO.LiftAsync(
      async (ct) =>
      {
        var context = GetContext();
        try
        {
          var dbSet = context.Set<T>();

          // Check if table exists by attempting a query
          bool hasData;
          try
          {
            hasData = await dbSet.AnyAsync(ct);
          }
          catch (Exception ex)
          {
            return ValidationResult.Failure(
              catalogKey: typeof(T).Name,
              errorType: ValidationErrorType.NotFound,
              message: $"Table '{typeof(T).Name}' does not exist or is not accessible",
              details: ex.Message
            );
          }

          // If table is empty and empty data is not allowed, fail validation
          if (!hasData && !_allowEmptyData)
          {
            return ValidationResult.Failure(
              catalogKey: typeof(T).Name,
              errorType: ValidationErrorType.EmptyDataset,
              message: $"Table '{typeof(T).Name}' is empty and empty data is not allowed",
              details: "Set allowEmptyData: true when creating the catalog entry if empty tables are valid for this use case."
            );
          }

          // Optionally sample first N rows to validate they're readable
          if (hasData && sampleSize > 0)
          {
            try
            {
              var sample = await dbSet.Take(sampleSize).ToListAsync(ct);
              // Successfully read sample - validation passed
            }
            catch (Exception ex)
            {
              return ValidationResult.Failure(
                catalogKey: typeof(T).Name,
                errorType: ValidationErrorType.DeserializationError,
                message: $"Failed to read sample rows from table '{typeof(T).Name}'",
                details: ex.Message
              );
            }
          }

          return ValidationResult.Success();
        }
        finally
        {
          if (_ownsContext && context != null)
          {
            await context.DisposeAsync();
          }
        }
      }
    );
  }

  /// <inheritdoc/>
  public FlowIO<ValidationResult> InspectDeep()
  {
    return FlowIO.LiftAsync(
      async (ct) =>
      {
        var context = GetContext();
        try
        {
          var dbSet = context.Set<T>();

          // Check if table exists by attempting a query
          bool hasData;
          try
          {
            hasData = await dbSet.AnyAsync(ct);
          }
          catch (Exception ex)
          {
            return ValidationResult.Failure(
              catalogKey: typeof(T).Name,
              errorType: ValidationErrorType.NotFound,
              message: $"Table '{typeof(T).Name}' does not exist or is not accessible",
              details: ex.Message
            );
          }

          // If table is empty and empty data is not allowed, fail validation
          if (!hasData && !_allowEmptyData)
          {
            return ValidationResult.Failure(
              catalogKey: typeof(T).Name,
              errorType: ValidationErrorType.EmptyDataset,
              message: $"Table '{typeof(T).Name}' is empty and empty data is not allowed",
              details: "Set allowEmptyData: true when creating the catalog entry if empty tables are valid for this use case."
            );
          }

          // Deep inspection: read and validate ALL rows
          if (hasData)
          {
            try
            {
              var all = await dbSet.ToListAsync(ct);
              // Successfully read all rows - validation passed
            }
            catch (Exception ex)
            {
              return ValidationResult.Failure(
                catalogKey: typeof(T).Name,
                errorType: ValidationErrorType.DeserializationError,
                message: $"Failed to read all rows from table '{typeof(T).Name}'",
                details: ex.Message
              );
            }
          }

          return ValidationResult.Success();
        }
        finally
        {
          if (_ownsContext && context != null)
          {
            await context.DisposeAsync();
          }
        }
      }
    );
  }

  /// <summary>
  /// Default save strategy: replaces all rows with the new data.
  /// Reference this explicitly when composing with a custom save delegate
  /// (e.g., "use default load but custom save").
  /// </summary>
  public static async Task DefaultSave(DbContext context, IEnumerable<T> data, CancellationToken ct)
  {
    var dbSet = context.Set<T>();
    var existing = await dbSet.ToListAsync(ct);
    dbSet.RemoveRange(existing);
    await dbSet.AddRangeAsync(data, ct);
    await context.SaveChangesAsync(ct);
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
