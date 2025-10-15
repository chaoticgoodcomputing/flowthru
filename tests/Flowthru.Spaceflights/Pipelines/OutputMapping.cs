using System.Linq.Expressions;
using Flowthru.Data;

namespace Flowthru.Pipelines;

/// <summary>
/// Type-safe mapping from multi-output node properties to catalog entries.
/// 
/// <para><strong>Compile-Time Type Safety:</strong></para>
/// <para>
/// The generic Add&lt;TProp&gt; method ensures that the property type extracted
/// from the expression MUST match the catalog entry's type. This prevents:
/// - Assigning wrong-typed catalog entries to properties
/// - Runtime type mismatches in multi-output scenarios
/// - Refactoring errors (rename property → compiler finds all usages)
/// </para>
/// 
/// <para><strong>Example Usage:</strong></para>
/// <code>
/// var mapping = new OutputMapping&lt;SplitDataOutputs&gt;();
/// mapping.Add(s => s.XTrain, catalog.XTrain);  // ✅ Compiles: both are IEnumerable&lt;FeatureRow&gt;
/// mapping.Add(s => s.XTrain, catalog.Regressor);  // ❌ Compile error: type mismatch!
/// </code>
/// </summary>
/// <typeparam name="TOutput">The multi-output schema type</typeparam>
public class OutputMapping<TOutput>
{
  private readonly Dictionary<string, object> _mappings = new();

  /// <summary>
  /// Maps a property of TOutput to a typed catalog entry.
  /// 
  /// The generic constraint ensures TProp (property type) matches the catalog entry type,
  /// providing compile-time validation of the entire mapping.
  /// </summary>
  /// <typeparam name="TProp">The property type (inferred from expression)</typeparam>
  /// <param name="propertySelector">Expression selecting the property (e.g., s => s.XTrain)</param>
  /// <param name="catalogEntry">Catalog entry that must store data of type TProp</param>
  public void Add<TProp>(
    Expression<Func<TOutput, TProp>> propertySelector,
    ICatalogEntry<TProp> catalogEntry)
  {
    var propertyName = GetPropertyName(propertySelector);
    _mappings[propertyName] = catalogEntry;
  }

  /// <summary>
  /// Gets the catalog entry mapped to a specific property.
  /// </summary>
  public ICatalogEntry<TProp> Get<TProp>(Expression<Func<TOutput, TProp>> propertySelector)
  {
    var propertyName = GetPropertyName(propertySelector);
    if (!_mappings.TryGetValue(propertyName, out var entry))
    {
      throw new InvalidOperationException($"No catalog entry mapped for property '{propertyName}'");
    }
    return (ICatalogEntry<TProp>)entry;
  }

  /// <summary>
  /// Gets all mappings as property name → catalog entry pairs.
  /// </summary>
  public IReadOnlyDictionary<string, object> GetAllMappings() => _mappings;

  /// <summary>
  /// Extracts property name from a lambda expression.
  /// </summary>
  private static string GetPropertyName<TProp>(Expression<Func<TOutput, TProp>> propertySelector)
  {
    if (propertySelector.Body is not MemberExpression memberExpression)
    {
      throw new ArgumentException(
        "Property selector must be a simple member access expression (e.g., s => s.Property)",
        nameof(propertySelector));
    }

    return memberExpression.Member.Name;
  }
}
