using Flowthru.Prelude;
using Flowthru.Validation.Runtime;

namespace Flowthru.Data.Storage.Gql;

/// <summary>
/// Wraps a GQL storage adapter to declare its endpoint as a scheduler
/// conflict resource (ADR-0019, #104). Every operation delegates to the
/// inner adapter unchanged; the only addition is
/// <see cref="IHasServiceDependencies"/>, which surfaces the opt-in
/// endpoint concurrency cap so the scheduler throttles concurrent calls.
/// </summary>
/// <remarks>
/// Constructed via <c>IItem&lt;T&gt;.WithGqlConcurrency(...)</c>; mirrors
/// the <c>ConstrainedStorageAdapter</c> decorator idiom. Forwards
/// <see cref="IHasStorageKind"/> so the wrapped item keeps its
/// <c>"gql"</c> metadata shape.
/// </remarks>
internal sealed class ThrottledGqlAdapter<T> : IStorageAdapter<T>, IHasStorageKind, IHasServiceDependencies
{
  private readonly IStorageAdapter<T> _inner;
  private readonly IReadOnlyList<ServiceDependency> _serviceDependencies;

  internal ThrottledGqlAdapter(IStorageAdapter<T> inner, ServiceDependency endpointDependency)
  {
    _inner = inner ?? throw new ArgumentNullException(nameof(inner));
    _serviceDependencies = new[] { endpointDependency };
  }

  /// <inheritdoc/>
  public StorageTraits Traits => _inner.Traits;

  /// <inheritdoc/>
  public IReadOnlyList<ServiceDependency> ServiceDependencies => _serviceDependencies;

  /// <inheritdoc/>
  public string StorageKind => _inner is IHasStorageKind kinded ? kinded.StorageKind : "gql";

  /// <inheritdoc/>
  public FlowIO<T> Load() => _inner.Load();

  /// <inheritdoc/>
  public FlowIO<FlowUnit> Save(T data) => _inner.Save(data);

  /// <inheritdoc/>
  public FlowIO<bool> Exists() => _inner.Exists();

  /// <inheritdoc/>
  public FlowIO<ValidationResult> InspectShallow(int sampleSize) => _inner.InspectShallow(sampleSize);

  /// <inheritdoc/>
  public FlowIO<ValidationResult> InspectDeep() => _inner.InspectDeep();

  /// <inheritdoc/>
  public FlowIO<ValidationResult> InspectTarget() => _inner.InspectTarget();
}
