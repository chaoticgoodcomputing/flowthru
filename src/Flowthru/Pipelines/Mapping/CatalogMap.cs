using Flowthru.Data;
using Flowthru.Nodes.Factory;
using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;
using System.Reflection;

namespace Flowthru.Pipelines.Mapping;

/// <summary>
/// Unified mapping abstraction for both input and output directions.
/// Maps between catalog entries and schema type <typeparamref name="T"/>.
/// </summary>
/// <typeparam name="T">The schema type being mapped (must have parameterless constructor for mapped mode)</typeparam>
/// <remarks>
/// <para>
/// <strong>Bidirectional Design:</strong>
/// CatalogMap is intentionally designed to work in both directions:
/// - Input: LoadAsync() loads data from catalog entries and constructs T instances
/// - Output: SaveAsync() extracts properties from T instances and saves to catalog entries
/// </para>
/// <para>
/// The direction is determined by usage context in PipelineBuilder, not by distinct types.
/// This simplifies the API and maintains consistency between input and output patterns.
/// </para>
/// <para>
/// <strong>Two Modes of Operation:</strong>
/// 1. **Pass-Through Mode** (created via FromEntry): Directly wraps a single catalog entry
///    for simple single-input or single-output nodes. No schema construction needed.
/// 2. **Mapped Mode** (created via constructor): Maps multiple properties to catalog entries
///    for complex multi-input or multi-output nodes.
/// </para>
/// <para>
/// <strong>IMPORTANT - Validation Timing (Phase 1):</strong>
/// Property mapping completeness is validated at PIPELINE BUILD TIME (not compile-time).
/// This happens when PipelineBuilder.AddNode() is called, which invokes ValidateComplete().
/// 
/// This is a pragmatic trade-off:
/// - ✅ Type compatibility IS enforced at compile-time via generic constraints
/// - ✅ Validation occurs before any pipeline execution (fail-fast)
/// - ❌ Incomplete mappings are NOT caught until AddNode() is called
/// 
/// TODO: PHASE 2 UPGRADE - Roslyn Analyzer for Compile-Time Validation
/// A Roslyn source generator/analyzer could provide true compile-time validation by:
/// 1. Analyzing CatalogMap construction and Map() calls
/// 2. Comparing against properties on type T with [Required] attribute
/// 3. Emitting compiler diagnostics for missing mappings
/// 
/// Benefits of Phase 2 upgrade:
/// - Catches incomplete mappings at build time, not runtime
/// - Better developer experience (red squiggles in IDE)
/// - No possibility of deploying incomplete pipeline configurations
/// 
/// Trade-offs:
/// - Requires separate analyzer project and NuGet package
/// - More complex tooling infrastructure
/// - Increases initial implementation complexity
/// </para>
/// <para>
/// <strong>IMPORTANT - Singleton Load Behavior (Phase 1):</strong>
/// LoadAsync() always returns a SINGLETON enumerable (single T instance) for mapped schemas.
/// 
/// Rationale:
/// - Input schemas typically represent configuration/coordination data, not bulk data
/// - Bulk data flows through pass-through mode (FromEntry), not mapped mode
/// - Simpler reasoning: one input schema → one node execution → one output schema
/// - Predictable behavior avoids confusion about when multiple instances are created
/// 
/// Example:
/// <code>
/// // Pass-through for bulk data (many records)
/// pipeline.AddNode&lt;PreprocessNode&gt;(
///     catalog.Companies,  // ICatalogEntry&lt;IEnumerable&lt;CompanySchema&gt;&gt;
///     catalog.Processed,
///     "preprocess"
/// );
/// // Node receives: IEnumerable&lt;IEnumerable&lt;CompanySchema&gt;&gt; with full data
/// 
/// // Mapped schema for coordination (single instance)
/// var inputMap = new CatalogMap&lt;TrainModelInputs&gt;();
/// inputMap.Map(i => i.XTrain, catalog.XTrain);
/// inputMap.Map(i => i.YTrain, catalog.YTrain);
/// 
/// pipeline.AddNode&lt;TrainModelNode&gt;(inputMap, catalog.Model, "train");
/// // Node receives: IEnumerable&lt;TrainModelInputs&gt; with ONE instance containing all data
/// </code>
/// 
/// TODO: PHASE 2 UPGRADE - Explicit Multi-Instance Support
/// If use cases emerge that require multiple instances from a mapped schema, add:
/// <code>
/// public enum LoadBehavior { Singleton, MultipleFromFirstProperty }
/// public LoadBehavior Behavior { get; set; } = LoadBehavior.Singleton;
/// </code>
/// 
/// Benefits of Phase 2 upgrade:
/// - Explicit control over instance creation
/// - Clear intent in pipeline configuration
/// - Backward compatible (Singleton remains default)
/// 
/// Current recommendation: Wait for concrete use cases before adding complexity.
/// </para>
/// </remarks>
public class CatalogMap<T> where T : new()
{
  private readonly List<CatalogMapping> _mappings = new();
  private readonly bool _isPassThrough;
  private readonly ICatalogEntry? _passThroughEntry;

