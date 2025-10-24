using Flowthru.Data;
using Flowthru.Meta.Models;
using Flowthru.Pipelines;
using Flowthru.Pipelines.Mapping;

namespace Flowthru.Meta.Builders;

/// <summary>
/// Builds DAG metadata from a built pipeline.
/// </summary>
/// <remarks>
/// <para>
/// This builder traverses the pipeline's execution layers and catalog entries
/// to extract a complete structural representation of the DAG. The resulting
/// metadata can be serialized to JSON for visualization in Flowthru.Viz.
/// </para>
/// <para>
/// <strong>Prerequisites:</strong> Pipeline must be built (Pipeline.Build() called)
/// before this builder can extract metadata.
/// </para>
/// </remarks>
internal static class DagBuilder {
  /// <summary>
  /// Builds DAG metadata from a built pipeline.
  /// </summary>
  /// <param name="pipeline">The pipeline to extract metadata from (must be built)</param>
  /// <returns>Complete DAG metadata including nodes, catalog entries, and edges</returns>
  /// <exception cref="InvalidOperationException">Thrown if pipeline is not built</exception>
  public static DagMetadata Build(Pipeline pipeline) {
    if (!pipeline.IsBuilt) {
      throw new InvalidOperationException(
        "Cannot build DAG metadata from an unbuilt pipeline. Call Pipeline.Build() first.");
    }

    var dag = new DagMetadata {
      PipelineName = pipeline.Name ?? "UnnamedPipeline",
      GeneratedAt = DateTime.UtcNow
    };

    // Step 1: Extract all catalog entries from all nodes (inputs + outputs)
    var allCatalogEntries = ExtractCatalogEntries(pipeline);

    // Step 2: Build node metadata with layer information
    dag.Nodes.AddRange(BuildNodeMetadata(pipeline));

    // Step 3: Build catalog entry metadata with producer-consumer relationships
    dag.CatalogEntries.AddRange(BuildCatalogEntryMetadata(allCatalogEntries, dag.Nodes));

    // Step 4: Generate edges representing data flow
    dag.Edges.AddRange(BuildEdges(dag.Nodes, allCatalogEntries));

    return dag;
  }

  /// <summary>
  /// Extracts all unique catalog entries from the pipeline nodes.
  /// </summary>
  /// <remarks>
  /// Handles both simple catalog entries and CatalogMap entries by expanding
  /// maps into their constituent catalog entries.
  /// </remarks>
  private static Dictionary<string, ICatalogEntry> ExtractCatalogEntries(Pipeline pipeline) {
    var catalogEntries = new Dictionary<string, ICatalogEntry>();

    foreach (var node in pipeline.Nodes) {
      // Process inputs
      foreach (var input in node.Inputs) {
        AddCatalogEntry(catalogEntries, input);
      }

      // Process outputs
      foreach (var output in node.Outputs) {
        AddCatalogEntry(catalogEntries, output);
      }
    }

    return catalogEntries;
  }

  /// <summary>
  /// Adds a catalog entry to the dictionary, expanding CatalogMaps if necessary.
  /// </summary>
  private static void AddCatalogEntry(Dictionary<string, ICatalogEntry> catalogEntries, ICatalogEntry entry) {
    // Skip _nodata entries (placeholder entries that don't represent actual data)
    if (entry.Key.StartsWith("_nodata", StringComparison.OrdinalIgnoreCase)) {
      return;
    }

    // Check if this is a CatalogMap (which needs to be expanded into individual entries)
    var entryType = entry.GetType();
    if (entryType.IsGenericType && entryType.GetGenericTypeDefinition().Name == "CatalogMap`1") {
      // Use reflection to get the mapped entries from CatalogMap
      var getMappedEntriesMethod = entryType.GetMethod("GetMappedEntries",
        System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);

      if (getMappedEntriesMethod?.Invoke(entry, null) is IEnumerable<ICatalogEntry> mappedEntries) {
        foreach (var mappedEntry in mappedEntries) {
          // Skip _nodata entries in mapped entries too
          if (!mappedEntry.Key.StartsWith("_nodata", StringComparison.OrdinalIgnoreCase)) {
            catalogEntries.TryAdd(mappedEntry.Key, mappedEntry);
          }
        }
      }
    } else {
      // Simple catalog entry
      catalogEntries.TryAdd(entry.Key, entry);
    }
  }

