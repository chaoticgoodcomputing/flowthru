using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;

namespace Flowthru.Data.Storage;

/// <summary>
/// Factory for creating schema instances during deserialization. Handles
/// both traditional parameterless-constructor records and modern records
/// with <c>required</c> members or positional constructors.
/// </summary>
/// <remarks>
/// <para>
/// Properties on the returned instance are populated by the format
/// converter via reflection after construction; this activator's role is
/// only to allocate. Validation happens at the Flow boundaries — pre-flight
/// for raw inputs, type-checking for step outputs — not here.
/// </para>
/// <para>
/// Two strategies, cached per type:
/// </para>
/// <list type="bullet">
/// <item><strong>Fast path</strong>: parameterless constructor via compiled expression tree.</item>
/// <item><strong>Slow path</strong>: <see cref="RuntimeHelpers.GetUninitializedObject"/> for types
///   with <c>required</c> members or positional records.</item>
/// </list>
/// </remarks>
public static class SchemaActivator
{
  private static readonly ConcurrentDictionary<Type, InstantiationStrategy> _strategyCache = new();
  private static readonly ConcurrentDictionary<Type, Delegate> _factoryCache = new();

  /// <summary>Creates an instance of <typeparamref name="T"/> using the optimal strategy.</summary>
  public static T CreateInstance<T>()
    where T : notnull
  {
    var type = typeof(T);
    if (type.IsAbstract || type.IsInterface)
    {
      throw new InvalidOperationException(
        $"Cannot instantiate abstract type or interface: {type.FullName}"
      );
    }

    var strategy = _strategyCache.GetOrAdd(type, DetermineStrategy);
    return strategy switch
    {
      InstantiationStrategy.ParameterlessConstructor => CreateViaConstructor<T>(),
      InstantiationStrategy.UninitializedObject => CreateUninitializedObject<T>(),
      _ => throw new InvalidOperationException($"Unknown instantiation strategy: {strategy}"),
    };
  }

  private static InstantiationStrategy DetermineStrategy(Type type)
  {
    var hasParameterlessConstructor = type.GetConstructors()
      .Any(c => c.IsPublic && c.GetParameters().Length == 0);
    return hasParameterlessConstructor
      ? InstantiationStrategy.ParameterlessConstructor
      : InstantiationStrategy.UninitializedObject;
  }

  private static T CreateViaConstructor<T>()
  {
    var factory = _factoryCache.GetOrAdd(typeof(T), CompileFactory<T>);
    return ((Func<T>)factory)();
  }

  private static Func<T> CompileFactory<T>(Type type)
  {
    var newExpression = Expression.New(type);
    var lambda = Expression.Lambda<Func<T>>(newExpression);
    return lambda.Compile();
  }

  private static T CreateUninitializedObject<T>()
  {
    try
    {
      var instance = RuntimeHelpers.GetUninitializedObject(typeof(T));
      return (T)instance;
    }
    catch (Exception ex)
    {
      throw new InvalidOperationException(
        $"Failed to create uninitialized object for type '{typeof(T).FullName}'. "
          + "Consider adding a parameterless constructor.",
        ex
      );
    }
  }

  private enum InstantiationStrategy
  {
    ParameterlessConstructor = 0,
    UninitializedObject = 1,
  }
}
