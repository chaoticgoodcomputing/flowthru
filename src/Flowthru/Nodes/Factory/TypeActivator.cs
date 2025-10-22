using System.Collections.Concurrent;
using System.Linq.Expressions;

namespace Flowthru.Nodes.Factory;

/// <summary>
/// Factory for creating instances of types using compiled expression trees for performance.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Design Pattern:</strong> Factory Pattern with caching - creates instances of types
/// using reflection on first call, then caches compiled expression trees for subsequent calls.
/// </para>
/// <para>
/// <strong>Performance:</strong>
/// - First call uses Expression.Compile() which has overhead
/// - Subsequent calls use cached delegate which is nearly as fast as `new T()`
/// - Significantly faster than Activator.CreateInstance&lt;T&gt;() for repeated calls
/// </para>
/// <para>
/// <strong>Inspiration:</strong> ChainSharp uses similar pattern for node instantiation.
/// </para>
/// <para>
/// <strong>Thread Safety:</strong> This class is thread-safe. Multiple threads can safely
/// call Create&lt;T&gt;() concurrently.
/// </para>
/// </remarks>
public static class TypeActivator {
  private static readonly ConcurrentDictionary<Type, Delegate> _factoryCache = new();

  /// <summary>
  /// Creates an instance of type <typeparamref name="T"/> using a cached factory.
  /// </summary>
  /// <typeparam name="T">The type to instantiate (must have parameterless constructor)</typeparam>
  /// <returns>A new instance of type T</returns>
  /// <exception cref="InvalidOperationException">
  /// Thrown if type T does not have a parameterless constructor
  /// </exception>
  /// <remarks>
  /// <para>
  /// <strong>Compile-Time Safety:</strong> The `new()` constraint ensures that T has a
  /// parameterless constructor. This is enforced at compile-time by the C# compiler.
  /// </para>
  /// <para>
  /// <strong>Caching Strategy:</strong>
  /// - First call: Compiles an expression tree and caches the resulting delegate
  /// - Subsequent calls: Reuses the cached delegate
  /// - One cache entry per type T
  /// </para>
  /// </remarks>
  public static T Create<T>() where T : new() {
    var type = typeof(T);

    // Get or create factory for this type
    var factory = _factoryCache.GetOrAdd(type, CompileFactory<T>);

    // Invoke the cached factory
    return ((Func<T>)factory)();
  }

  /// <summary>
  /// Compiles a factory function for type T using expression trees.
  /// </summary>
  private static Func<T> CompileFactory<T>(Type type) where T : new() {
    // Create expression: () => new T()
    var newExpression = Expression.New(type);
    var lambda = Expression.Lambda<Func<T>>(newExpression);

    // Compile to delegate
    return lambda.Compile();
  }

  /// <summary>
  /// Clears the factory cache.
  /// </summary>
  /// <remarks>
  /// Useful for testing or memory management in long-running applications
  /// that dynamically load/unload types.
  /// </remarks>
  public static void ClearCache() {
    _factoryCache.Clear();
  }

  /// <summary>
  /// Gets the number of cached factory functions.
  /// </summary>
  public static int CacheCount => _factoryCache.Count;
}