  /// <summary>
  /// Builds metadata for all nodes in the pipeline.
  /// </summary>
  private static List<NodeMetadata> BuildNodeMetadata(Pipeline pipeline) {
    var nodes = new List<NodeMetadata>();

    foreach (var pipelineNode in pipeline.Nodes) {
      // Extract simple type name from node instance
      var nodeTypeName = pipelineNode.NodeInstance.GetType().Name;

      // Get input catalog keys (expanding CatalogMaps, filtering _nodata)
      var inputKeys = pipelineNode.Inputs
        .SelectMany(ExpandCatalogEntry)
        .Select(e => e.Key)
        .Where(key => !key.StartsWith("_nodata", StringComparison.OrdinalIgnoreCase))
        .ToList();

      // Get output catalog keys (expanding CatalogMaps, filtering _nodata)
      var outputKeys = pipelineNode.Outputs
        .SelectMany(ExpandCatalogEntry)
        .Select(e => e.Key)
        .Where(key => !key.StartsWith("_nodata", StringComparison.OrdinalIgnoreCase))
        .ToList();

      // Extract original pipeline name from node name if merged
      // Merged nodes have format: "PipelineName.NodeName"
      var originalPipelineName = ExtractOriginalPipelineName(pipelineNode.Name, pipeline.Name);

      nodes.Add(new NodeMetadata {
        Id = pipelineNode.Name,
        Label = FormatLabel(pipelineNode.Name),
        NodeType = nodeTypeName,
        Layer = pipelineNode.Layer,
        PipelineName = originalPipelineName,
        Inputs = inputKeys,
        Outputs = outputKeys
      });
    }

    return nodes;
  }

  /// <summary>
  /// Extracts the original pipeline name from a node name in a merged pipeline.
  /// </summary>
  /// <param name="nodeName">The node name (may be prefixed with pipeline name)</param>
  /// <param name="pipelineName">The current pipeline name</param>
  /// <returns>The original pipeline name if detected, otherwise the current pipeline name</returns>
  /// <remarks>
  /// In merged pipelines, node names are prefixed with their original pipeline name
  /// (e.g., "DataProcessing.PreprocessCompanies"). This method extracts that prefix.
  /// For non-merged pipelines, returns the current pipeline name as-is.
  /// </remarks>
  private static string ExtractOriginalPipelineName(string nodeName, string? pipelineName) {
    // Check if node name contains a dot (indicating it's from a merged pipeline)
    var dotIndex = nodeName.IndexOf('.');
    if (dotIndex > 0) {
      // Extract the prefix before the first dot as the original pipeline name
      return nodeName.Substring(0, dotIndex);
    }

    // No prefix found - use the current pipeline name
    return pipelineName ?? "UnnamedPipeline";
  }

  /// <summary>
  /// Expands a catalog entry, returning multiple entries if it's a CatalogMap.
  /// </summary>
  private static IEnumerable<ICatalogEntry> ExpandCatalogEntry(ICatalogEntry entry) {
    var entryType = entry.GetType();
    if (entryType.IsGenericType && entryType.GetGenericTypeDefinition().Name == "CatalogMap`1") {
      var getMappedEntriesMethod = entryType.GetMethod("GetMappedEntries",
        System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);

      if (getMappedEntriesMethod?.Invoke(entry, null) is IEnumerable<ICatalogEntry> mappedEntries) {
        return mappedEntries;
      }
    }

    return new[] { entry };
  }

