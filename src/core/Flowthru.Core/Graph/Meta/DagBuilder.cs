using Flowthru.Core.Data;
using Flowthru.Core.Flows;
using Flowthru.Core.Graph;
using Flowthru.Core.Graph.Meta.Models;

namespace Flowthru.Core.Graph.Meta;

/// <summary>
/// Builds DAG metadata from a built pipeline.
/// </summary>
/// <remarks>
/// <para>
/// This builder traverses the pipeline's execution layers and catalog entries
/// to extract a complete structural representation of the DAG. The resulting
/// metadata can be serialized to JSON for visualization in Flowthru.Core.Viz.
/// </para>
/// <para>
/// <strong>Prerequisites:</strong> Pipeline must be built (Pipeline.Build() called)
/// before this builder can extract metadata.
/// </para>
/// </remarks>
internal static class DagBuilder
{
  /// <summary>
  /// Builds DAG metadata from a built pipeline.
  /// </summary>
  /// <param name="pipeline">The pipeline to extract metadata from (must be built)</param>
  /// <returns>Complete DAG metadata including nodes, catalog entries, and edges</returns>
  /// <exception cref="InvalidOperationException">Thrown if pipeline is not built</exception>
  public static DagMetadata Build(Flow pipeline)
  {
    if (!pipeline.IsBuilt)
    {
      throw new InvalidOperationException(
        "Cannot build DAG metadata from an unbuilt pipeline. Call Pipeline.Build() first."
      );
    }

    // Always build from the full DAG to provide complete context
    var allSteps = pipeline.StepsList;
    var slicedSteps = pipeline.GetSlicedSteps();

    var dag = new DagMetadata
    {
      FlowName = pipeline.Name ?? "UnnamedPipeline",
      GeneratedAt = DateTime.UtcNow,
      AppliedSlice =
        pipeline.AppliedSlice != null ? DagSliceMetadata.FromStrategy(pipeline.AppliedSlice) : null,
      SlicedStepIds = slicedSteps != null ? slicedSteps.Select(n => n.Label).ToHashSet() : null,
      SlicedCatalogItemIds =
        slicedSteps != null
          ? slicedSteps
            .SelectMany(n => n.Inputs.Concat(n.Outputs))
            .SelectMany(ExpandCatalogItem)
            .Where(e => !e.Label.StartsWith("_nodata", StringComparison.OrdinalIgnoreCase))
            .Select(e => GetQualifiedLabel(e))
            .ToHashSet()
          : null,
    };

    // Step 1: Extract all catalog entries from full DAG
    var allCatalogItems = ExtractCatalogItems(allSteps);

    // Step 2: Build node metadata with layer information
    dag.Steps.AddRange(BuildStepMetadata(allSteps));

    // Step 3: Build catalog entry metadata with producer-consumer relationships
    dag.CatalogItems.AddRange(BuildCatalogItemMetadata(allCatalogItems, dag.Steps));

    // Step 4: Generate edges representing data flow
    dag.Edges.AddRange(BuildEdges(dag.Steps, allCatalogItems));

    return dag;
  }

  /// <summary>
  /// Extracts all unique catalog items from the Flow steps.
  /// </summary>
  /// <remarks>
  /// Handles both simple catalog items and CatalogMap items by expanding
  /// maps into their constituent parts.
  /// </remarks>
  private static Dictionary<string, INode> ExtractCatalogItems(List<FlowStep> steps)
  {
    var catalogItems = new Dictionary<string, INode>();

    foreach (var step in steps)
    {
      // Process inputs
      foreach (var input in step.Inputs)
      {
        AddCatalogItem(catalogItems, input);
      }

      // Process outputs
      foreach (var output in step.Outputs)
      {
        AddCatalogItem(catalogItems, output);
      }
    }

    return catalogItems;
  }

