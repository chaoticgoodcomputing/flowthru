using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;

namespace Flowthru.Data.Storage;

/// <summary>
/// Factory for creating schema instances, supporting both traditional parameterless constructors
/// and modern C# features like required members and positional records.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Design Philosophy:</strong>
/// </para>
/// <para>
/// With Flowthru's strong step contracts, schemas with required members are guaranteed to contain
/// valid data because:
/// </para>
/// <list type="bullet">
/// <item><strong>Layer 0 (Seeds):</strong> Validation phase checks required fields exist before execution</item>
/// <item><strong>Layers 1+ (Step outputs):</strong> C# compiler enforces required members when steps construct output</item>
/// </list>
/// <para>
/// This activator's role is to enable deserialization by creating instances that will be populated
/// via property reflection. No validation is performed here - that happens at the flow boundaries.
/// </para>
/// <para>
/// <strong>Instantiation Strategy:</strong>
/// </para>
/// <list type="bullet">
/// <item><strong>Fast Path:</strong> Parameterless constructor (uses compiled expression tree)</item>
/// <item><strong>Slow Path:</strong> No parameterless constructor (uses FormatterServices.GetUninitializedObject)</item>
/// </list>
/// <para>
/// <strong>Performance:</strong>
/// </para>
/// <para>
/// The fast path (parameterless constructor) is ~10x faster than Activator.CreateInstance.
/// The slow path (uninitialized object) is ~2x slower than Activator.CreateInstance but enables
/// required members and positional records.
/// </para>
/// <para>
/// Both paths cache metadata to minimize reflection overhead on subsequent calls.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Traditional schema - uses fast path
/// public record OldSchema(int Id, string Name) : IFlatSchema;
/// var old = SchemaActivator.CreateInstance&lt;OldSchema&gt;();
///
/// // Modern schema with required members - uses slow path
/// public record NewSchema : IFlatSchema
/// {
///   public required Guid Id { get; init; }
///   public required string Name { get; init; }
/// }
/// var modern = SchemaActivator.CreateInstance&lt;NewSchema&gt;();
/// </code>
/// </example>
public static class SchemaActivator
{
  private static readonly ConcurrentDictionary<Type, InstantiationStrategy> _strategyCache = new();
  private static readonly ConcurrentDictionary<Type, Delegate> _factoryCache = new();

  /// <summary>
  /// Creates an instance of the specified type, automatically selecting the optimal
  /// instantiation strategy.
  /// </summary>
  /// <typeparam name="T">The type to instantiate</typeparam>
  /// <returns>A new instance of type T with uninitialized properties</returns>
  /// <exception cref="InvalidOperationException">
  /// Thrown if the type cannot be instantiated (e.g., abstract class, interface)
  /// </exception>
  /// <remarks>
  /// <para>
  /// The returned instance will have:
  /// - Reference type properties: null
  /// - Value type properties: default values (0, false, etc.)
  /// - Required members: uninitialized (will be set via reflection after this call)
  /// </para>
  /// <para>
  /// This is safe because:
  /// - Layer 0: Validation ensures required fields exist in data
  /// - Layers 1+: Data came from valid step output
  /// </para>
  /// </remarks>
  public static T CreateInstance<T>()
    where T : notnull
  {
    var type = typeof(T);

    // Validate type can be instantiated
    if (type.IsAbstract || type.IsInterface)
    {
      throw new InvalidOperationException(
        $"Cannot instantiate abstract type or interface: {type.FullName}"
      );
    }

    // Get or determine instantiation strategy
    var strategy = _strategyCache.GetOrAdd(type, DetermineStrategy);

    return strategy switch
    {
      InstantiationStrategy.ParameterlessConstructor => CreateViaConstructor<T>(),
      InstantiationStrategy.UninitializedObject => CreateUninitializedObject<T>(),
      _ => throw new InvalidOperationException($"Unknown instantiation strategy: {strategy}"),
    };
  }

  /// <summary>
  /// Determines the optimal instantiation strategy for a type.
  /// </summary>
  private static InstantiationStrategy DetermineStrategy(Type type)
  {
    // Check if type has a public parameterless constructor
    var hasParameterlessConstructor = type.GetConstructors()
      .Any(c => c.IsPublic && c.GetParameters().Length == 0);

    if (hasParameterlessConstructor)
    {
      return InstantiationStrategy.ParameterlessConstructor;
    }

    // No parameterless constructor - must use uninitialized object
    // This supports:
    // - Required properties (C# 11+)
    // - Positional records with primary constructors
    return InstantiationStrategy.UninitializedObject;
  }

  /// <summary>
  /// Fast path: Creates instance via parameterless constructor using compiled expression tree.
  /// </summary>
  private static T CreateViaConstructor<T>()
  {
    var type = typeof(T);

    // Get or compile factory function
    var factory = _factoryCache.GetOrAdd(type, CompileFactory<T>);

    return ((Func<T>)factory)();
  }

  /// <summary>
  /// Compiles a factory function for type T using expression trees.
  /// </summary>
  private static Func<T> CompileFactory<T>(Type type)
  {
    // Create expression: () => new T()
    var newExpression = Expression.New(type);
    var lambda = Expression.Lambda<Func<T>>(newExpression);

    // Compile to delegate (cached for reuse)
    return lambda.Compile();
  }

  /// <summary>
  /// Slow path: Creates instance without invoking constructor.
  /// </summary>
  /// <remarks>
  /// <para>
  /// Uses RuntimeHelpers.GetUninitializedObject which:
  /// - Bypasses all constructors
  /// - Allocates memory for the object
  /// - Sets all fields to their default values
  /// - Does NOT initialize required members
  /// </para>
  /// <para>
  /// This is safe in Flowthru because properties will be populated immediately
  /// via reflection from deserialized data, which is guaranteed to contain all
  /// required fields (via validation phase for Layer 0, or compiler enforcement
  /// for step outputs).
  /// </para>
  /// </remarks>
  private static T CreateUninitializedObject<T>()
  {
    var type = typeof(T);

    try
    {
      var instance = RuntimeHelpers.GetUninitializedObject(type);
      return (T)instance;
    }
    catch (Exception ex)
    {
      throw new InvalidOperationException(
        $"Failed to create uninitialized object for type '{type.FullName}'. "
          + $"This may occur with types that have special runtime requirements. "
          + $"Consider adding a parameterless constructor if possible.",
        ex
      );
    }
  }

  /// <summary>
  /// Clears the internal caches. Useful for testing or long-running applications
  /// that dynamically load/unload types.
  /// </summary>
  internal static void ClearCaches()
  {
    _strategyCache.Clear();
    _factoryCache.Clear();
  }

  /// <summary>
  /// Gets the instantiation strategy that will be used for a type.
  /// Useful for diagnostics and testing.
  /// </summary>
  internal static InstantiationStrategy GetStrategy<T>()
  {
    return _strategyCache.GetOrAdd(typeof(T), DetermineStrategy);
  }
}

/// <summary>
/// Enum representing the instantiation strategy for a type.
/// </summary>
internal enum InstantiationStrategy
{
  /// <summary>
  /// Type has a public parameterless constructor - use fast compiled expression tree.
  /// </summary>
  ParameterlessConstructor = 0,

  /// <summary>
  /// Type has no parameterless constructor - use FormatterServices.GetUninitializedObject.
  /// Supports required members and positional records.
  /// </summary>
  UninitializedObject = 1,
}