  /// <summary>
  /// Builds metadata for all catalog entries with producer-consumer relationships.
  /// </summary>
  private static List<CatalogEntryMetadata> BuildCatalogEntryMetadata(
    Dictionary<string, ICatalogEntry> catalogEntries,
    List<NodeMetadata> nodes) {
    var entries = new List<CatalogEntryMetadata>();

    foreach (var (key, entry) in catalogEntries) {
      // Find producer (node that outputs this catalog entry)
      var producer = nodes.FirstOrDefault(n => n.Outputs.Contains(key));

      // Find consumers (nodes that input this catalog entry)
      var consumers = nodes
        .Where(n => n.Inputs.Contains(key))
        .Select(n => n.Id)
        .ToList();

      // Extract simple type name from DataType
      var dataTypeName = GetSimpleTypeName(entry.DataType);

      // Build fields dictionary with additional metadata
      var fields = BuildCatalogEntryFields(entry);

      // Generate schema (will be implemented in SchemaInference)
      var schema = SchemaInference.InferSchema(entry.DataType);

      entries.Add(new CatalogEntryMetadata {
        Key = key,
        Label = FormatLabel(key),
        DataType = dataTypeName,
        Schema = schema,
        Fields = fields,
        Producer = producer?.Id,
        Consumers = consumers
      });
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
  private static Dictionary<string, object> BuildCatalogEntryFields(ICatalogEntry entry) {
    var fields = new Dictionary<string, object>();
    var entryType = entry.GetType();

    // Add catalog type name
    fields["catalogType"] = GetSimpleTypeName(entryType);

    // Try to get filepath (for file-based datasets)
    var filePathProperty = entryType.GetProperty("FilePath");
    if (filePathProperty != null) {
      var filePath = filePathProperty.GetValue(entry);
      if (filePath != null) {
        fields["filepath"] = filePath;
      }
    }

    // Check if read-only (implements IReadableCatalogDataset but not ICatalogDataset)
    var isReadable = entryType.GetInterfaces()
      .Any(i => i.IsGenericType && i.GetGenericTypeDefinition().Name == "IReadableCatalogDataset`1");
    var isWritable = entryType.GetInterfaces()
      .Any(i => i.IsGenericType && i.GetGenericTypeDefinition().Name == "ICatalogDataset`1");

    if (isReadable && !isWritable) {
      fields["isReadOnly"] = true;
    }

    // Get inspection level if configured
    if (entry.PreferredInspectionLevel.HasValue) {
      fields["inspectionLevel"] = entry.PreferredInspectionLevel.Value.ToString();
    }

    return fields;
  }

  /// <summary>
  /// Builds edges representing data flow in the DAG.
  /// </summary>
  /// <remarks>
  /// Creates two types of edges:
  /// 1. Catalog Entry → Node (node reads from catalog)
  /// 2. Node → Catalog Entry (node writes to catalog)
  /// </remarks>
  private static List<EdgeMetadata> BuildEdges(
    List<NodeMetadata> nodes,
    Dictionary<string, ICatalogEntry> catalogEntries) {
    var edges = new List<EdgeMetadata>();

    foreach (var node in nodes) {
      // Create edges for inputs (Catalog → Node)
      foreach (var inputKey in node.Inputs) {
        if (catalogEntries.TryGetValue(inputKey, out var catalogEntry)) {
          var dataTypeName = GetSimpleTypeName(catalogEntry.DataType);

          edges.Add(new EdgeMetadata {
            Source = inputKey,
            Target = node.Id,
            DataType = dataTypeName
          });
        }
      }

      // Create edges for outputs (Node → Catalog)
      foreach (var outputKey in node.Outputs) {
        if (catalogEntries.TryGetValue(outputKey, out var catalogEntry)) {
          var dataTypeName = GetSimpleTypeName(catalogEntry.DataType);

          edges.Add(new EdgeMetadata {
            Source = node.Id,
            Target = outputKey,
            DataType = dataTypeName
          });
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
  /// - "Flowthru.Tests.KedroSpaceflights.Data.Schemas.Company" → "Company"
  /// - "System.Collections.Generic.List`1[System.String]" → "List"
  /// - "CsvCatalogDataset`1" → "CsvCatalogDataset"
  /// </remarks>
  private static string GetSimpleTypeName(Type type) {
    var name = type.Name;

    // Remove generic arity indicator (e.g., "List`1" → "List")
    var backtickIndex = name.IndexOf('`');
    if (backtickIndex >= 0) {
      name = name.Substring(0, backtickIndex);
    }

    return name;
  }

  /// <summary>
  /// Formats an identifier into a human-readable label.
  /// </summary>
  /// <remarks>
  /// Examples:
  /// - "PreprocessCompanies" → "Preprocess Companies"
  /// - "XTrain" → "X Train"
  /// - "ModelInputTable" → "Model Input Table"
  /// </remarks>
  private static string FormatLabel(string identifier) {
    if (string.IsNullOrEmpty(identifier)) {
      return identifier;
    }

    // Insert spaces before capital letters (except the first character)
    var formatted = System.Text.RegularExpressions.Regex.Replace(
      identifier,
      "(\\B[A-Z])",
      " $1"
    );

    return formatted;
  }
}