  /// <summary>
  /// Adds a catalog item to the dictionary, expanding CatalogMaps if necessary.
  /// </summary>
  private static void AddCatalogItem(Dictionary<string, INode> catalogItems, INode item)
  {
    // Skip _nodata items (placeholder items that don't represent actual data)
    if (item.Label.StartsWith("_nodata", StringComparison.OrdinalIgnoreCase))
    {
      return;
    }

    // Check if this is a CatalogMap (which needs to be expanded into individual items)
    var itemType = item.GetType();
    if (itemType.IsGenericType && itemType.GetGenericTypeDefinition().Name == "CatalogMap`1")
    {
      // Use reflection to get the mapped items from CatalogMap
      var getMappedItemsMethod = itemType.GetMethod(
        "GetMappedItems",
        System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic
      );

      if (getMappedItemsMethod?.Invoke(item, null) is IEnumerable<IItem> mappedItems)
      {
        foreach (var mappedItem in mappedItems)
        {
          // Skip _nodata in mapped items too
          if (!mappedItem.Label.StartsWith("_nodata", StringComparison.OrdinalIgnoreCase))
          {
            catalogItems.TryAdd(GetQualifiedLabel(mappedItem), mappedItem);
          }
        }
      }
    }
    else
    {
      // Simple catalog item
      catalogItems.TryAdd(GetQualifiedLabel(item), item);
    }
  }

  /// <summary>
  /// Lazy-loaded test count map from FUnit's <c>StepTestRegistry</c>.
  /// Populated on first call if <c>Flowthru.FUnit</c> is loaded; null otherwise.
  /// </summary>
  private static IReadOnlyDictionary<Type, int>? _stepTestRegistry;
  private static bool _stepTestRegistryResolved;

  private static IReadOnlyDictionary<Type, int>? TryGetStepTestRegistry()
  {
    if (_stepTestRegistryResolved)
    {
      return _stepTestRegistry;
    }

    _stepTestRegistryResolved = true;

    // StepTestRegistry is an internal type emitted by Flowthru.FUnit.SourceGenerators
    // into the consuming test assembly. Walk all loaded assemblies to find it.
    foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
    {
      var registryType = assembly.GetType("StepTestRegistry");
      if (registryType is null)
      {
        continue;
      }

      var prop = registryType.GetProperty(
        "TestCounts",
        System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static
      );

      if (prop?.GetValue(null) is IReadOnlyDictionary<Type, int> counts)
      {
        _stepTestRegistry = counts;
        break;
      }
    }

    return _stepTestRegistry;
  }

  /// <summary>
  /// Builds metadata for all steps in the flow.
  /// </summary>
  private static List<StepMetadata> BuildStepMetadata(List<FlowStep> steps)
  {
    var stepMetadataList = new List<StepMetadata>();
    var testCounts = TryGetStepTestRegistry();

    foreach (var flowStep in steps)
    {
      // Use step name directly (no longer extracting from instance type)
      var stepLabel = flowStep.Label;

      // Get input catalog keys (expanding CatalogMaps, filtering _nodata)
      var inputKeys = flowStep
        .Inputs.SelectMany(ExpandCatalogItem)
        .Where(e => !e.Label.StartsWith("_nodata", StringComparison.OrdinalIgnoreCase))
        .Select(e => GetQualifiedLabel(e))
        .ToList();

      // Get output catalog keys (expanding CatalogMaps, filtering _nodata)
      var outputKeys = flowStep
        .Outputs.SelectMany(ExpandCatalogItem)
        .Where(e => !e.Label.StartsWith("_nodata", StringComparison.OrdinalIgnoreCase))
        .Select(e => GetQualifiedLabel(e))
        .ToList();

      // Extract original flow name from Flow label if merged
      // Merged nodes have format: "FlowName.StepName"
      var originalFlowName = ExtractOriginalFlowName(flowStep.Label);

      // Resolve test count from FUnit StepTestRegistry (null when FUnit absent)
      int? testCount = null;
      if (
        testCounts is not null
        && flowStep.TransformFunction?.Method?.DeclaringType is Type stepType
      )
      {
        testCounts.TryGetValue(stepType, out var count);
        testCount = count;
      }

      stepMetadataList.Add(
        new StepMetadata
        {
          Id = flowStep.Label,
          Label = flowStep.Label,
          StepType = stepLabel,
          Layer = flowStep.Layer,
          FlowName = originalFlowName ?? "UnnamedFlow",
          Inputs = inputKeys,
          Outputs = outputKeys,
          TestCount = testCount,
        }
      );
    }

    return stepMetadataList;
  }

