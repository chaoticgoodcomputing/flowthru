using System.Collections;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;

namespace Flowthru.Extensions.EFCore.Data;

/// <summary>
/// A deferred EF Core query handle — analogous to <c>TypedFrame&lt;T&gt;</c> in the Spark extension.
/// </summary>
/// <remarks>
/// <para>
/// <c>DbQuery&lt;T&gt;</c> captures all query configuration at catalog construction time but does
/// <em>not</em> execute any database calls until explicitly materialized. The catalog declares
/// <em>what</em> to query; steps decide <em>when</em> to materialize via
/// <see cref="ToListAsync"/> or by iterating the <see cref="IEnumerable{T}"/> interface.
/// </para>
/// <para>
/// <strong>Materialization boundaries:</strong>
/// </para>
/// <list type="bullet">
/// <item>
///   Explicit: call <see cref="ToListAsync"/> in your step transform.
/// </item>
/// <item>
///   Implicit: <c>DbQuery&lt;T&gt;</c> implements <see cref="IEnumerable{T}"/>, so
///   LINQ operators and <c>foreach</c> trigger synchronous materialization automatically.
///   Explicit calls are preferred for readability — they make the database boundary visible.
/// </item>
/// </list>
/// <para>
/// <strong>Fluent composition:</strong> Use <see cref="Where"/>, <see cref="OrderBy{TKey}"/>,
/// <see cref="Take"/>, <see cref="Skip"/> to refine the query without triggering execution.
/// Each method returns a new <c>DbQuery&lt;T&gt;</c> with the composed expression tree.
/// </para>
/// <para>
/// <strong>Type-changing projection:</strong> Use <see cref="Project{TResult}"/> to build a
/// deferred query of a different entity type on the same database and scope. This enables
/// steps to construct a derived <c>DbQuery&lt;TResult&gt;</c> that can be saved by a
/// <see cref="Flowthru.Core.Data.Storage.DbQueryStorageAdapter{T}"/> using the fused
/// INSERT-FROM-SELECT path.
/// </para>
/// <para>
/// <strong>Save semantics:</strong> <c>DbQuery&lt;T&gt;</c> values passed to
/// <see cref="Flowthru.Core.Data.Storage.DbQueryStorageAdapter{T}.Save"/> trigger a
/// server-side fused DELETE + INSERT-FROM-SELECT when source and destination share the
/// same <see cref="Scope"/>. All other cases fall back to full materialization.
/// </para>
/// </remarks>
/// <typeparam name="T">The entity type. Must be a class registered in the underlying DbContext.</typeparam>
public sealed class DbQuery<T> : IEnumerable<T>
  where T : class
{
  internal readonly string Label;
  internal readonly DbScope Scope;
  internal readonly Func<DbContext, IQueryable<T>> BuildQuery;

  private readonly Func<DbContext> _contextFactory;
  private readonly bool _ownsContext;

  // Internal — use EFCoreItemFactory.Query or DbQuery<TOther>.Project<T>() to construct.
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

  // ── Internal context access for the storage adapter ───────────────────────

  internal DbContext OpenContext() => _contextFactory();

  internal bool OwnsContext => _ownsContext;

  // ── Fluent LINQ composition ────────────────────────────────────────────────

  /// <summary>Filters the query. Returns a new handle; does not execute.</summary>
  public DbQuery<T> Where(Expression<Func<T, bool>> predicate) =>
    WithQuery(q => q.Where(predicate));

  /// <summary>Orders ascending by <paramref name="keySelector"/>. Returns a new handle.</summary>
  public DbQuery<T> OrderBy<TKey>(Expression<Func<T, TKey>> keySelector) =>
    WithQuery(q => q.OrderBy(keySelector));

  /// <summary>Orders descending by <paramref name="keySelector"/>. Returns a new handle.</summary>
  public DbQuery<T> OrderByDescending<TKey>(Expression<Func<T, TKey>> keySelector) =>
    WithQuery(q => q.OrderByDescending(keySelector));

  /// <summary>Limits the number of rows returned. Returns a new handle.</summary>
  public DbQuery<T> Take(int count) => WithQuery(q => q.Take(count));

  /// <summary>Skips the first <paramref name="count"/> rows. Returns a new handle.</summary>
  public DbQuery<T> Skip(int count) => WithQuery(q => q.Skip(count));

  // ── Type-changing projection ────────────────────────────────────────────────

  /// <summary>
  /// Builds a deferred query of a different entity type on the same database and scope.
  /// </summary>
  /// <remarks>
  /// <para>
  /// Use this in step transforms when you need to construct a derived query (e.g., a JOIN
  /// across multiple tables) that should be saved using the fused INSERT-FROM-SELECT path:
  /// </para>
  /// <code>
  /// return shuttles.Project&lt;ModelInputSchema&gt;(ctx =>
  ///     from s in ctx.Set&lt;ShuttleSchema&gt;()
  ///     join c in ctx.Set&lt;CompanySchema&gt;() on s.CompanyId equals c.Id
  ///     select new ModelInputSchema { ... });
  /// </code>
  /// <para>
  /// The returned <c>DbQuery&lt;TResult&gt;</c> inherits the <see cref="Scope"/> and context
  /// factory of this handle, so it will match a <c>DbQueryStorageAdapter&lt;TResult&gt;</c>
  /// configured against the same database.
  /// </para>
  /// </remarks>
  /// <typeparam name="TResult">The target entity type.</typeparam>
  /// <param name="buildProjection">
  /// Function that builds the <see cref="IQueryable{TResult}"/> for a given context.
  /// The context is the same database as this handle.
  /// </param>
  public DbQuery<TResult> Project<TResult>(Func<DbContext, IQueryable<TResult>> buildProjection)
    where TResult : class => new(Label, Scope, _contextFactory, _ownsContext, buildProjection);

  // ── Materialization ────────────────────────────────────────────────────────

  /// <summary>
  /// Executes the query and returns all matching rows as a list.
  /// Applies <c>AsNoTracking()</c> automatically.
  /// </summary>
  public async Task<List<T>> ToListAsync(CancellationToken cancellationToken = default)
  {
    var context = _contextFactory();
    try
    {
      return await BuildQuery(context).AsNoTracking().ToListAsync(cancellationToken);
    }
    finally
    {
      if (_ownsContext)
        await context.DisposeAsync();
    }
  }

  // ── IEnumerable<T> sync bridge ─────────────────────────────────────────────

  /// <inheritdoc/>
  /// <remarks>
  /// Triggers synchronous materialization. Prefer <see cref="ToListAsync"/> in async step
  /// transforms to avoid blocking a thread during database I/O.
  /// </remarks>
  public IEnumerator<T> GetEnumerator() => ToListAsync().GetAwaiter().GetResult().GetEnumerator();

  IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

  // ── Private helpers ────────────────────────────────────────────────────────

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