  /// <summary>
  /// Gets the property-to-catalog mappings for this map.
  /// Exposed for pipeline execution to correctly map inputs/outputs.
  /// </summary>
  internal IReadOnlyList<CatalogMapping> Mappings => _mappings.AsReadOnly();

  /// <summary>
  /// Creates a new mapped catalog map (for multi-input/output scenarios).
  /// </summary>
  public CatalogMap()
  {
    _isPassThrough = false;
  }

  /// <summary>
  /// Private constructor for pass-through mode (single catalog entry).
  /// </summary>
  private CatalogMap(ICatalogEntry entry)
  {
    _isPassThrough = true;
    _passThroughEntry = entry ?? throw new ArgumentNullException(nameof(entry));
  }

  /// <summary>
  /// Maps a catalog entry to a property on type T (bidirectional).
  /// </summary>
  /// <typeparam name="TProp">The type of the property being mapped</typeparam>
  /// <param name="propertySelector">Expression selecting the property to map</param>
  /// <param name="catalogEntry">The catalog entry to map to this property</param>
  /// <remarks>
  /// <para>
  /// <strong>Breaking Change (v0.2.0):</strong> Now accepts ICatalogEntry (base interface) instead of
  /// ICatalogEntry&lt;TProp&gt;, allowing both ICatalogDataset and ICatalogObject.
  /// </para>
  /// <para>
  /// <strong>Compile-Time Type Safety:</strong>
  /// The generic constraint ensures that TProp matches the property type exactly.
  /// Type mismatches are caught by the C# compiler, not at runtime.
  /// </para>
  /// <para>
  /// <strong>Expression Trees:</strong>
  /// Using Expression&lt;Func&lt;T, TProp&gt;&gt; instead of strings provides:
  /// - IntelliSense support when writing property selectors
  /// - Go To Definition navigation
  /// - Automatic rename refactoring
  /// - Compile-time validation of property existence
  /// </para>
  /// </remarks>
  public CatalogMap<T> Map<TProp>(
      Expression<Func<T, TProp>> propertySelector,
      ICatalogEntry catalogEntry)
  {
    if (_isPassThrough)
    {
      throw new InvalidOperationException(
          "Cannot add mappings to a pass-through CatalogMap. " +
          "Pass-through maps are created via FromEntry() for single catalog entries.");
    }

    var propertyInfo = ExtractPropertyInfo(propertySelector);
    ValidatePropertyType(propertyInfo, typeof(TProp));

    _mappings.Add(new CatalogPropertyMapping(propertyInfo, catalogEntry));
    return this;
  }

  /// <summary>
  /// Maps a constant parameter value to a property on type T (input only).
  /// </summary>
  /// <typeparam name="TProp">The type of the property being mapped</typeparam>
  /// <param name="propertySelector">Expression selecting the property to map</param>
  /// <param name="value">The constant value to assign to this property</param>
  /// <remarks>
  /// <para>
  /// <strong>Input-Only:</strong>
  /// Parameter mappings only make sense in the input direction. If a CatalogMap with
  /// parameter mappings is used in the output position, SaveAsync() will throw.
  /// </para>
  /// <para>
  /// <strong>Use Cases:</strong>
  /// - Configuration values (e.g., ModelOptions with TestSize, RandomState)
  /// - Algorithm parameters
  /// - Pipeline-level settings
  /// </para>
  /// </remarks>
  public CatalogMap<T> MapParameter<TProp>(
      Expression<Func<T, TProp>> propertySelector,
      TProp value)
  {
    if (_isPassThrough)
    {
      throw new InvalidOperationException(
          "Cannot add mappings to a pass-through CatalogMap.");
    }

    var propertyInfo = ExtractPropertyInfo(propertySelector);
    ValidatePropertyType(propertyInfo, typeof(TProp));

    _mappings.Add(new ParameterMapping(propertyInfo, value!));
    return this;
  }

