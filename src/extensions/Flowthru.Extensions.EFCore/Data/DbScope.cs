using System.Runtime.CompilerServices;

namespace Flowthru.Extensions.EFCore.Data;

/// <summary>
/// Identifies which database instance a <see cref="DbQuery{T}"/> or
/// <see cref="Flowthru.Core.Data.Storage.DbQueryStorageAdapter{T}"/> is associated with,
/// enabling the fused INSERT-FROM-SELECT save path when source and destination share the same DB.
/// </summary>
/// <remarks>
/// <para>
/// Two scopes are considered the same database when they are equal under their equality rule:
/// </para>
/// <list type="bullet">
/// <item>
///   <see cref="Inferred"/> — reference equality on the factory object.
///   The default for catalog entries created via <c>EFCoreItemFactory.Query</c>.
/// </item>
/// <item>
///   <see cref="Explicit"/> — case-sensitive string equality on the scope name.
///   Use this when two catalog entries point to the same logical database but use
///   different factory instances (e.g., two separate DI-injected factory objects).
/// </item>
/// </list>
/// </remarks>
public abstract class DbScope
{
  /// <summary>
  /// Creates a scope inferred from factory object identity.
  /// Two entries sharing the exact same factory reference are considered the same database.
  /// </summary>
  /// <param name="factory">The factory object whose reference identity keys this scope.</param>
  public static DbScope Inferred(object factory) => new InferredDbScope(factory);

  /// <summary>
  /// Creates a named scope.
  /// Two entries with the same <paramref name="name"/> are considered the same database
  /// regardless of factory instance identity.
  /// </summary>
  /// <param name="name">Case-sensitive scope name.</param>
  public static DbScope Explicit(string name) => new ExplicitDbScope(name);

  /// <summary>
  /// Returns <see langword="true"/> if this scope refers to the same database as <paramref name="other"/>.
  /// </summary>
  /// <remarks>
  /// Virtual hook for future subclasses that match by connection string or other criteria.
  /// </remarks>
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
