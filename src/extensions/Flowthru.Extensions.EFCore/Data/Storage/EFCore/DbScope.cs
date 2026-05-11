using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Flowthru.Data.Storage.EFCore;

/// <summary>
/// Identifies which database instance an EF Core lifecycle resource
/// is operating on. Used as the scope type for
/// <see cref="Flowthru.Prelude.FlowResource{TScope}"/> when the
/// catalog declares an EF Core lifecycle.
/// </summary>
/// <remarks>
/// <para>
/// Two scopes are considered the same database when they are equal
/// under their equality rule:
/// </para>
/// <list type="bullet">
/// <item><see cref="Inferred"/> — reference equality on the factory
/// object. Default for resources built from
/// <see cref="EFCoreLifecycleExtensions.EphemeralDatabase"/> /
/// <see cref="EFCoreLifecycleExtensions.EphemeralSchema"/>.</item>
/// <item><see cref="Explicit"/> — case-sensitive string equality on
/// the scope name. Use when two factory instances point at the same
/// logical database.</item>
/// </list>
/// </remarks>
public abstract class DbScope
{
  /// <summary>Scope inferred from factory object identity.</summary>
  public static DbScope Inferred(object factory) => new InferredDbScope(factory);

  /// <summary>Named scope; two scopes with the same name match.</summary>
  public static DbScope Explicit(string name) => new ExplicitDbScope(name);

  /// <summary>True if this scope refers to the same database as <paramref name="other"/>.</summary>
  [ExcludeFromCodeCoverage]
  internal virtual bool IsSameDatabase(DbScope other) => Equals(other);

  private sealed class InferredDbScope : DbScope
  {
    private readonly object _factory;
    internal InferredDbScope(object factory) => _factory = factory;

    internal override bool IsSameDatabase(DbScope other) =>
      other is InferredDbScope o && ReferenceEquals(_factory, o._factory);

    public override bool Equals(object? obj) =>
      obj is InferredDbScope o && ReferenceEquals(_factory, o._factory);

    public override int GetHashCode() => RuntimeHelpers.GetHashCode(_factory);
  }

  private sealed class ExplicitDbScope : DbScope
  {
    private readonly string _name;
    internal ExplicitDbScope(string name) => _name = name;

    internal override bool IsSameDatabase(DbScope other) =>
      other is ExplicitDbScope o && string.Equals(_name, o._name, StringComparison.Ordinal);

    public override bool Equals(object? obj) =>
      obj is ExplicitDbScope o && string.Equals(_name, o._name, StringComparison.Ordinal);

    public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(_name);
  }
}
