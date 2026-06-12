using System.Linq.Expressions;
using System.Reflection;
using Flowthru.Prelude;
using Flowthru.Validation.Runtime;

namespace Flowthru.Data.Storage.EFCore;

/// <summary>
/// Wraps an <see cref="EFCoreStorageAdapter{T}"/> and exposes the
/// <see cref="ISupportsFingerprint"/> capability — the EF Core item
/// participates in Flowthru's cache plan by querying a small
/// aggregate (<c>SELECT COUNT(*), MAX(&lt;column&gt;) FROM
/// &lt;table&gt;</c>) during pre-flight.
/// </summary>
/// <remarks>
/// <para>
/// Constructed via
/// <see cref="EFCoreStorageAdapter{T}.WithFingerprintColumn(Expression{Func{T, DateTime}})"/>;
/// catalog authors don't typically name this type directly. EF Core
/// storage adapters without a fingerprint column simply do not
/// implement <see cref="ISupportsFingerprint"/>, which the cache
/// plan interprets as "this item is uncacheable."
/// </para>
/// <para>
/// All <see cref="IStorageAdapter{T}"/> operations are forwarded
/// verbatim to the wrapped adapter; the wrapper exists solely to
/// declare the fingerprint capability and route the
/// <see cref="Fingerprint"/> call back into the EF Core adapter's
/// aggregate query path.
/// </para>
/// </remarks>
/// <typeparam name="T">Entity type — must match the wrapped adapter's element type.</typeparam>
public sealed class EFCoreFingerprintingStorageAdapter<T>
  : IStorageAdapter<IEnumerable<T>>, IHasEfficientCount, ISupportsFingerprint, IHasServiceDependencies
  where T : class
{
  private readonly EFCoreStorageAdapter<T> _inner;
  private readonly string _columnName;

  internal EFCoreFingerprintingStorageAdapter(
    EFCoreStorageAdapter<T> inner,
    LambdaExpression columnSelector
  )
  {
    _inner = inner ?? throw new ArgumentNullException(nameof(inner));
    _columnName = ResolveColumnName(columnSelector);
  }

  /// <summary>The wrapped adapter — exposed for advanced consumers and tests.</summary>
  public EFCoreStorageAdapter<T> Inner => _inner;

  /// <summary>The reflected fingerprint column name.</summary>
  public string FingerprintColumnName => _columnName;

  /// <inheritdoc/>
  public StorageTraits Traits => _inner.Traits;

  /// <inheritdoc/>
  public IReadOnlyList<ServiceDependency> ServiceDependencies =>
    ((IHasServiceDependencies)_inner).ServiceDependencies;

  /// <inheritdoc/>
  public FlowIO<IEnumerable<T>> Load() => _inner.Load();

  /// <inheritdoc/>
  public FlowIO<FlowUnit> Save(IEnumerable<T> data) => _inner.Save(data);

  /// <inheritdoc/>
  public FlowIO<bool> Exists() => _inner.Exists();

  /// <inheritdoc/>
  public FlowIO<ValidationResult> InspectShallow(int sampleSize) =>
    _inner.InspectShallow(sampleSize);

  /// <inheritdoc/>
  public FlowIO<ValidationResult> InspectDeep() => _inner.InspectDeep();

  /// <inheritdoc/>
  public FlowIO<ValidationResult> InspectTarget() => _inner.InspectTarget();

  /// <inheritdoc/>
  FlowIO<int> IHasEfficientCount.GetCountAsync() =>
    ((IHasEfficientCount)_inner).GetCountAsync();

  /// <inheritdoc/>
  /// <remarks>
  /// Routes through the wrapped adapter's
  /// <see cref="EFCoreStorageAdapter{T}.ComputeFingerprint"/> path,
  /// which issues a server-side <c>COUNT(*) + MAX(&lt;column&gt;)</c>
  /// query and SHA-256-digests the result. Cheap by design — no
  /// rows are materialised to the host.
  /// </remarks>
  public FlowIO<string> Fingerprint() => _inner.ComputeFingerprint(_columnName);

  // ── Column-selector reflection ─────────────────────────────────────

  private static string ResolveColumnName(LambdaExpression selector)
  {
    // Unwrap a Convert (DateTime → DateTime?, etc.) the C# compiler
    // sometimes emits in nullable-coercion selectors.
    var body = selector.Body;
    while (body is UnaryExpression unary && body.NodeType == ExpressionType.Convert)
    {
      body = unary.Operand;
    }

    // Must be a property access rooted in the lambda's parameter —
    // i.e. `e => e.UpdatedAt`. Reject static accesses like
    // `DateTime.UtcNow` and computed expressions outright.
    if (body is MemberExpression member
        && member.Member is PropertyInfo property
        && member.Expression is ParameterExpression parameter
        && selector.Parameters.Count == 1
        && parameter == selector.Parameters[0])
    {
      return property.Name;
    }

    throw new ArgumentException(
      $"WithFingerprintColumn expects a simple property selector like "
      + $"'e => e.UpdatedAt' but received: {selector}. Nested expressions, "
      + $"method calls, and computed values are not supported.",
      nameof(selector)
    );
  }
}
