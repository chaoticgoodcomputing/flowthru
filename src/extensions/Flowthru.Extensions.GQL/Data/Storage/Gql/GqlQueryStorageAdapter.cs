using Flowthru.Data.Storage.Gql.Internal;
using Flowthru.Prelude;
using Flowthru.Validation.Runtime;

namespace Flowthru.Data.Storage.Gql;

/// <summary>
/// Storage adapter that holds a <see cref="GqlQuery{TResult,T}"/>
/// handle. Catalog items backed by this adapter surface a
/// <em>deferred</em> query handle rather than an eagerly-fetched
/// collection — no network I/O happens during <see cref="Load"/>.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="InspectShallow"/> issues a single-page probe to
/// validate endpoint reachability and schema compatibility before
/// pipeline execution begins; the full dataset is never fetched
/// during pre-flight.
/// </para>
/// </remarks>
public sealed class GqlQueryStorageAdapter<TResult, T> : IStorageAdapter<GqlQuery<TResult, T>>
  where TResult : class
  where T : class
{
  private readonly GqlQuery<TResult, T> _query;

  /// <param name="query">The pre-built deferred query handle.</param>
  public GqlQueryStorageAdapter(GqlQuery<TResult, T> query)
  {
    _query = query ?? throw new ArgumentNullException(nameof(query));
    Traits = new StorageTraits { CanWrite = false, IsPersistent = false };
  }

  /// <inheritdoc/>
  public StorageTraits Traits { get; }

  /// <inheritdoc/>
  /// <remarks>Returns the deferred query handle synchronously — no network I/O.</remarks>
  public FlowIO<GqlQuery<TResult, T>> Load() => FlowIO.Lift(() => _query);

  /// <inheritdoc/>
  public FlowIO<FlowUnit> Save(GqlQuery<TResult, T> data) =>
    FlowIO.Fail<FlowUnit>(new RuntimeError.External(
      $"GqlQueryStorageAdapter.Save[{_query.Label}]",
      new NotSupportedException(
        $"GQL query catalog items are read-only. '{_query.Label}' does not support Save()."
      )));

  /// <inheritdoc/>
  public FlowIO<bool> Exists() => FlowIO.Lift(() => true);

  /// <inheritdoc/>
  public FlowIO<ValidationResult> InspectShallow(int sampleSize) =>
    FlowIO.LiftAsync(async ct =>
    {
      try
      {
        var reachable = await GqlQueryExecutor.ProbeAsync(_query, ct).ConfigureAwait(false);

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
    }, source: $"GqlQueryStorageAdapter.InspectShallow[{_query.Label}]");

  /// <inheritdoc/>
  public FlowIO<ValidationResult> InspectDeep() =>
    FlowIO.LiftAsync(async ct =>
    {
      try
      {
        await _query.ToListAsync(ct).ConfigureAwait(false);
        return ValidationResult.Success();
      }
      catch (Exception ex)
      {
        return ValidationResult.FromException(_query.Label, ex);
      }
    }, source: $"GqlQueryStorageAdapter.InspectDeep[{_query.Label}]");

  /// <inheritdoc/>
  public FlowIO<ValidationResult> InspectTarget() => FlowIO.Pure(ValidationResult.Success());
}

/// <summary>
/// Storage adapter that holds a <see cref="GqlQuery{TFilter,TResult,T}"/>
/// handle — the filtered variant. Pre-flight probes with a
/// <see langword="null"/> filter to validate connectivity
/// independently of any runtime-supplied filter value.
/// </summary>
public sealed class GqlQueryStorageAdapter<TFilter, TResult, T>
  : IStorageAdapter<GqlQuery<TFilter, TResult, T>>
  where TFilter : class
  where TResult : class
  where T : class
{
  private readonly GqlQuery<TFilter, TResult, T> _query;

  /// <param name="query">The pre-built deferred filtered query handle.</param>
  public GqlQueryStorageAdapter(GqlQuery<TFilter, TResult, T> query)
  {
    _query = query ?? throw new ArgumentNullException(nameof(query));
    Traits = new StorageTraits { CanWrite = false, IsPersistent = false };
  }

  /// <inheritdoc/>
  public StorageTraits Traits { get; }

  /// <inheritdoc/>
  public FlowIO<GqlQuery<TFilter, TResult, T>> Load() => FlowIO.Lift(() => _query);

  /// <inheritdoc/>
  public FlowIO<FlowUnit> Save(GqlQuery<TFilter, TResult, T> data) =>
    FlowIO.Fail<FlowUnit>(new RuntimeError.External(
      $"GqlQueryStorageAdapter.Save[{_query.Label}]",
      new NotSupportedException(
        $"GQL query catalog items are read-only. '{_query.Label}' does not support Save()."
      )));

  /// <inheritdoc/>
  public FlowIO<bool> Exists() => FlowIO.Lift(() => true);

  /// <inheritdoc/>
  public FlowIO<ValidationResult> InspectShallow(int sampleSize) =>
    FlowIO.LiftAsync(async ct =>
    {
      try
      {
        var reachable = await GqlQueryExecutor.FilteredProbeAsync(_query, ct).ConfigureAwait(false);

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
    }, source: $"GqlQueryStorageAdapter.InspectShallow[{_query.Label}]");

  /// <inheritdoc/>
  public FlowIO<ValidationResult> InspectDeep() =>
    FlowIO.LiftAsync(async ct =>
    {
      try
      {
        await _query.ToListAsync(ct).ConfigureAwait(false);
        return ValidationResult.Success();
      }
      catch (Exception ex)
      {
        return ValidationResult.FromException(_query.Label, ex);
      }
    }, source: $"GqlQueryStorageAdapter.InspectDeep[{_query.Label}]");

  /// <inheritdoc/>
  public FlowIO<ValidationResult> InspectTarget() => FlowIO.Pure(ValidationResult.Success());
}