  /// <summary>
  /// Factory method: Creates a pass-through catalog map for a single entry.
  /// </summary>
  /// <param name="entry">The catalog entry to wrap</param>
  /// <returns>A pass-through CatalogMap</returns>
  /// <remarks>
  /// Used by PipelineBuilder overloads 1 and 3 to automatically wrap single
  /// catalog entries for simple nodes.
  /// </remarks>
  public static CatalogMap<T> FromEntry(ICatalogEntry entry)
  {
    return new CatalogMap<T>(entry);
  }

  /// <summary>
  /// Validates that all required properties of type T are mapped.
  /// </summary>
  /// <exception cref="InvalidOperationException">
  /// Thrown if any property marked with [Required] is not mapped
  /// </exception>
  /// <remarks>
  /// <para>
  /// <strong>VALIDATION TIMING (Phase 1):</strong>
  /// This method is called at pipeline BUILD time by PipelineBuilder.AddNode().
  /// This ensures fail-fast behavior before any pipeline execution.
  /// </para>
  /// <para>
  /// <strong>Required Attribute:</strong>
  /// Properties are considered required if they have the [Required] attribute from
  /// System.ComponentModel.DataAnnotations.
  /// </para>
  /// <para>
  /// See class-level remarks for Phase 2 upgrade path (Roslyn analyzer).
  /// </para>
  /// </remarks>
  public void ValidateComplete()
  {
    if (_isPassThrough)
    {
      // Pass-through maps are always "complete" by definition
      return;
    }

    var requiredProperties = typeof(T).GetProperties()
        .Where(p => Attribute.IsDefined(p, typeof(RequiredAttribute)))
        .ToList();

    var mappedProperties = _mappings
        .Select(m => m.Property)
        .ToHashSet();

    var unmappedProperties = requiredProperties
        .Where(p => !mappedProperties.Contains(p))
        .ToList();

    if (unmappedProperties.Any())
    {
      var unmappedNames = string.Join(", ", unmappedProperties.Select(p => p.Name));
      throw new InvalidOperationException(
          $"CatalogMap<{typeof(T).Name}> is incomplete. " +
          $"The following required properties are not mapped: {unmappedNames}");
    }
  }

  /// <summary>
  /// Gets all catalog entries mapped in this CatalogMap.
  /// Used by PipelineBuilder to determine node dependencies.
  /// </summary>
  /// <returns>All mapped catalog entries (for dependency analysis)</returns>
  /// <remarks>
  /// For pass-through mode, returns the single wrapped catalog entry.
  /// For mapped mode, returns all catalog entries from CatalogPropertyMappings
  /// (excludes ParameterMappings since they don't represent dependencies).
  /// </remarks>
  internal IEnumerable<ICatalogEntry> GetMappedEntries()
  {
    if (_isPassThrough)
    {
      return new[] { _passThroughEntry! };
    }

    // Extract only catalog entries (not parameters)
    return _mappings
        .OfType<CatalogPropertyMapping>()
        .Select(m => m.CatalogEntry);
  }

  /// <summary>
  /// Loads data from catalog entries and constructs T instance(s).
  /// Used when CatalogMap is in the input position.
  /// </summary>
  /// <returns>
  /// For pass-through mode: The data loaded directly from the catalog entry.
  /// For mapped mode: A singleton enumerable containing one T instance with all properties populated.
  /// </returns>
  /// <remarks>
  /// See class-level remarks for detailed explanation of singleton behavior and Phase 2 upgrade path.
  /// </remarks>
  internal async Task<T> LoadAsync()
  {
    if (_isPassThrough)
    {
      // Pass-through: load directly from catalog entry (using untyped API)
      var data = await _passThroughEntry!.LoadUntyped();
      return (T)data;
    }
    else
    {
      // Mapped: load all catalog entries and construct single T instance
      var instance = await LoadMappedInstanceAsync();
      return instance;
    }
  }