  /// <summary>
  /// Extracts the original Flow name from a step name in a merged pipeline.
  /// </summary>
  /// <param name="stepName">The step name (may be prefixed with Flow name)</param>
  /// <returns>The original Flow name if detected, otherwise null</returns>
  /// <remarks>
  /// In merged flows, step names are prefixed with their original subflow name
  /// (e.g., "DataProcessing.PreprocessCompanies"). This method extracts that prefix.
  /// For non-merged flows, returns null.
  /// </remarks>
  private static string? ExtractOriginalFlowName(string stepName)
  {
    // Check if step name contains a dot (indicating it's from a merged flow)
    var dotIndex = stepName.IndexOf('.');
    if (dotIndex > 0)
    {
      // Extract the prefix before the first dot as the original Flow name
      return stepName.Substring(0, dotIndex);
    }

    // No prefix found - use the current Flow name
    return "UnnamedFlow";
  }

  /// <summary>
  /// Expands a catalog item, returning multiple items if it's a CatalogMap.
  /// </summary>
  private static IEnumerable<INode> ExpandCatalogItem(INode item)
  {
    var itemType = item.GetType();
    if (itemType.IsGenericType && itemType.GetGenericTypeDefinition().Name == "CatalogMap`1")
    {
      var getMappedItemsMethod = itemType.GetMethod(
        "GetMappedItems",
        System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic
      );

      if (getMappedItemsMethod?.Invoke(item, null) is IEnumerable<IItem> mappedItems)
      {
        return mappedItems.Cast<INode>();
      }
    }

    return new[] { item };
  }

  /// <summary>
  /// Builds metadata for all catalog items with producer-consumer relationships.
  /// </summary>
  private static List<ItemMetadata> BuildCatalogItemMetadata(
    Dictionary<string, INode> catalogItems,
    List<StepMetadata> steps
  )
  {
    var entries = new List<ItemMetadata>();

    foreach (var (key, entry) in catalogItems)
    {
      // Find producer (node that outputs this catalog entry)
      var producer = steps.FirstOrDefault(n => n.Outputs.Contains(key));

      // Find consumers (nodes that input this catalog entry)
      var consumers = steps.Where(n => n.Inputs.Contains(key)).Select(n => n.Id).ToList();

      // Extract simple type name from DataType
      var dataTypeName = GetSimpleTypeName(entry.DataType);

      // Build fields dictionary with additional metadata
      var fields = BuildCatalogItemFields(entry);

      // Generate schema (will be implemented in SchemaInference)
      var schema = SchemaInference.InferSchema(entry.DataType);

      entries.Add(
        new ItemMetadata
        {
          Key = key,
          Label = key,
          DataType = dataTypeName,
          Schema = schema,
          Fields = fields,
          Producer = producer?.Id,
          Consumers = consumers,
        }
      );
    }

    return entries;
  }

