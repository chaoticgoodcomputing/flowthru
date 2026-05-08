using Flowthru.Prelude;
using Flowthru.Validation.Runtime;
using StrawberryShake;

namespace Flowthru.Data.Storage.Gql;

/// <summary>
/// Storage adapter for a single-item GraphQL query using a
/// StrawberryShake client. Implements <see cref="IStorageAdapter{T}"/>
/// directly — GraphQL inherently couples transport, serialisation, and
/// schema in the generated client, so the medium × format × container
/// composition that file-based formats use does not apply.
/// </summary>
/// <typeparam name="TResult">
/// The StrawberryShake-generated result data type
/// (e.g. <c>IGetCurrentUserResult</c>).
/// </typeparam>
/// <typeparam name="T">
/// The target type surfaced to the catalog item (selected from
/// <typeparamref name="TResult"/> via <c>selectData</c>).
/// </typeparam>
/// <remarks>
/// <para>
/// <strong>StrawberryShake boundary.</strong> The extension does not
/// own or configure the StrawberryShake client — the caller brings
/// their own configured client (registered via DI). The adapter wraps
/// operation delegate invocations in <see cref="FlowIO{A}"/> effects
/// and maps GraphQL errors into typed
/// <see cref="RuntimeError.External"/> failures.
/// </para>
/// <para>
/// <strong>Mutation support.</strong> Providing a <c>mutationFunc</c>
/// enables <see cref="Save"/>; otherwise <c>Traits.CanWrite</c> is
/// <c>false</c> and <see cref="Save"/> short-circuits as
/// <see cref="RuntimeError.External"/>.
/// </para>
/// <para>
/// <strong>Pre-flight inspection.</strong> <see cref="InspectShallow"/>
/// executes the full query against the live endpoint to validate
/// reachability, authentication, and schema compatibility. For
/// single-item queries the query itself is the minimal probe.
/// </para>
/// </remarks>
public sealed class GqlSingleStorageAdapter<TResult, T> : IStorageAdapter<T>
  where TResult : class
  where T : class
{
  private readonly string _label;
  private readonly Func<CancellationToken, Task<IOperationResult<TResult>>> _queryFunc;
  private readonly Func<TResult, T> _selectData;
  private readonly Func<T, CancellationToken, Task<IOperationResult>>? _mutationFunc;
  private readonly bool _allowEmptyData;

  /// <summary>Read-only single-item GQL adapter.</summary>
  public GqlSingleStorageAdapter(
    string label,
    Func<CancellationToken, Task<IOperationResult<TResult>>> queryFunc,
    Func<TResult, T> selectData,
    bool allowEmptyData = false
  )
    : this(label, queryFunc, selectData, mutationFunc: null, allowEmptyData) { }

  /// <summary>Read-write single-item GQL adapter.</summary>
  public GqlSingleStorageAdapter(
    string label,
    Func<CancellationToken, Task<IOperationResult<TResult>>> queryFunc,
    Func<TResult, T> selectData,
    Func<T, CancellationToken, Task<IOperationResult>>? mutationFunc,
    bool allowEmptyData = false
  )
  {
    _label = label ?? throw new ArgumentNullException(nameof(label));
    _queryFunc = queryFunc ?? throw new ArgumentNullException(nameof(queryFunc));
    _selectData = selectData ?? throw new ArgumentNullException(nameof(selectData));
    _mutationFunc = mutationFunc;
    _allowEmptyData = allowEmptyData;

    Traits = new StorageTraits
    {
      CanWrite = mutationFunc is not null,
      IsPersistent = false,
    };
  }

  /// <inheritdoc/>
  public StorageTraits Traits { get; }

  /// <inheritdoc/>
  public FlowIO<T> Load() =>
    FlowIO.LiftAsync(async ct =>
    {
      var result = await _queryFunc(ct).ConfigureAwait(false);
      result.EnsureNoErrors();

      if (result.Data is null)
      {
        throw new InvalidOperationException(
          $"GraphQL query for '{_label}' returned null data with no errors. "
            + "Verify the query returns a non-null field, or set allowEmptyData: true."
        );
      }

      return _selectData(result.Data);
    }, source: $"GqlSingleStorageAdapter.Load[{_label}]");

  /// <inheritdoc/>
  public FlowIO<FlowUnit> Save(T data)
  {
    if (_mutationFunc is null)
    {
      return FlowIO.Fail<FlowUnit>(new RuntimeError.External(
        $"GqlSingleStorageAdapter.Save[{_label}]",
        new InvalidOperationException(
          $"Cannot write to GQL adapter '{_label}': no mutation delegate was provided. "
            + "Supply a mutationFunc when creating the catalog item, or constrain the item with CanWrite = false."
        )));
    }

    return FlowIO.LiftAsync(async ct =>
    {
      var result = await _mutationFunc(data, ct).ConfigureAwait(false);
      result.EnsureNoErrors();
      return FlowUnit.Default;
    }, source: $"GqlSingleStorageAdapter.Save[{_label}]");
  }

  /// <inheritdoc/>
  public FlowIO<bool> Exists() =>
    FlowIO.LiftAsync(async ct =>
    {
      try
      {
        var result = await _queryFunc(ct).ConfigureAwait(false);
        return !result.Errors.Any() && result.Data is not null;
      }
      catch
      {
        return false;
      }
    }, source: $"GqlSingleStorageAdapter.Exists[{_label}]");

  /// <inheritdoc/>
  /// <remarks>
  /// Executes the full query against the live endpoint. For
  /// single-item queries the query itself is the minimal probe — it
  /// validates endpoint reachability, authentication, and that the
  /// server accepts the query shape.
  /// </remarks>
  public FlowIO<ValidationResult> InspectShallow(int sampleSize) =>
    FlowIO.LiftAsync(async ct =>
    {
      IOperationResult<TResult> result;
      try
      {
        result = await _queryFunc(ct).ConfigureAwait(false);
      }
      catch (Exception ex)
      {
        return ValidationResult.Failure(
          catalogKey: _label,
          errorType: ValidationErrorType.NotFound,
          message: $"GraphQL endpoint for '{_label}' is unreachable.",
          details: ex.Message
        );
      }

      if (result.Errors.Any())
      {
        var details = string.Join("; ", result.Errors.Select(e => e.Message));
        return ValidationResult.Failure(
          catalogKey: _label,
          errorType: ValidationErrorType.InspectionFailure,
          message: $"GraphQL query for '{_label}' returned errors.",
          details: details
        );
      }

      if (result.Data is null && !_allowEmptyData)
      {
        return ValidationResult.Failure(
          catalogKey: _label,
          errorType: ValidationErrorType.EmptyDataset,
          message: $"GraphQL query for '{_label}' returned null data.",
          details: "Set allowEmptyData: true when creating the catalog item if null data is valid for this query."
        );
      }

      return ValidationResult.Success();
    }, source: $"GqlSingleStorageAdapter.InspectShallow[{_label}]");

  /// <inheritdoc/>
  /// <remarks>
  /// For single-item queries, deep inspection is equivalent to shallow
  /// inspection — there is only one result to validate.
  /// </remarks>
  public FlowIO<ValidationResult> InspectDeep() => InspectShallow(sampleSize: 0);

  /// <inheritdoc/>
  /// <remarks>
  /// GQL mutations cannot be probed without side effects. Endpoint
  /// reachability is already validated by <see cref="InspectShallow"/>
  /// on the read-side query.
  /// </remarks>
  public FlowIO<ValidationResult> InspectTarget() => FlowIO.Pure(ValidationResult.Success());
}
