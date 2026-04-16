using Flowthru.Core.Data.Capabilities;
using Flowthru.Core.Data.Validation;
using Flowthru.Core.Effects;

namespace Flowthru.Core.Data.Storage;

/// <summary>
/// Storage adapter that holds a <see cref="GqlQuery{TResult,T}"/> handle.
/// </summary>
/// <remarks>
/// <para>
/// Catalog entries backed by this adapter surface a <em>deferred</em> query handle rather
/// than an eagerly-fetched <c>IEnumerable&lt;T&gt;</c>. No network I/O happens during
/// <see cref="Load"/>; the handle is passed by value to the step, which decides when to
/// materialize by calling <c>ToList</c> / <c>ToListAsync</c>.
/// </para>
/// <para>
/// <see cref="InspectShallow"/> makes a single-page probe query to validate endpoint
/// reachability and schema compatibility before pipeline execution begins. This is the
/// only network call that occurs during pre-flight — the full dataset is never fetched.
/// </para>
/// </remarks>
public sealed class GqlQueryStorageAdapter<TResult, T>
  : IStorageAdapter<Extensions.GQL.Data.GqlQuery<TResult, T>>
  where TResult : class
  where T : class
{
  private readonly Extensions.GQL.Data.GqlQuery<TResult, T> _query;

  /// <param name="query">
  /// The pre-built deferred query handle. Constructed by
  /// <see cref="Extensions.GQL.Data.GqlItemFactory.Query"/>.
  /// </param>
  public GqlQueryStorageAdapter(Extensions.GQL.Data.GqlQuery<TResult, T> query)
  {
    _query = query ?? throw new ArgumentNullException(nameof(query));
    Traits = new StorageTraits { RequiresNetwork = true, CanWrite = false };
  }

  /// <inheritdoc/>
  public StorageTraits Traits { get; }

  /// <inheritdoc/>
  /// <remarks>Returns the deferred query handle synchronously — no network I/O.</remarks>
  public FlowIO<Extensions.GQL.Data.GqlQuery<TResult, T>> Load() => FlowIO.Lift(() => _query);

  /// <inheritdoc/>
  /// <remarks>GQL query handles are read-only; this always returns a failure effect.</remarks>
  public FlowIO<FlowUnit> Save(Extensions.GQL.Data.GqlQuery<TResult, T> data) =>
    FlowIO.Fail<FlowUnit>(
      new NotSupportedException(
        $"GQL query catalog entries are read-only. '{_query.Label}' does not support Save()."
      )
    );

  /// <inheritdoc/>
  /// <remarks>The query handle is always present at catalog construction time.</remarks>
  public FlowIO<bool> Exists() => FlowIO.Lift(() => true);

  /// <inheritdoc/>
  /// <remarks>
  /// Executes a minimal single-page probe to validate that the endpoint is reachable,
  /// authentication succeeds, and the schema matches before any step runs.
  /// </remarks>
  public FlowIO<ValidationResult> InspectShallow(int sampleSize) =>
    FlowIO.LiftAsync(
      async (ct) =>
      {
        try
        {
          var reachable = await Extensions.GQL.Data.GqlQueryExecutor.ProbeAsync(_query, ct);

          return reachable
            ? ValidationResult.Success()
            : ValidationResult.Failure(
              catalogKey: _query.Label,
              errorType: ValidationErrorType.NotFound,
              message: $"GraphQL endpoint for '{_query.Label}' is unreachable or returned no data.",
              details: "Verify the endpoint URL, authentication, and that the query is valid."
            );
        }
        catch (Exception ex)
        {
          return ValidationResult.FromException(_query.Label, ex);
        }
      }
    );

  /// <inheritdoc/>
  public FlowIO<ValidationResult> InspectDeep() =>
    FlowIO.LiftAsync(
      async (ct) =>
      {
        try
        {
          await _query.ToListAsync(ct);
          return ValidationResult.Success();
        }
        catch (Exception ex)
        {
          return ValidationResult.FromException(_query.Label, ex);
        }
      }
    );
}

/// <summary>
/// Storage adapter that holds a <see cref="GqlQuery{TFilter,TResult,T}"/> handle.
/// </summary>
/// <remarks>
/// Identical to <see cref="GqlQueryStorageAdapter{TResult,T}"/> but for the filtered variant.
/// The pre-flight probe is executed with a <see langword="null"/> filter to validate connectivity
/// independently of any runtime-supplied filter value.
/// </remarks>
public sealed class GqlQueryStorageAdapter<TFilter, TResult, T>
  : IStorageAdapter<Extensions.GQL.Data.GqlQuery<TFilter, TResult, T>>
  where TFilter : class
  where TResult : class
  where T : class
{
  private readonly Extensions.GQL.Data.GqlQuery<TFilter, TResult, T> _query;

  /// <param name="query">The pre-built deferred filtered query handle.</param>
  public GqlQueryStorageAdapter(Extensions.GQL.Data.GqlQuery<TFilter, TResult, T> query)
  {
    _query = query ?? throw new ArgumentNullException(nameof(query));
    Traits = new StorageTraits { RequiresNetwork = true, CanWrite = false };
  }

  /// <inheritdoc/>
  public StorageTraits Traits { get; }

  /// <inheritdoc/>
  public FlowIO<Extensions.GQL.Data.GqlQuery<TFilter, TResult, T>> Load() =>
    FlowIO.Lift(() => _query);

  /// <inheritdoc/>
  public FlowIO<FlowUnit> Save(Extensions.GQL.Data.GqlQuery<TFilter, TResult, T> data) =>
    FlowIO.Fail<FlowUnit>(
      new NotSupportedException(
        $"GQL query catalog entries are read-only. '{_query.Label}' does not support Save()."
      )
    );

  /// <inheritdoc/>
  public FlowIO<bool> Exists() => FlowIO.Lift(() => true);

  /// <inheritdoc/>
  /// <remarks>
  /// Probes with a <see langword="null"/> filter to validate connectivity without requiring
  /// a filter value from a step.
  /// </remarks>
  public FlowIO<ValidationResult> InspectShallow(int sampleSize) =>
    FlowIO.LiftAsync(
      async (ct) =>
      {
        try
        {
          var reachable = await Extensions.GQL.Data.GqlQueryExecutor.FilteredProbeAsync(_query, ct);

          return reachable
            ? ValidationResult.Success()
            : ValidationResult.Failure(
              catalogKey: _query.Label,
              errorType: ValidationErrorType.NotFound,
              message: $"GraphQL endpoint for '{_query.Label}' is unreachable or returned no data.",
              details: "Verify the endpoint URL, authentication, and that the query is valid."
            );
        }
        catch (Exception ex)
        {
          return ValidationResult.FromException(_query.Label, ex);
        }
      }
    );

  /// <inheritdoc/>
  public FlowIO<ValidationResult> InspectDeep() =>
    FlowIO.LiftAsync(
      async (ct) =>
      {
        try
        {
          await _query.ToListAsync(ct);
          return ValidationResult.Success();
        }
        catch (Exception ex)
        {
          return ValidationResult.FromException(_query.Label, ex);
        }
      }
    );
}