  /// <summary>
  /// Builds the fields dictionary for a catalog entry.
  /// </summary>
  /// <remarks>
  /// Extracts metadata like filepath, catalog type, read-only status, etc.
  /// using reflection to check for well-known properties.
  /// </remarks>
  private static Dictionary<string, object> BuildCatalogItemFields(INode node)
  {
    var fields = new Dictionary<string, object>();
    var entryType = node.GetType();

    // Add catalog type name
    fields["catalogType"] = GetSimpleTypeName(entryType);

    // Try to get filepath (for file-based datasets)
    var filePathProperty = entryType.GetProperty("FilePath");
    if (filePathProperty != null)
    {
      var filePath = filePathProperty.GetValue(node);
      if (filePath != null)
      {
        fields["filepath"] = filePath;
      }
    }

    // Check if read-only using StorageTraits
    var adapter = node.GetType().GetProperty("Adapter")?.GetValue(node);
    if (adapter != null)
    {
      var traitsProperty = adapter.GetType().GetProperty("Traits");
      if (traitsProperty != null)
      {
        var traits = traitsProperty.GetValue(adapter);
        if (traits is Data.Capabilities.StorageTraits storageTraits && !storageTraits.CanWrite)
        {
          fields["isReadOnly"] = true;
        }
      }
    }

    // Get inspection level if configured (IItem-specific)
    if (node is IItem item && item.PreferredInspectionLevel.HasValue)
    {
      fields["inspectionLevel"] = item.PreferredInspectionLevel.Value.ToString();
    }

    return fields;
  }

  /// <summary>
  /// Builds edges representing data Flow in the DAG.
  /// </summary>
  /// <remarks>
  /// Creates two types of edges:
  /// 1. Catalog Item → Step (node reads from catalog)
  /// 2. Step → Catalog Item (node writes to catalog)
  /// </remarks>
  private static List<EdgeMetadata> BuildEdges(
    List<StepMetadata> steps,
    Dictionary<string, INode> catalogItems
  )
  {
    var edges = new List<EdgeMetadata>();

    foreach (var step in steps)
    {
      // Create edges for inputs (Catalog Item → Step)
      foreach (var inputKey in step.Inputs)
      {
        if (catalogItems.TryGetValue(inputKey, out var catalogEntry))
        {
          var dataTypeName = GetSimpleTypeName(catalogEntry.DataType);

          edges.Add(
            new EdgeMetadata
            {
              Source = inputKey,
              Target = step.Id,
              DataType = dataTypeName,
            }
          );
        }
      }

      // Create edges for outputs (Step → Catalog)
      foreach (var outputKey in step.Outputs)
      {
        if (catalogItems.TryGetValue(outputKey, out var catalogEntry))
        {
          var dataTypeName = GetSimpleTypeName(catalogEntry.DataType);

          edges.Add(
            new EdgeMetadata
            {
              Source = step.Id,
              Target = outputKey,
              DataType = dataTypeName,
            }
          );
        }
      }
    }

    return edges;
  }

  /// <summary>
  /// Extracts simple type name without namespace or generic parameters.
  /// </summary>
  /// <remarks>
  /// Examples:
  /// - "KedroSpaceflights.Custom.Data.Schemas.Company" → "Company"
  /// - "System.Collections.Generic.List`1[System.String]" → "List"
  /// - "CsvCatalogDataset`1" → "CsvCatalogDataset"
  /// </remarks>
  private static string GetSimpleTypeName(Type type)
  {
    var name = type.Name;

    // Remove generic arity indicator (e.g., "List`1" → "List")
    var backtickIndex = name.IndexOf('`');
    if (backtickIndex >= 0)
    {
      name = name.Substring(0, backtickIndex);
    }

    return name;
  }

  /// <summary>
  /// Returns the fully-qualified metadata label for a catalog item.
  /// When the item was created via <c>DataCatalogBase.CreateItem</c> the catalog's
  /// <c>CatalogLabel</c> is prepended: <c>"CatalogName.ItemLabel"</c>.
  /// Falls back to <c>item.Label</c> for items created outside a catalog.
  /// </summary>
  private static string GetQualifiedLabel(INode node) =>
    node is IItem item && item.OwningCatalogLabel is { } catalog
      ? $"{catalog}.{node.Label}"
      : node.Label;
}
