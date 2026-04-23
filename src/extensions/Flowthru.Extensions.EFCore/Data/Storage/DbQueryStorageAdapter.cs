using System.Data;
using System.Data.Common;
using Flowthru.Core.Data.Capabilities;
using Flowthru.Core.Data.Validation;
using Flowthru.Core.Effects;
using Flowthru.Extensions.EFCore.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace Flowthru.Core.Data.Storage;

/// <summary>
/// Storage adapter that surfaces a deferred <see cref="DbQuery{T}"/> handle for reading
/// and handles both fused server-side and materialised fallback saves.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Read path:</strong> <see cref="Load"/> returns a <see cref="DbQuery{T}"/> handle
/// typed as <see cref="IEnumerable{T}"/>. No database I/O occurs until a step iterates the
/// value or calls <see cref="DbQuery{T}.ToListAsync"/>.
/// </para>
/// <para>
/// <strong>Write path:</strong> <see cref="Save"/> inspects the incoming value:
/// </para>
/// <list type="bullet">
/// <item>
///   <strong>Fused (same DB):</strong> if the value is a <see cref="DbQuery{T}"/> whose
///   <see cref="DbQuery{T}.Scope"/> matches this adapter's scope, a single-round-trip
///   <c>DELETE</c> + <c>INSERT INTO … SELECT …</c> is executed entirely on the database server.
///   No rows are transferred to the application host.
/// </item>
/// <item>
///   <strong>Materialised fallback:</strong> if the value is a plain <see cref="IEnumerable{T}"/>
///   (e.g., from a preprocessing step) or the scopes differ, the data is materialised and
///   written with a <c>RemoveRange</c> + <c>AddRange</c> round-trip.
/// </item>
/// </list>
/// <para>
/// <strong>Drop-in replacement:</strong> This adapter produces <c>IItem&lt;IEnumerable&lt;T&gt;&gt;</c>
/// entries, the same outer type as <see cref="EFCoreStorageAdapter{T}"/>. Changing a catalog entry
/// from <c>EFCoreItemFactory.Enumerable.EFCore</c> to <c>EFCoreItemFactory.Query.EFCore</c>
/// defers all reads without requiring any step code changes.
/// </para>
/// </remarks>
/// <typeparam name="T">Entity type. Must be a class registered in the underlying DbContext.</typeparam>
public sealed class DbQueryStorageAdapter<T> : IStorageAdapter<IEnumerable<T>>, IHasEfficientCount
  where T : class
{
  private readonly DbQuery<T> _handle;
  private readonly bool _allowEmptyData;
  private readonly Func<DbContext, IEnumerable<T>, CancellationToken, Task>? _saveFunc;

  /// <summary>
  /// Creates an adapter with an injected <see cref="DbContext"/>.
  /// The caller owns the context lifecycle; the adapter does not dispose it.
  /// </summary>
  public DbQueryStorageAdapter(
    DbContext context,
    bool allowEmptyData = false,
    Func<IQueryable<T>, IQueryable<T>>? queryCustomizer = null,
    Func<DbContext, IEnumerable<T>, CancellationToken, Task>? saveFunc = null,
    DbScope? scope = null
  )
  {
    var effectiveScope = scope ?? DbScope.Inferred(context);
    _handle = new DbQuery<T>(
      typeof(T).Name,
      effectiveScope,
      () => context,
      ownsContext: false,
      ctx => queryCustomizer != null ? queryCustomizer(ctx.Set<T>()) : ctx.Set<T>()
    );
    _allowEmptyData = allowEmptyData;
    _saveFunc = saveFunc;
    Traits = new StorageTraits
    {
      RequiresNetwork = true,
      IsTransactional = true,
      CanStream = true,
    };
    ValidateEntityConfiguration(context);
  }

  /// <summary>
  /// Creates an adapter with a <see cref="DbContext"/> factory.
  /// A fresh context is created and disposed per Load/Save operation.
  /// </summary>
  public DbQueryStorageAdapter(
    Func<DbContext> contextFactory,
    bool allowEmptyData = false,
    Func<IQueryable<T>, IQueryable<T>>? queryCustomizer = null,
    Func<DbContext, IEnumerable<T>, CancellationToken, Task>? saveFunc = null,
    DbScope? scope = null
  )
  {
    var effectiveScope = scope ?? DbScope.Inferred(contextFactory);
    _handle = new DbQuery<T>(
      typeof(T).Name,
      effectiveScope,
      contextFactory,
      ownsContext: true,
      ctx => queryCustomizer != null ? queryCustomizer(ctx.Set<T>()) : ctx.Set<T>()
    );
    _allowEmptyData = allowEmptyData;
    _saveFunc = saveFunc;
    Traits = new StorageTraits
    {
      RequiresNetwork = true,
      IsTransactional = true,
      CanStream = true,
    };
    using var ctx = contextFactory();
    ValidateEntityConfiguration(ctx);
  }

  /// <inheritdoc/>
  public StorageTraits Traits { get; }

  /// <inheritdoc/>
  /// <remarks>Returns the deferred query handle — no database I/O.</remarks>
  public FlowIO<IEnumerable<T>> Load() => FlowIO.Lift<IEnumerable<T>>(() => _handle);

  /// <inheritdoc/>
  /// <remarks>
  /// Executes <c>SELECT COUNT(*)</c> against the query's predicate — no rows are transferred
  /// to the application host.
  /// </remarks>
  FlowIO<int> IHasEfficientCount.GetCountAsync()
  {
    return FlowIO.LiftAsync(async ct =>
    {
      var ctx = _handle.OpenContext();
      try
      {
        return await _handle.BuildQuery(ctx).CountAsync(ct);
      }
      finally
      {
        if (_handle.OwnsContext)
        {
          await ctx.DisposeAsync();
        }
      }
    });
  }

  /// <inheritdoc/>
  /// <remarks>
  /// Fused path when <paramref name="data"/> is a <see cref="DbQuery{T}"/> with a matching scope;
  /// materialised fallback otherwise.
  /// </remarks>
  public FlowIO<FlowUnit> Save(IEnumerable<T> data)
  {
    if (!Traits.CanWrite)
    {
      return FlowIO.Fail<FlowUnit>(
        new InvalidOperationException(
          "Cannot write to read-only DbQueryStorageAdapter. Check StorageTraits.CanWrite."
        )
      );
    }

    return FlowIO.LiftAsync(async ct =>
    {
      if (_saveFunc != null)
      {
        var ctx = _handle.OpenContext();
        try
        {
          await _saveFunc(ctx, data, ct);
        }
        finally
        {
          if (_handle.OwnsContext)
          {
            await ctx.DisposeAsync();
          }
        }
        return FlowUnit.Default;
      }

      if (data is DbQuery<T> query && query.Scope.IsSameDatabase(_handle.Scope))
      {
        await FusedSaveAsync(query, ct);
      }
      else
      {
        await MaterialisedSaveAsync(data, ct);
      }

      return FlowUnit.Default;
    });
  }

  /// <inheritdoc/>
  public FlowIO<bool> Exists()
  {
    return FlowIO.LiftAsync(async ct =>
    {
      var ctx = _handle.OpenContext();
      try
      {
        return await ctx.Set<T>().AnyAsync(ct);
      }
      catch
      {
        return false;
      }
      finally
      {
        if (_handle.OwnsContext)
        {
          await ctx.DisposeAsync();
        }
      }
    });
  }

  /// <inheritdoc/>
  public FlowIO<ValidationResult> InspectShallow(int sampleSize)
  {
    return FlowIO.LiftAsync(async ct =>
    {
      var ctx = _handle.OpenContext();
      try
      {
        bool hasData;
        try
        {
          hasData = await ctx.Set<T>().AnyAsync(ct);
        }
        catch (Exception ex)
        {
          return ValidationResult.Failure(
            catalogKey: typeof(T).Name,
            errorType: ValidationErrorType.NotFound,
            message: $"Table '{typeof(T).Name}' does not exist or is not accessible.",
            details: ex.Message
          );
        }

        if (!hasData && !_allowEmptyData)
        {
          return ValidationResult.Failure(
            catalogKey: typeof(T).Name,
            errorType: ValidationErrorType.EmptyDataset,
            message: $"Table '{typeof(T).Name}' is empty and empty data is not allowed.",
            details: "Set allowEmptyData: true when creating the catalog entry if empty tables are valid."
          );
        }

        if (hasData && sampleSize > 0)
        {
          try
          {
            _ = await ctx.Set<T>().AsNoTracking().Take(sampleSize).ToListAsync(ct);
          }
          catch (Exception ex)
          {
            return ValidationResult.Failure(
              catalogKey: typeof(T).Name,
              errorType: ValidationErrorType.DeserializationError,
              message: $"Failed to read sample rows from table '{typeof(T).Name}'.",
              details: ex.Message
            );
          }
        }

        return ValidationResult.Success();
      }
      finally
      {
        if (_handle.OwnsContext)
        {
          await ctx.DisposeAsync();
        }
      }
    });
  }

  /// <inheritdoc/>
  public FlowIO<ValidationResult> InspectDeep()
  {
    return FlowIO.LiftAsync(async ct =>
    {
      var ctx = _handle.OpenContext();
      try
      {
        bool hasData;
        try
        {
          hasData = await ctx.Set<T>().AnyAsync(ct);
        }
        catch (Exception ex)
        {
          return ValidationResult.Failure(
            catalogKey: typeof(T).Name,
            errorType: ValidationErrorType.NotFound,
            message: $"Table '{typeof(T).Name}' does not exist or is not accessible.",
            details: ex.Message
          );
        }

        if (!hasData && !_allowEmptyData)
        {
          return ValidationResult.Failure(
            catalogKey: typeof(T).Name,
            errorType: ValidationErrorType.EmptyDataset,
            message: $"Table '{typeof(T).Name}' is empty and empty data is not allowed.",
            details: "Set allowEmptyData: true when creating the catalog entry if empty tables are valid."
          );
        }

        if (hasData)
        {
          try
          {
            _ = await ctx.Set<T>().AsNoTracking().ToListAsync(ct);
          }
          catch (Exception ex)
          {
            return ValidationResult.Failure(
              catalogKey: typeof(T).Name,
              errorType: ValidationErrorType.DeserializationError,
              message: $"Failed to read all rows from table '{typeof(T).Name}'.",
              details: ex.Message
            );
          }
        }

        return ValidationResult.Success();
      }
      finally
      {
        if (_handle.OwnsContext)
        {
          await ctx.DisposeAsync();
        }
      }
    });
  }

  // ── Fused INSERT-FROM-SELECT ───────────────────────────────────────────────

  private async Task FusedSaveAsync(DbQuery<T> source, CancellationToken ct)
  {
    var context = _handle.OpenContext();
    try
    {
      // Build the SELECT command before opening the connection.
      // CreateDbCommand() interrogates the EF query pipeline to produce a DbCommand
      // with CommandText = parametrised SELECT SQL and Parameters populated.
      var dbCommand = source.BuildQuery(context).CreateDbCommand();

      // Resolve target table metadata and quote identifiers using the provider's helper.
      var entityType = context.Model.FindEntityType(typeof(T))!;
      var tableName = entityType.GetTableName()!;
      var schema = entityType.GetSchema();

      var sqlHelper = context.GetInfrastructure().GetRequiredService<ISqlGenerationHelper>();

      var quotedTable =
        schema != null
          ? $"{sqlHelper.DelimitIdentifier(schema)}.{sqlHelper.DelimitIdentifier(tableName)}"
          : sqlHelper.DelimitIdentifier(tableName);

      var columnList = string.Join(
        ", ",
        entityType.GetProperties().Select(p => sqlHelper.DelimitIdentifier(p.GetColumnName()))
      );

      // Final SQL: INSERT INTO "target" (col1, col2, ...) <SELECT SQL>
      var insertSql = $"INSERT INTO {quotedTable} ({columnList}) {dbCommand.CommandText}";

      // Self-referential guard: if the SELECT reads from the same table we are about to
      // delete from, the INSERT would receive 0 rows. Fall back to the materialized path.
      if (dbCommand.CommandText.Contains(quotedTable, StringComparison.Ordinal))
      {
        var rows = await source.ToListAsync(ct);
        await MaterialisedSaveAsync(rows, ct);
        return;
      }

      await context.Database.OpenConnectionAsync(ct);
      await using var tx = await context.Database.BeginTransactionAsync(
        IsolationLevel.ReadCommitted,
        ct
      );
      try
      {
        // Server-side DELETE — honours the ambient transaction opened above.
        await context.Set<T>().ExecuteDeleteAsync(ct);

        // Execute INSERT-FROM-SELECT on the same connection and transaction.
        var conn = context.Database.GetDbConnection();
        using var insertCmd = conn.CreateCommand();
        insertCmd.CommandText = insertSql;
        insertCmd.Transaction = tx.GetDbTransaction();

        // Copy parameters from the source SELECT command.
        foreach (DbParameter src in dbCommand.Parameters)
        {
          var p = insertCmd.CreateParameter();
          p.ParameterName = src.ParameterName;
          p.Value = src.Value;
          p.DbType = src.DbType;
          insertCmd.Parameters.Add(p);
        }

        await insertCmd.ExecuteNonQueryAsync(ct);
        await tx.CommitAsync(ct);
      }
      catch
      {
        await tx.RollbackAsync(ct);
        throw;
      }
      finally
      {
        await context.Database.CloseConnectionAsync();
      }
    }
    finally
    {
      if (_handle.OwnsContext)
      {
        await context.DisposeAsync();
      }
    }
  }

  private async Task MaterialisedSaveAsync(IEnumerable<T> data, CancellationToken ct)
  {
    var ctx = _handle.OpenContext();
    try
    {
      await DefaultSave(ctx, data, ct);
    }
    finally
    {
      if (_handle.OwnsContext)
      {
        await ctx.DisposeAsync();
      }
    }
  }

  /// <summary>
  /// Default save strategy: removes all existing rows then inserts the new data.
  /// Exposed so catalog engineers can reference it when composing custom save delegates.
  /// </summary>
  public static async Task DefaultSave(DbContext context, IEnumerable<T> data, CancellationToken ct)
  {
    var dbSet = context.Set<T>();
    var existing = await dbSet.ToListAsync(ct);
    dbSet.RemoveRange(existing);
    await dbSet.AddRangeAsync(data, ct);
    await context.SaveChangesAsync(ct);
  }

  // ── Pre-flight validation ──────────────────────────────────────────────────

  private static void ValidateEntityConfiguration(DbContext context)
  {
    var entityType = context.Model.FindEntityType(typeof(T));
    if (entityType == null)
    {
      throw new InvalidOperationException(
        $"Entity type '{typeof(T).Name}' is not configured in DbContext '{context.GetType().Name}'. "
          + "Ensure the entity is added to the DbContext model."
      );
    }

    var primaryKey = entityType.FindPrimaryKey();
    if (primaryKey == null)
    {
      throw new InvalidOperationException(
        $"Entity type '{typeof(T).Name}' in DbContext '{context.GetType().Name}' does not have "
          + "a primary key configured. Configure a primary key using HasKey() in OnModelCreating."
      );
    }

    var arrayProperty = primaryKey.Properties.FirstOrDefault(p => p.ClrType.IsArray);
    if (arrayProperty != null)
    {
      throw new InvalidOperationException(
        $"Property '{arrayProperty.Name}' on entity '{typeof(T).Name}' uses array type "
          + $"'{arrayProperty.ClrType.Name}', which cannot be used as an entity key."
      );
    }
  }
}
