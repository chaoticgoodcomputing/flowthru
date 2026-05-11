using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;

namespace Flowthru.Data.Storage.EFCore;

/// <summary>
/// A deferred EF Core query handle — captures all query configuration
/// at catalog construction time without executing any database call
/// until the value is iterated or
/// <see cref="ToListAsync(CancellationToken)"/> is invoked.
/// </summary>
/// <typeparam name="T">Entity type — must be a class registered in the underlying DbContext.</typeparam>
/// <remarks>
/// <para>
/// <strong>Why deferred.</strong> The eager
/// <see cref="EFCoreStorageAdapter{T}"/> materialises the entire table
/// in <see cref="EFCoreStorageAdapter{T}.Load"/>; that wastes I/O when
/// a step only needs a slice. <c>DbQuery&lt;T&gt;</c> ships the query
/// description down to the step body — the step composes filters / joins
/// / projections and the database evaluates the final shape on demand.
/// </para>
/// <para>
/// <strong>Materialisation boundary.</strong> Three paths trigger
/// execution:
/// </para>
/// <list type="bullet">
/// <item><see cref="ToListAsync(CancellationToken)"/> — explicit, async,
/// the recommended call from a step.</item>
/// <item>Iterating the value (<c>foreach</c> / LINQ) — implicit,
/// synchronous, supported via <see cref="IEnumerable{T}"/>.</item>
/// <item>Passing the handle to a save target with a matching
/// <see cref="DbScope"/> — fused INSERT-FROM-SELECT in
/// <see cref="EFCoreQueryStorageAdapter{T}"/>.</item>
/// </list>
/// <para>
/// <strong>Composition.</strong> <see cref="Where"/>,
/// <see cref="OrderBy{TKey}"/>, <see cref="Take"/>, <see cref="Skip"/>,
/// and <see cref="Project{TResult}"/> return new handles with the
/// composed expression tree — none execute the underlying query.
/// </para>
/// </remarks>
public sealed class DbQuery<T> : IEnumerable<T>
  where T : class
{
  internal readonly string Label;
  internal readonly DbScope Scope;
  internal readonly Func<DbContext, IQueryable<T>> BuildQuery;

  private readonly Func<DbContext> _contextFactory;
  private readonly bool _ownsContext;

  internal DbQuery(
    string label,
    DbScope scope,
    Func<DbContext> contextFactory,
    bool ownsContext,
    Func<DbContext, IQueryable<T>> buildQuery
  )
  {
    Label = label;
    Scope = scope;
    _contextFactory = contextFactory;
    _ownsContext = ownsContext;
    BuildQuery = buildQuery;
  }

  internal DbContext OpenContext() => _contextFactory();
  internal bool OwnsContext => _ownsContext;

  // ── Fluent LINQ composition ────────────────────────────────────────

  /// <summary>Filters the query. Returns a new handle; does not execute.</summary>
  public DbQuery<T> Where(Expression<Func<T, bool>> predicate) =>
    WithQuery(q => q.Where(predicate));

  /// <summary>Orders ascending. Returns a new handle.</summary>
  public DbQuery<T> OrderBy<TKey>(Expression<Func<T, TKey>> keySelector) =>
    WithQuery(q => q.OrderBy(keySelector));

  /// <summary>Orders descending. Returns a new handle.</summary>
  public DbQuery<T> OrderByDescending<TKey>(Expression<Func<T, TKey>> keySelector) =>
    WithQuery(q => q.OrderByDescending(keySelector));

  /// <summary>Limits the number of rows returned. Returns a new handle.</summary>
  public DbQuery<T> Take(int count) => WithQuery(q => q.Take(count));

  /// <summary>Skips the first <paramref name="count"/> rows. Returns a new handle.</summary>
  public DbQuery<T> Skip(int count) => WithQuery(q => q.Skip(count));

  // ── Type-changing projection ───────────────────────────────────────

  /// <summary>
  /// Builds a deferred query of a different entity type on the same
  /// database and scope — enables the fused INSERT-FROM-SELECT path
  /// when a step constructs a derived shape (e.g. JOIN of two
  /// upstream tables) that should be saved into a different DbSet.
  /// </summary>
  public DbQuery<TResult> Project<TResult>(Func<DbContext, IQueryable<TResult>> buildProjection)
    where TResult : class =>
    new(Label, Scope, _contextFactory, _ownsContext, buildProjection);

  // ── Materialisation ────────────────────────────────────────────────

  /// <summary>
  /// Executes the query and returns all matching rows. Applies
  /// <c>AsNoTracking()</c> automatically — query results are read-only
  /// snapshots, not change-tracked entities.
  /// </summary>
  public async Task<List<T>> ToListAsync(CancellationToken cancellationToken = default)
  {
    var context = _contextFactory();
    try
    {
      return await BuildQuery(context).AsNoTracking().ToListAsync(cancellationToken).ConfigureAwait(false);
    }
    finally
    {
      if (_ownsContext) await context.DisposeAsync().ConfigureAwait(false);
    }
  }

  /// <inheritdoc/>
  /// <remarks>
  /// Triggers synchronous materialisation. Prefer
  /// <see cref="ToListAsync(CancellationToken)"/> in async step bodies
  /// to avoid blocking a thread on database I/O.
  /// </remarks>
  public IEnumerator<T> GetEnumerator() =>
    ToListAsync().GetAwaiter().GetResult().GetEnumerator();

  [ExcludeFromCodeCoverage]
  IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

  private DbQuery<T> WithQuery(Func<IQueryable<T>, IQueryable<T>> compose)
  {
    var innerBuild = BuildQuery;
    return new DbQuery<T>(
      Label,
      Scope,
      _contextFactory,
      _ownsContext,
      ctx => compose(innerBuild(ctx))
    );
  }
}
