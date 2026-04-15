using Flowthru.Core.Data.Capabilities;
using Flowthru.Core.Data.Validation;
using Flowthru.Core.Effects;
using StrawberryShake;

namespace Flowthru.Core.Data.Storage;

/// <summary>
/// Storage adapter for a single-item GraphQL query using a StrawberryShake client.
/// </summary>
/// <typeparam name="TResult">
/// The StrawberryShake-generated result data type (e.g. <c>IGetCurrentUserResult</c>).
/// Must satisfy the <c>class</c> constraint imposed by <see cref="IOperationResult{TResultData}"/>.
/// </typeparam>
/// <typeparam name="T">
/// The target type surfaced to the Flowthru catalog entry (e.g. <c>GetCurrentUser_User</c>).
/// Selected from <typeparamref name="TResult"/> via the <c>selectData</c> delegate.
/// </typeparam>
/// <remarks>
/// <para>
/// <strong>Design Rationale:</strong>
/// </para>
/// <para>
/// This is a specialized adapter that directly implements <see cref="IStorageAdapter{T}"/>
/// rather than the Medium→Format→Container composition pattern. GraphQL inherently
/// couples transport (HTTP/WebSocket), serialization (JSON), and schema in the generated
/// client — decomposing them would fight StrawberryShake's architecture.
/// </para>
/// <para>
/// <strong>StrawberryShake Boundary:</strong>
/// </para>
/// <para>
/// This extension does not own or configure the StrawberryShake client — the caller
/// brings their own configured client (registered via DI). The extension wraps operation
/// delegate invocations in <see cref="FlowIO{A}"/> effects, mapping GQL errors to
/// structured <see cref="ValidationResult"/> or <see cref="FlowIO"/> failures.
/// </para>
/// <para>
/// <strong>Mutation Support:</strong>
/// </para>
/// <para>
/// Providing a <c>mutationFunc</c> enables <see cref="Save"/>. When omitted,
/// <c>StorageTraits.CanWrite</c> is set to <c>false</c> and <see cref="Save"/> fails fast.
/// </para>
/// <para>
/// <strong>Pre-flight Validation:</strong>
/// </para>
/// <para>
/// <see cref="InspectShallow"/> executes the full query against the live endpoint to
/// validate reachability, authentication, and schema compatibility before any pipeline step
/// runs. For single-item queries the query itself is the minimal probe.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Read-only single-item query
/// var adapter = new GqlStorageAdapter&lt;IGetCurrentUserResult, GetCurrentUser_Me&gt;(
///     label: "current-user",
///     queryFunc: ct => _client.GetCurrentUser.ExecuteAsync(ct),
///     selectData: r => r.Me!
/// );
///
/// // With mutation support
/// var adapter = new GqlStorageAdapter&lt;IGetCurrentUserResult, GetCurrentUser_Me&gt;(
///     label: "current-user",
///     queryFunc: ct => _client.GetCurrentUser.ExecuteAsync(ct),
///     selectData: r => r.Me!,
///     mutationFunc: (data, ct) => _client.UpdateCurrentUser.ExecuteAsync(data.Name, ct)
/// );
/// </code>
/// </example>
public sealed class GqlStorageAdapter<TResult, T> : IStorageAdapter<T>
  where TResult : class
  where T : class
{
  private readonly string _label;
  private readonly Func<CancellationToken, Task<IOperationResult<TResult>>> _queryFunc;
  private readonly Func<TResult, T> _selectData;
  private readonly Func<T, CancellationToken, Task<IOperationResult>>? _mutationFunc;
  private readonly bool _allowEmptyData;

  /// <summary>
  /// Creates a read-only single-item GQL adapter.
  /// </summary>
  /// <param name="label">The catalog entry label, used in validation error messages.</param>
  /// <param name="queryFunc">Delegate that executes the StrawberryShake query operation.</param>
  /// <param name="selectData">Projects the result data type to the target type <typeparamref name="T"/>.</param>
  /// <param name="allowEmptyData">
  /// If <c>true</c>, a <c>null</c> <see cref="IOperationResult{TResultData}.Data"/> is treated
  /// as valid during inspection. Defaults to <c>false</c>.
  /// </param>
  public GqlStorageAdapter(
    string label,
    Func<CancellationToken, Task<IOperationResult<TResult>>> queryFunc,
    Func<TResult, T> selectData,
    bool allowEmptyData = false
  )
    : this(label, queryFunc, selectData, mutationFunc: null, allowEmptyData) { }

  /// <summary>
  /// Creates a read-write single-item GQL adapter.
  /// </summary>
  /// <param name="label">The catalog entry label, used in validation error messages.</param>
  /// <param name="queryFunc">Delegate that executes the StrawberryShake query operation.</param>
  /// <param name="selectData">Projects the result data type to the target type <typeparamref name="T"/>.</param>
  /// <param name="mutationFunc">
  /// Delegate that executes the StrawberryShake mutation operation for <see cref="Save"/>.
  /// When provided, <c>StorageTraits.CanWrite</c> is set to <c>true</c>.
  /// </param>
  /// <param name="allowEmptyData">
  /// If <c>true</c>, a <c>null</c> <see cref="IOperationResult{TResultData}.Data"/> is treated
  /// as valid during inspection. Defaults to <c>false</c>.
  /// </param>
  public GqlStorageAdapter(
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

    Traits = new StorageTraits { RequiresNetwork = true, CanWrite = mutationFunc != null };
  }

  /// <inheritdoc/>
  public StorageTraits Traits { get; }

  /// <inheritdoc/>
  public FlowIO<T> Load() =>
    FlowIO.LiftAsync(
      async (ct) =>
      {
        var result = await _queryFunc(ct);
        result.EnsureNoErrors();

        if (result.Data is null)
        {
          throw new InvalidOperationException(
            $"GraphQL query for '{_label}' returned null data with no errors. "
              + "Verify the query returns a non-null field, or set allowEmptyData: true."
          );
        }

        return _selectData(result.Data);
      }
    );

  /// <inheritdoc/>
  public FlowIO<FlowUnit> Save(T data)
  {
    if (_mutationFunc is null)
    {
      return FlowIO.Fail<FlowUnit>(
        new InvalidOperationException(
          $"Cannot write to GQL adapter '{_label}': no mutation delegate was provided. "
            + "Supply a mutationFunc when creating the catalog entry, or constrain the entry with CanWrite = false."
        )
      );
    }

    return FlowIO.LiftAsync(
      async (ct) =>
      {
        var result = await _mutationFunc(data, ct);
        result.EnsureNoErrors();
        return FlowUnit.Default;
      }
    );
  }

  /// <inheritdoc/>
  public FlowIO<bool> Exists() =>
    FlowIO.LiftAsync(
      async (ct) =>
      {
        try
        {
          var result = await _queryFunc(ct);
          return !result.Errors.Any() && result.Data is not null;
        }
        catch
        {
          return false;
        }
      }
    );

  /// <inheritdoc/>
  /// <remarks>
  /// Executes the full query against the live endpoint. For single-item queries the
  /// query itself is the minimal viable probe — it validates endpoint reachability,
  /// authentication, and that the server accepts the query shape.
  /// </remarks>
  public FlowIO<ValidationResult> InspectShallow(int sampleSize) =>
    FlowIO.LiftAsync(
      async (ct) =>
      {
        IOperationResult<TResult> result;
        try
        {
          result = await _queryFunc(ct);
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
            details: "Set allowEmptyData: true when creating the catalog entry if null data is valid for this query."
          );
        }

        return ValidationResult.Success();
      }
    );

  /// <inheritdoc/>
  /// <remarks>
  /// For single-item queries, deep inspection is equivalent to shallow inspection —
  /// there is only one result to validate.
  /// </remarks>
  public FlowIO<ValidationResult> InspectDeep() => InspectShallow(sampleSize: 0);
}
