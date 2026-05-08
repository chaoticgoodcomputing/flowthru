using System.Data;
using System.Data.Common;
using Flowthru.Data.Storage.EFCore.Internal;
using Flowthru.Prelude;
using Flowthru.Validation.Runtime;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace Flowthru.Data.Storage.EFCore;

/// <summary>
/// Storage adapter that surfaces a deferred <see cref="DbQuery{T}"/>
/// handle as <see cref="IEnumerable{T}"/>. Eager reads happen on the
/// step's terms — not at <see cref="Load"/>. Save inspects the value:
/// when it is a <see cref="DbQuery{T}"/> with a matching scope, a
/// fused server-side <c>DELETE</c> + <c>INSERT-FROM-SELECT</c> avoids
/// pulling rows to the host; otherwise falls back to materialised
/// <c>RemoveRange + AddRange</c>.
/// </summary>
/// <typeparam name="T">Entity type — must be a class registered in the underlying DbContext.</typeparam>
/// <remarks>
/// <para>
/// <strong>Drop-in replacement.</strong> The outer item type is
/// <c>IItem&lt;IEnumerable&lt;T&gt;&gt;</c>, the same as
/// <see cref="EFCoreStorageAdapter{T}"/> — switching a catalog entry
/// from <c>EFCore</c> to <c>EFCoreQuery</c> defers reads without
/// requiring step-body changes.
/// </para>
/// <para>
/// <strong>Self-referential guard.</strong> If the SELECT plan
/// references the destination table, the fused path would receive 0
/// rows after the preceding DELETE. The adapter detects this and
/// silently falls back to the materialised path.
/// </para>
/// </remarks>
public sealed class EFCoreQueryStorageAdapter<T>
  : IStorageAdapter<IEnumerable<T>>, IHasEfficientCount
  where T : class
{
  private readonly DbQuery<T> _handle;
  private readonly bool _allowEmptyData;
  private readonly Func<DbContext, IEnumerable<T>, CancellationToken, Task>? _saveFunc;

  /// <summary>Adapter with an injected DbContext (caller owns lifetime).</summary>
  public EFCoreQueryStorageAdapter(
    DbContext context,
    bool allowEmptyData = false,
    Func<IQueryable<T>, IQueryable<T>>? queryCustomizer = null,
    Func<DbContext, IEnumerable<T>, CancellationToken, Task>? saveFunc = null,
    DbScope? scope = null
  )
  {
    if (context is null) throw new ArgumentNullException(nameof(context));
    var effectiveScope = scope ?? DbScope.Inferred(context);
    _handle = new DbQuery<T>(
      typeof(T).Name,
      effectiveScope,
      () => context,
      ownsContext: false,
      ctx => queryCustomizer is not null ? queryCustomizer(ctx.Set<T>()) : ctx.Set<T>()
    );
    _allowEmptyData = allowEmptyData;
    _saveFunc = saveFunc;
    Traits = new StorageTraits
    {
      IsTransactional = true,
      CanStream = true,
    };
    ValidateEntityConfiguration(context);
  }

  /// <summary>Adapter with a DbContext factory (adapter creates + disposes per operation).</summary>
  public EFCoreQueryStorageAdapter(
    Func<DbContext> contextFactory,
    bool allowEmptyData = false,
    Func<IQueryable<T>, IQueryable<T>>? queryCustomizer = null,
    Func<DbContext, IEnumerable<T>, CancellationToken, Task>? saveFunc = null,
    DbScope? scope = null
  )
  {
    if (contextFactory is null) throw new ArgumentNullException(nameof(contextFactory));
    var effectiveScope = scope ?? DbScope.Inferred(contextFactory);
    _handle = new DbQuery<T>(
      typeof(T).Name,
      effectiveScope,
      contextFactory,
      ownsContext: true,
      ctx => queryCustomizer is not null ? queryCustomizer(ctx.Set<T>()) : ctx.Set<T>()
    );
    _allowEmptyData = allowEmptyData;
    _saveFunc = saveFunc;
    Traits = new StorageTraits
    {
      IsTransactional = true,
      CanStream = true,
    };
    using var ctx = contextFactory();
    ValidateEntityConfiguration(ctx);
  }

  /// <inheritdoc/>
  public StorageTraits Traits { get; }

  /// <inheritdoc/>
  /// <remarks>Returns the deferred handle — no database I/O yet.</remarks>
  public FlowIO<IEnumerable<T>> Load() =>
    FlowIO.Lift<IEnumerable<T>>(() => _handle, source: $"EFCoreQueryStorageAdapter.Load[{typeof(T).Name}]");

  /// <inheritdoc/>
  public FlowIO<FlowUnit> Save(IEnumerable<T> data)
  {
    if (!Traits.CanWrite)
    {
      return FlowIO.Fail<FlowUnit>(new RuntimeError.External(
        $"EFCoreQueryStorageAdapter.Save[{typeof(T).Name}]",
        new InvalidOperationException(
          "Cannot write to a read-only EFCoreQuery adapter. The catalog "
          + "item was Constrain()'d — verify StorageTraits.CanWrite first."
        )
      ));
    }

    return FlowIO.LiftAsync(async ct =>
    {
      if (_saveFunc is not null)
      {
        var ctx = _handle.OpenContext();
        try
        {
          await _saveFunc(ctx, data, ct).ConfigureAwait(false);
        }
        finally
        {
          if (_handle.OwnsContext) await ctx.DisposeAsync().ConfigureAwait(false);
        }
        return FlowUnit.Default;
      }

      if (data is DbQuery<T> query && query.Scope.IsSameDatabase(_handle.Scope))
      {
        await FusedSaveAsync(query, ct).ConfigureAwait(false);
      }
      else
      {
        await MaterialisedSaveAsync(data, ct).ConfigureAwait(false);
      }

      return FlowUnit.Default;
    }, source: $"EFCoreQueryStorageAdapter.Save[{typeof(T).Name}]");
  }

  /// <inheritdoc/>
  public FlowIO<bool> Exists() =>
    FlowIO.LiftAsync(async ct =>
    {
      var ctx = _handle.OpenContext();
      try
      {
        try { return await ctx.Set<T>().AnyAsync(ct).ConfigureAwait(false); }
        catch { return false; }
      }
      finally
      {
        if (_handle.OwnsContext) await ctx.DisposeAsync().ConfigureAwait(false);
      }
    }, source: $"EFCoreQueryStorageAdapter.Exists[{typeof(T).Name}]");

  /// <inheritdoc/>
  public FlowIO<ValidationResult> InspectShallow(int sampleSize) =>
    FlowIO.LiftAsync(async ct => await InspectInternal(sampleSize, full: false, ct).ConfigureAwait(false),
      source: $"EFCoreQueryStorageAdapter.InspectShallow[{typeof(T).Name}]");

  /// <inheritdoc/>
  public FlowIO<ValidationResult> InspectDeep() =>
    FlowIO.LiftAsync(async ct => await InspectInternal(sampleSize: 0, full: true, ct).ConfigureAwait(false),
      source: $"EFCoreQueryStorageAdapter.InspectDeep[{typeof(T).Name}]");

  /// <inheritdoc/>
  public FlowIO<ValidationResult> InspectTarget() =>
    FlowIO.LiftAsync(async ct =>
    {
      var ctx = _handle.OpenContext();
      try
      {
        try
        {
          await ctx.Set<T>().AnyAsync(ct).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
          return ValidationResult.Failure(
            catalogKey: typeof(T).Name,
            errorType: ValidationErrorType.NotFound,
            message: $"Write target '{typeof(T).Name}' does not exist or is not accessible",
            details: $"Via {ctx.GetType().Name} on {GetConnectionDescription(ctx)}: {ex.Message}"
          );
        }

        return await EFCoreShapeValidator.ValidateAsync(
          ctx, typeof(T), typeof(T).Name, ct
        ).ConfigureAwait(false);
      }
      finally
      {
        if (_handle.OwnsContext) await ctx.DisposeAsync().ConfigureAwait(false);
      }
    }, source: $"EFCoreQueryStorageAdapter.InspectTarget[{typeof(T).Name}]");

  /// <inheritdoc/>
  /// <remarks>
  /// Pushes <c>COUNT(*)</c> through the configured query — no rows
  /// are returned to the host.
  /// </remarks>
  FlowIO<int> IHasEfficientCount.GetCountAsync() =>
    FlowIO.LiftAsync(async ct =>
    {
      var ctx = _handle.OpenContext();
      try
      {
        return await _handle.BuildQuery(ctx).CountAsync(ct).ConfigureAwait(false);
      }
      finally
      {
        if (_handle.OwnsContext) await ctx.DisposeAsync().ConfigureAwait(false);
      }
    }, source: $"EFCoreQueryStorageAdapter.GetCountAsync[{typeof(T).Name}]");

  // ── Internals ────────────────────────────────────────────────────

  private async Task<ValidationResult> InspectInternal(int sampleSize, bool full, CancellationToken ct)
  {
    var ctx = _handle.OpenContext();
    try
    {
      var dbSet = ctx.Set<T>();

      bool hasData;
      try { hasData = await dbSet.AnyAsync(ct).ConfigureAwait(false); }
      catch (Exception ex)
      {
        return ValidationResult.Failure(
          catalogKey: typeof(T).Name,
          errorType: ValidationErrorType.NotFound,
          message: $"Table '{typeof(T).Name}' does not exist or is not accessible",
          details: $"Via {ctx.GetType().Name} on {GetConnectionDescription(ctx)}: {ex.Message}"
        );
      }

      var shapeResult = await EFCoreShapeValidator.ValidateAsync(
        ctx, typeof(T), typeof(T).Name, ct
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
            details: $"Via {ctx.GetType().Name} on {GetConnectionDescription(ctx)}: {ex.Message}"
          );
        }
      }

      return ValidationResult.Success();
    }
    finally
    {
      if (_handle.OwnsContext) await ctx.DisposeAsync().ConfigureAwait(false);
    }
  }

  private async Task FusedSaveAsync(DbQuery<T> source, CancellationToken ct)
  {
    var context = _handle.OpenContext();
    try
    {
      var dbCommand = source.BuildQuery(context).CreateDbCommand();

      var entityType = context.Model.FindEntityType(typeof(T))!;
      var tableName = entityType.GetTableName()!;
      var schema = entityType.GetSchema();

      var sqlHelper = context.GetInfrastructure().GetRequiredService<ISqlGenerationHelper>();

      var quotedTable =
        schema is not null
          ? $"{sqlHelper.DelimitIdentifier(schema)}.{sqlHelper.DelimitIdentifier(tableName)}"
          : sqlHelper.DelimitIdentifier(tableName);

      var columnList = string.Join(
        ", ",
        entityType.GetProperties().Select(p => sqlHelper.DelimitIdentifier(p.GetColumnName()))
      );

      var insertSql = $"INSERT INTO {quotedTable} ({columnList}) {dbCommand.CommandText}";

      // If the SELECT references the destination table, the fused
      // path would observe 0 rows after the preceding DELETE.
      // Materialise + RemoveRange/AddRange instead.
      if (dbCommand.CommandText.Contains(quotedTable, StringComparison.Ordinal))
      {
        var rows = await source.ToListAsync(ct).ConfigureAwait(false);
        await MaterialisedSaveAsync(rows, ct).ConfigureAwait(false);
        return;
      }

      await context.Database.OpenConnectionAsync(ct).ConfigureAwait(false);
      await using var tx = await context.Database.BeginTransactionAsync(
        IsolationLevel.ReadCommitted, ct
      ).ConfigureAwait(false);
      try
      {
        await context.Set<T>().ExecuteDeleteAsync(ct).ConfigureAwait(false);

        var conn = context.Database.GetDbConnection();
        using var insertCmd = conn.CreateCommand();
        insertCmd.CommandText = insertSql;
        insertCmd.Transaction = tx.GetDbTransaction();

        foreach (DbParameter src in dbCommand.Parameters)
        {
          var p = insertCmd.CreateParameter();
          p.ParameterName = src.ParameterName;
          p.Value = src.Value;
          p.DbType = src.DbType;
          insertCmd.Parameters.Add(p);
        }

        await insertCmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
        await tx.CommitAsync(ct).ConfigureAwait(false);
      }
      catch
      {
        await tx.RollbackAsync(ct).ConfigureAwait(false);
        throw;
      }
      finally
      {
        await context.Database.CloseConnectionAsync().ConfigureAwait(false);
      }
    }
    finally
    {
      if (_handle.OwnsContext) await context.DisposeAsync().ConfigureAwait(false);
    }
  }

  private async Task MaterialisedSaveAsync(IEnumerable<T> data, CancellationToken ct)
  {
    var ctx = _handle.OpenContext();
    try
    {
      await DefaultSave(ctx, data, ct).ConfigureAwait(false);
    }
    finally
    {
      if (_handle.OwnsContext) await ctx.DisposeAsync().ConfigureAwait(false);
    }
  }

  /// <summary>
  /// Default save: <c>RemoveRange(existing) + AddRange(new) + SaveChanges</c>.
  /// Exposed so catalog authors can compose custom delegates that
  /// wrap the default ("custom pre-step + default save").
  /// </summary>
  public static async Task DefaultSave(DbContext context, IEnumerable<T> data, CancellationToken ct)
  {
    var dbSet = context.Set<T>();
    var existing = await dbSet.ToListAsync(ct).ConfigureAwait(false);
    dbSet.RemoveRange(existing);
    await dbSet.AddRangeAsync(data, ct).ConfigureAwait(false);
    await context.SaveChangesAsync(ct).ConfigureAwait(false);
  }

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
        + "which cannot be used as an EF Core entity key."
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