  /// <summary>
  /// Saves data to catalog entries by extracting properties from T instances.
  /// Used when CatalogMap is in the output position.
  /// </summary>
  /// <param name="data">The data to save</param>
  /// <exception cref="InvalidOperationException">
  /// Thrown if this CatalogMap contains parameter mappings (parameters are input-only)
  /// </exception>
  internal async Task SaveAsync(T data)
  {
    if (_isPassThrough)
    {
      // Pass-through: save directly to catalog entry (using untyped API)
      await _passThroughEntry!.SaveUntyped(data!);
    }
    else
    {
      // Mapped: extract properties and save to catalog entries
      await SaveMappedInstanceAsync(data);
    }
  }

  /// <summary>
  /// Gets whether this is a pass-through catalog map.
  /// </summary>
  internal bool IsPassThrough => _isPassThrough;

  /// <summary>
  /// Gets the catalog entries referenced by this map (for dependency analysis).
  /// </summary>
  internal IEnumerable<ICatalogEntry> GetCatalogEntries()
  {
    if (_isPassThrough)
    {
      return new[] { _passThroughEntry! };
    }

    return _mappings
        .OfType<CatalogPropertyMapping>()
        .Select(m => m.CatalogEntry);
  }

  private async Task<T> LoadMappedInstanceAsync()
  {
    // Load all catalog entries in parallel
    var catalogMappings = _mappings.OfType<CatalogPropertyMapping>().ToList();
    var loadTasks = catalogMappings
        .Select(async m => new
        {
          m.Property,
          Data = await m.CatalogEntry.LoadUntyped()
        })
        .ToList();

    var loadedData = await Task.WhenAll(loadTasks);

    // Get parameter values
    var parameterMappings = _mappings.OfType<ParameterMapping>().ToList();

    // Construct T instance using TypeActivator
    var instance = TypeActivator.Create<T>();

    // Set properties from loaded data
    foreach (var item in loadedData)
    {
      item.Property.SetValue(instance, item.Data);
    }

    // Set properties from parameters
    foreach (var param in parameterMappings)
    {
      param.Property.SetValue(instance, param.Value);
    }

    // Return constructed instance (Phase 1 behavior - see class-level remarks)
    return instance;
  }

  private async Task SaveMappedInstanceAsync(T data)
  {
    // Check for parameter mappings (invalid in output direction)
    var parameterMappings = _mappings.OfType<ParameterMapping>().ToList();
    if (parameterMappings.Any())
    {
      throw new InvalidOperationException(
          "Cannot save using a CatalogMap that contains parameter mappings. " +
          "Parameter mappings are input-only.");
    }

    var catalogMappings = _mappings.OfType<CatalogPropertyMapping>().ToList();

    // Extract property values and save to catalog entries in parallel
    var saveTasks = catalogMappings
        .Select(async m =>
        {
          var propertyValue = m.Property.GetValue(data);
          if (propertyValue != null)
          {
            await m.CatalogEntry.SaveUntyped(propertyValue);
          }
        });

    await Task.WhenAll(saveTasks);
  }

  private PropertyInfo ExtractPropertyInfo<TProp>(Expression<Func<T, TProp>> selector)
  {
    if (selector.Body is MemberExpression memberExpression &&
        memberExpression.Member is PropertyInfo propertyInfo)
    {
      return propertyInfo;
    }

    throw new ArgumentException(
        "Expression must be a simple property selector (e.g., x => x.Property)",
        nameof(selector));
  }

  private void ValidatePropertyType(PropertyInfo property, Type expectedType)
  {
    if (property.PropertyType != expectedType)
    {
      throw new InvalidOperationException(
          $"Property '{property.Name}' has type {property.PropertyType.Name}, " +
          $"but mapping expects type {expectedType.Name}");
    }
  }
}
