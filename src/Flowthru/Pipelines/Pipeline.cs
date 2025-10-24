using System.Diagnostics;
using Flowthru.Data;
using Microsoft.Extensions.Logging;

namespace Flowthru.Pipelines;

/// <summary>
/// Represents a complete data pipeline with nodes, dependencies, and execution order.
/// </summary>
/// <remarks>
/// <para>
/// A pipeline is a directed acyclic graph (DAG) of transformation nodes.
/// Each node reads data from catalog entries, performs transformations,
/// and writes results back to catalog entries.
/// </para>
/// <para>
/// <strong>Execution Model:</strong>
/// </para>
/// <list type="bullet">
/// <item>Nodes are organized into layers via topological sort</item>
/// <item>Nodes in layer 0 have no dependencies (read external data only)</item>
/// <item>Nodes in layer N depend only on nodes in layers 0..N-1</item>
/// <item>Sequential execution: Execute all nodes in layer order</item>
/// <item>Parallel execution (Phase 2): Execute nodes within same layer concurrently</item>
/// </list>
/// <para>
/// <strong>Single Producer Rule:</strong> Each catalog entry can be written by at most
/// one node. This ensures deterministic execution order and prevents race conditions.
/// </para>
/// </remarks>
public class Pipeline {
  /// <summary>
  /// All nodes in this pipeline, in the order they were added.
  /// </summary>
  internal List<PipelineNode> Nodes { get; } = new();

  /// <summary>
  /// Nodes grouped by execution layer.
  /// Populated after Build() is called.
  /// </summary>
  internal IReadOnlyList<List<PipelineNode>>? ExecutionLayers { get; private set; }

  /// <summary>
  /// Optional logger for pipeline execution.
  /// </summary>
  public ILogger? Logger { get; set; }

  /// <summary>
  /// Optional service provider for dependency injection into nodes.
  /// </summary>
  /// <remarks>
  /// Set by FlowthruApplication before pipeline execution to enable nodes
  /// to resolve services (e.g., database connections, external APIs).
  /// </remarks>
  public IServiceProvider? ServiceProvider { get; set; }

  /// <summary>
  /// Pipeline name for identification and logging.
  /// </summary>
  /// <remarks>
  /// Set by PipelineRegistry during pipeline registration.
  /// </remarks>
  public string? Name { get; internal set; }

  /// <summary>
  /// Optional description of what this pipeline does.
  /// </summary>
  public string? Description { get; internal set; }

  /// <summary>
  /// Tags for categorizing and filtering pipelines.
  /// </summary>
  public IReadOnlyList<string> Tags { get; internal set; } = Array.Empty<string>();

  /// <summary>
  /// Validation options for this pipeline.
  /// </summary>
  /// <remarks>
  /// Configures how external data sources (Layer 0 inputs) are validated
  /// before pipeline execution begins.
  /// </remarks>
  public Validation.ValidationOptions ValidationOptions { get; internal set; } = Validation.ValidationOptions.Default();

  /// <summary>
  /// Indicates whether the pipeline has been built (dependencies analyzed and layers assigned).
  /// </summary>
  public bool IsBuilt => ExecutionLayers != null;

  /// <summary>
  /// Adds a node to the pipeline.
  /// </summary>
  /// <param name="node">The pipeline node to add</param>
  /// <exception cref="InvalidOperationException">Thrown if pipeline has already been built</exception>
  internal void AddNode(PipelineNode node) {
    if (IsBuilt) {
      throw new InvalidOperationException(
        "Cannot add nodes to a pipeline that has already been built. " +
        "Create a new pipeline or use PipelineBuilder.");
    }

    Nodes.Add(node);
  }

  /// <summary>
  /// Merges multiple pipelines into a single pipeline by combining all their nodes.
  /// </summary>
  /// <param name="pipelines">Dictionary of pipeline names to pipeline instances</param>
  /// <returns>A new pipeline containing all nodes from all input pipelines</returns>
  /// <remarks>
  /// <para>
  /// This method creates a new pipeline by combining all nodes from the input pipelines.
  /// Node names are prefixed with their source pipeline name (e.g., "data_processing.PreprocessCompanies")
  /// to ensure uniqueness and maintain traceability in logs.
  /// </para>
  /// <para>
  /// The existing DependencyAnalyzer will automatically resolve cross-pipeline dependencies
  /// based on catalog entries. The single producer rule is enforced - if multiple pipelines
  /// attempt to write to the same catalog entry, Build() will throw an InvalidOperationException.
  /// </para>
  /// </remarks>
  public static Pipeline Merge(Dictionary<string, Pipeline> pipelines) {
    var mergedPipeline = new Pipeline {
      Name = "Pipelines",
      Description = $"Combined execution of: {string.Join(", ", pipelines.Keys)}"
    };

    // Combine all nodes from all pipelines, prefixing node names with pipeline name
    foreach (var (pipelineName, pipeline) in pipelines) {
      foreach (var node in pipeline.Nodes) {
        // Create a new node with prefixed name
        var prefixedNode = new PipelineNode(
          name: $"{pipelineName}.{node.Name}",
          nodeInstance: node.NodeInstance,
          inputs: node.Inputs,
          outputs: node.Outputs,
          inputMappings: node.InputMappings,
          outputMappings: node.OutputMappings
        );

        mergedPipeline.AddNode(prefixedNode);
      }
    }

    return mergedPipeline;
  }

  /// <summary>
  /// Builds the pipeline by analyzing dependencies and assigning execution layers.
  /// Must be called before executing the pipeline.
  /// </summary>
  /// <exception cref="InvalidOperationException">
  /// Thrown if:
  /// - Multiple nodes write to the same catalog entry
  /// - A circular dependency is detected
  /// </exception>
  public void Build() {
    if (IsBuilt) {
      Logger?.LogWarning("Pipeline.Build() called on already-built pipeline. Rebuilding...");
    }

    Logger?.LogInformation("Building pipeline with {NodeCount} nodes", Nodes.Count);

    // Analyze dependencies and assign layers
    DependencyAnalyzer.AnalyzeAndAssignLayers(Nodes);

    // Group nodes by layer
    ExecutionLayers = DependencyAnalyzer.GroupByLayer(Nodes).ToList();

    Logger?.LogInformation(
      "Pipeline built successfully. Execution will proceed in {LayerCount} layers",
      ExecutionLayers.Count);

    // Log layer details
    for (int i = 0; i < ExecutionLayers.Count; i++) {
      var layerNodes = ExecutionLayers[i];
      Logger?.LogDebug(
        "Layer {LayerIndex}: {NodeCount} nodes ({NodeNames})",
        i,
        layerNodes.Count,
        string.Join(", ", layerNodes.Select(n => n.Name)));
    }
  }

  /// <summary>
  /// Validates all external inputs (Layer 0) before pipeline execution.
  /// </summary>
  /// <returns>ValidationResult containing any errors found</returns>
  /// <exception cref="InvalidOperationException">Thrown if pipeline has not been built</exception>
  /// <remarks>
  /// <para>
  /// This method inspects catalog entries that serve as inputs to Layer 0 nodes.
  /// These are pre-existing external data sources (files, databases, APIs) that
  /// must exist and be valid before the pipeline can execute.
  /// </para>
  /// <para>
  /// <strong>Inspection Levels:</strong>
  /// </para>
  /// <list type="bullet">
  /// <item><strong>None:</strong> Skip inspection entirely</item>
  /// <item><strong>Shallow:</strong> Validate file exists, check headers/schema, deserialize sample rows</item>
  /// <item><strong>Deep:</strong> Validate all rows in the dataset (expensive!)</item>
  /// </list>
  /// <para>
  /// <strong>Default Behavior:</strong>
  /// </para>
  /// <list type="bullet">
  /// <item>If explicitly configured via WithValidation() → use that level</item>
  /// <item>If entry implements IShallowInspectable → Shallow</item>
  /// <item>Otherwise → None (skip)</item>
  /// </list>
  /// <para>
  /// <strong>Important:</strong> Only Layer 0 inputs are inspected. Intermediate pipeline
  /// outputs (Layer 1+) are never inspected, as they don't exist yet.
  /// </para>
  /// <para>
  /// <strong>Usage:</strong>
  /// </para>
  /// <code>
  /// pipeline.Build();
  /// var validationResult = await pipeline.ValidateExternalInputsAsync();
  /// if (!validationResult.IsValid) {
  ///   // Handle validation errors before execution
  ///   validationResult.ThrowIfInvalid();
  /// }
  /// await pipeline.RunAsync();
  /// </code>
  /// </remarks>
  public async Task<Data.Validation.ValidationResult> ValidateExternalInputsAsync() {
    if (!IsBuilt) {
      throw new InvalidOperationException(
        "Pipeline must be built before validation. Call Build() first.");
    }

    var result = Data.Validation.ValidationResult.Success();

    // No Layer 0? No external inputs to validate
    if (ExecutionLayers!.Count == 0) {
      Logger?.LogInformation("No nodes in pipeline, nothing to validate");
      return result;
    }

    var layer0Nodes = ExecutionLayers[0];
    Logger?.LogInformation("Validating external inputs from {Layer0NodeCount} Layer 0 nodes", layer0Nodes.Count);

    // Extract all unique input catalog entries from Layer 0 nodes
    // Handle CatalogMap entries by expanding them into their constituent catalog entries
    var externalInputs = layer0Nodes
      .SelectMany(node => node.Inputs)
      .SelectMany(entry => {
        // If this is a CatalogMap, expand it into its catalog entries
        if (entry is Mapping.CatalogMap<object> catalogMap) {
          return catalogMap.GetCatalogEntriesForInspection();
        }
        // Check for generic CatalogMap via reflection (since we don't know T at compile-time)
        var entryType = entry.GetType();
        if (entryType.IsGenericType && entryType.GetGenericTypeDefinition().Name == "CatalogMap`1") {
          var method = entryType.GetMethod("GetCatalogEntriesForInspection",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
          if (method != null) {
            if (method.Invoke(entry, null) is IEnumerable<ICatalogEntry> entries) {
              return entries;
            }
          }
        }
        // Otherwise, return the entry itself
        return new[] { entry };
      })
      .DistinctBy(entry => entry.Key)
      .ToList();

    Logger?.LogInformation("Found {ExternalInputCount} unique external input(s) to validate", externalInputs.Count);

    // Inspect each external input based on configured or default level
    foreach (var catalogEntry in externalInputs) {
      var inspectionLevel = ValidationOptions.GetEffectiveInspectionLevel(catalogEntry);

      if (inspectionLevel == Data.Validation.InspectionLevel.None) {
        Logger?.LogDebug("Skipping inspection for '{CatalogKey}' (level: None)", catalogEntry.Key);
        continue;
      }

      Logger?.LogInformation(
        "Inspecting '{CatalogKey}' with {InspectionLevel} inspection",
        catalogEntry.Key,
        inspectionLevel);

      try {
        Data.Validation.ValidationResult inspectionResult;

        if (inspectionLevel == Data.Validation.InspectionLevel.Shallow) {
          // Try shallow inspection
          var shallowInterface = catalogEntry.GetType()
            .GetInterfaces()
            .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IShallowInspectable<>));

          if (shallowInterface != null) {
            inspectionResult = await InvokeShallowInspectionAsync(catalogEntry, shallowInterface);
          } else {
            // Entry doesn't support shallow inspection but was configured for it
            inspectionResult = Data.Validation.ValidationResult.Failure(
              catalogEntry.Key,
              Data.Validation.ValidationErrorType.InspectionFailure,
              "Entry does not implement IShallowInspectable<T>");
          }
        } else // Deep
          {
          // Try deep inspection
          var deepInterface = catalogEntry.GetType()
            .GetInterfaces()
            .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IDeepInspectable<>));

          if (deepInterface != null) {
            inspectionResult = await InvokeDeepInspectionAsync(catalogEntry, deepInterface);
          } else {
            // Entry doesn't support deep inspection but was configured for it
            inspectionResult = Data.Validation.ValidationResult.Failure(
              catalogEntry.Key,
              Data.Validation.ValidationErrorType.InspectionFailure,
              "Entry does not implement IDeepInspectable<T>");
          }
        }

        result.Merge(inspectionResult);

        if (!inspectionResult.IsValid) {
          Logger?.LogWarning(
            "Validation failed for '{CatalogKey}': {ErrorCount} error(s)",
            catalogEntry.Key,
            inspectionResult.Errors.Count);
        } else {
          Logger?.LogInformation("'{CatalogKey}' passed {InspectionLevel} inspection", catalogEntry.Key, inspectionLevel);
        }
      } catch (Exception ex) {
        Logger?.LogError(ex, "Exception during inspection of '{CatalogKey}'", catalogEntry.Key);
        result.AddError(new Data.Validation.ValidationError(
          catalogEntry.Key,
          Data.Validation.ValidationErrorType.InspectionFailure,
          $"Inspection threw exception: {ex.Message}",
          ex.ToString()));
      }
    }

    if (result.IsValid) {
      Logger?.LogInformation("All external inputs passed validation");
    } else {
      Logger?.LogError(
        "Validation failed with {ErrorCount} error(s) across {CatalogCount} catalog entries",
        result.Errors.Count,
        result.Errors.Select(e => e.CatalogKey).Distinct().Count());
    }

    return result;
  }

  /// <summary>
  /// Invokes shallow inspection on a catalog entry using reflection to handle generic types.
  /// </summary>
  private async Task<Data.Validation.ValidationResult> InvokeShallowInspectionAsync(
    ICatalogEntry catalogEntry,
    Type shallowInterface) {
    var method = shallowInterface.GetMethod(nameof(IShallowInspectable<object>.InspectShallow));
    var task = (Task<Data.Validation.ValidationResult>)method!.Invoke(catalogEntry, new object[] { 10 })!;
    return await task;
  }

  /// <summary>
  /// Invokes deep inspection on a catalog entry using reflection to handle generic types.
  /// </summary>
  private async Task<Data.Validation.ValidationResult> InvokeDeepInspectionAsync(
    ICatalogEntry catalogEntry,
    Type deepInterface) {
    var method = deepInterface.GetMethod(nameof(IDeepInspectable<object>.InspectDeep));
    var task = (Task<Data.Validation.ValidationResult>)method!.Invoke(catalogEntry, Array.Empty<object>())!;
    return await task;
  }

  /// <summary>
  /// /// Builds and executes the pipeline, returning comprehensive execution results.
  /// </summary>
  /// <returns>PipelineResult containing execution status, timing, and node results</returns>
  /// <remarks>
  /// <para>
  /// This is the primary high-level API for executing pipelines. It automatically
  /// calls Build() if the pipeline hasn't been built yet, then executes and tracks results.
  /// </para>
  /// </remarks>
  public async Task<PipelineResult> RunAsync() {
    var stopwatch = Stopwatch.StartNew();
    var nodeResults = new Dictionary<string, NodeResult>();

    try {
      // Ensure pipeline is built
      if (!IsBuilt) {
        Logger?.LogInformation("Building pipeline before execution");
        Build();
      }

      Logger?.LogInformation("Starting pipeline execution via RunAsync()");

      // Execute all layers
      foreach (var layer in ExecutionLayers!) {
        Logger?.LogInformation("Executing layer with {NodeCount} nodes", layer.Count);

        foreach (var pipelineNode in layer) {
          var nodeResult = await ExecuteNodeWithTrackingAsync(pipelineNode);
          nodeResults[pipelineNode.Name] = nodeResult;

          // If node failed, stop execution
          if (!nodeResult.Success) {
            stopwatch.Stop();
            return PipelineResult.CreateFailure(
              stopwatch.Elapsed,
              nodeResult.Exception!,
              nodeResults,
              Name);
          }
        }
      }

      stopwatch.Stop();
      Logger?.LogInformation(
        "Pipeline execution completed successfully in {ElapsedMs}ms",
        stopwatch.ElapsedMilliseconds);

      return PipelineResult.CreateSuccess(stopwatch.Elapsed, nodeResults, Name);
    } catch (Exception ex) {
      stopwatch.Stop();
      Logger?.LogError(ex, "Pipeline execution failed: {ErrorMessage}", ex.Message);
      return PipelineResult.CreateFailure(stopwatch.Elapsed, ex, nodeResults, Name);
    }
  }

  /// <summary>
  /// Executes the pipeline sequentially, layer by layer.
  /// </summary>
  /// <returns>Task representing the pipeline execution</returns>
  /// <exception cref="InvalidOperationException">Thrown if pipeline has not been built</exception>
  /// <remarks>
  /// <para>
  /// This method executes nodes in topological order:
  /// 1. Execute all nodes in layer 0 sequentially
  /// 2. Execute all nodes in layer 1 sequentially
  /// 3. Continue until all layers are complete
  /// </para>
  /// <para>
  /// <strong>Note:</strong> This method throws exceptions on failure. For result-based
  /// execution with error handling, use RunAsync() instead.
  /// </para>
  /// <para>
  /// In Phase 2, this will be replaced with a parallel executor that can run
  /// nodes within the same layer concurrently.
  /// </para>
  /// </remarks>
  public async Task ExecuteAsync() {
    if (!IsBuilt) {
      throw new InvalidOperationException(
        "Pipeline must be built before execution. Call Build() first.");
    }

    Logger?.LogInformation("Starting pipeline execution");

    try {
      foreach (var layer in ExecutionLayers!) {
        Logger?.LogInformation("Executing layer with {NodeCount} nodes", layer.Count);

        foreach (var pipelineNode in layer) {
          await ExecuteNodeAsync(pipelineNode);
        }
      }

      Logger?.LogInformation("Pipeline execution completed successfully");
    } catch (Exception ex) {
      Logger?.LogError(ex, "Pipeline execution failed: {ErrorMessage}", ex.Message);
      throw;
    }
  }

  /// <summary>
  /// Executes a single node with execution tracking and returns detailed results.
  /// </summary>
  /// <param name="pipelineNode">The node to execute</param>
  /// <returns>NodeResult with execution details</returns>
  private async Task<NodeResult> ExecuteNodeWithTrackingAsync(PipelineNode pipelineNode) {
    var stopwatch = Stopwatch.StartNew();

    try {
      // Get input counts for diagnostics (before loading data)
      var inputCountTasks = pipelineNode.Inputs.Select(entry => entry.GetCountAsync());
      var inputCounts = await Task.WhenAll(inputCountTasks);
      var totalInputCount = inputCounts.Sum();

      Logger?.LogInformation(
        "Executing node: {NodeName} (inputs: {InputCount} observations from {EntryCount} entries)",
        pipelineNode.Name,
        totalInputCount,
        pipelineNode.Inputs.Count);

      // Load inputs from catalog entries
      var inputTasks = pipelineNode.Inputs.Select(async entry => {
        var data = await entry.LoadUntyped();

        // Check if this is a singleton object vs a dataset
        // Nodes always expect IEnumerable<T>, so wrap singletons
        var isSingletonObject = entry.GetType().GetInterfaces()
    .Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ICatalogObject<>));

        if (isSingletonObject) {
          // Wrap singleton in array so node receives IEnumerable<T>
          var arrayType = typeof(object[]);
          var wrappedArray = Array.CreateInstance(data.GetType(), 1);
          wrappedArray.SetValue(data, 0);
          return (object)wrappedArray;
        } else {
          // Dataset: return collection directly
          return data;
        }
      });
      var inputs = await Task.WhenAll(inputTasks);

      // Invoke node transformation via reflection
      // Find ExecuteAsync method on the node instance
      var nodeType = pipelineNode.NodeInstance.GetType();
      var executeMethod = nodeType.GetMethod("ExecuteAsync",
        System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);

      if (executeMethod == null) {
        throw new InvalidOperationException(
          $"Node {pipelineNode.Name} does not have an ExecuteAsync method");
      }

      // Prepare input parameter
      // For single-input nodes: pass the data directly as IEnumerable<TInput>
      // For multi-input nodes: wrap in singleton IEnumerable containing a composite input object
      object inputParameter;
      if (pipelineNode.Inputs.Count == 1 && pipelineNode.InputMappings == null) {
        // Single input: pass data directly
        inputParameter = inputs[0];
      } else {
        // Multi-input: create composite input object using InputMappings
        // The ExecuteAsync signature is: Task<IEnumerable<TOutput>> ExecuteAsync(IEnumerable<TInput> input)
        // For multi-input, TInput is a record/class with properties for each catalog entry
        // We need to wrap the composite object in a singleton IEnumerable

        // Get TInput type from ExecuteAsync method signature
        var executeParams = executeMethod.GetParameters();
        if (executeParams.Length != 1) {
          throw new InvalidOperationException(
            $"ExecuteAsync for node {pipelineNode.Name} should have exactly one parameter");
        }

        var inputEnumerableType = executeParams[0].ParameterType; // IEnumerable<TInput>
        var inputItemType = inputEnumerableType.GetGenericArguments()[0]; // TInput

        // Create instance of TInput
        var compositeInput = Activator.CreateInstance(inputItemType);
        if (compositeInput == null) {
          throw new InvalidOperationException(
            $"Failed to create instance of {inputItemType.Name} for node {pipelineNode.Name}");
        }

        // Map loaded data to properties using InputMappings
        if (pipelineNode.InputMappings != null) {
          foreach (var mapping in pipelineNode.InputMappings) {
            if (mapping is Mapping.CatalogPropertyMapping propertyMapping) {
              // Find the corresponding loaded data
              var catalogEntry = propertyMapping.CatalogEntry;
              var inputIndex = pipelineNode.Inputs.ToList().FindIndex(e => e.Key == catalogEntry.Key);

              if (inputIndex >= 0 && inputIndex < inputs.Length) {
                var data = inputs[inputIndex];

                Logger?.LogDebug(
                  "Mapping catalog entry '{Key}' to property '{PropertyName}' on {TypeName}",
                  catalogEntry.Key,
                  propertyMapping.Property.Name,
                  inputItemType.Name);

                // Set the property value
                if (propertyMapping.Property.CanWrite) {
                  propertyMapping.Property.SetValue(compositeInput, data);

                  Logger?.LogDebug(
                    "Set property '{PropertyName}' with data of type {DataType}",
                    propertyMapping.Property.Name,
                    data?.GetType().Name ?? "null");
                }
              }
            }
          }
        }

        // Wrap in singleton enumerable
        var listType = typeof(List<>).MakeGenericType(inputItemType);
        var list = (System.Collections.IList?)Activator.CreateInstance(listType);
        list?.Add(compositeInput);
        inputParameter = list!;
      }

      // Invoke ExecuteAsync and await the result
      var executeTask = (Task?)executeMethod.Invoke(pipelineNode.NodeInstance, new[] { inputParameter });
      if (executeTask == null) {
        throw new InvalidOperationException(
          $"ExecuteAsync invocation for node {pipelineNode.Name} returned null");
      }

      await executeTask.ConfigureAwait(false);

      // Extract result from Task<TOutput>
      var resultProperty = executeTask.GetType().GetProperty("Result");
      var output = resultProperty?.GetValue(executeTask);

      // Save outputs to catalog entries
      if (output != null && pipelineNode.Outputs.Count > 0) {
        // For single output nodes
        if (pipelineNode.Outputs.Count == 1) {
          var catalogEntry = pipelineNode.Outputs[0];

          // Check if this is a singleton object vs a dataset
          // Nodes always return IEnumerable<T>, but ICatalogObject expects T
          var isSingletonObject = catalogEntry.GetType().GetInterfaces()
            .Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ICatalogObject<>));

          if (isSingletonObject && output is System.Collections.IEnumerable enumerable) {
            // Unwrap singleton from collection
            var enumerator = enumerable.GetEnumerator();
            if (!enumerator.MoveNext()) {
              throw new InvalidOperationException(
                $"Node '{pipelineNode.Name}' returned empty collection for singleton object output '{catalogEntry.Key}'");
            }
            var singletonValue = enumerator.Current;
            if (enumerator.MoveNext()) {
              throw new InvalidOperationException(
                $"Node '{pipelineNode.Name}' returned multiple items for singleton object output '{catalogEntry.Key}'");
            }
            await catalogEntry.SaveUntyped(singletonValue!);
          } else {
            // Dataset: save collection directly
            await catalogEntry.SaveUntyped(output);
          }
        } else {
          // For multi-output nodes, use OutputMappings to correctly map properties to catalog entries
          if (pipelineNode.OutputMappings == null || pipelineNode.OutputMappings.Count == 0) {
            throw new InvalidOperationException(
              $"Node '{pipelineNode.Name}' has multiple outputs but no OutputMappings configured.");
          }

          // Multi-output nodes return IEnumerable<TOutputSchema>, extract the single item
          if (output is not System.Collections.IEnumerable outputEnumerable) {
            throw new InvalidOperationException(
              $"Multi-output node '{pipelineNode.Name}' returned non-enumerable output: {output.GetType().Name}");
          }

          var outputItem = outputEnumerable.Cast<object>().FirstOrDefault();
          if (outputItem == null) {
            throw new InvalidOperationException(
              $"Multi-output node '{pipelineNode.Name}' returned empty output collection");
          }

          foreach (var mapping in pipelineNode.OutputMappings) {
            // OutputMappings should be CatalogPropertyMapping instances
            if (mapping is not Mapping.CatalogPropertyMapping propertyMapping) {
              throw new InvalidOperationException(
                $"Node '{pipelineNode.Name}' has an invalid mapping type: {mapping.GetType().Name}");
            }

            // Use the property info from the mapping (which has the correct property name)
            var propertyValue = propertyMapping.Property.GetValue(outputItem);
            if (propertyValue != null) {
              // Check if the catalog entry is a singleton object
              var isSingletonObject = propertyMapping.CatalogEntry.GetType().GetInterfaces()
                .Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ICatalogObject<>));

              if (isSingletonObject && propertyValue is System.Collections.IEnumerable enumerable and not string) {
                // Unwrap singleton from collection
                var enumerator = enumerable.GetEnumerator();
                if (!enumerator.MoveNext()) {
                  throw new InvalidOperationException(
                    $"Node '{pipelineNode.Name}' property '{propertyMapping.Property.Name}' returned empty collection for singleton object output '{propertyMapping.CatalogEntry.Key}'");
                }
                var singletonValue = enumerator.Current;
                if (enumerator.MoveNext()) {
                  throw new InvalidOperationException(
                    $"Node '{pipelineNode.Name}' property '{propertyMapping.Property.Name}' returned multiple items for singleton object output '{propertyMapping.CatalogEntry.Key}'");
                }
                await propertyMapping.CatalogEntry.SaveUntyped(singletonValue!);
              } else {
                // Dataset or already unwrapped: save directly
                await propertyMapping.CatalogEntry.SaveUntyped(propertyValue);
              }
            }
          }
        }
      }

      stopwatch.Stop();

      // Get output counts for diagnostics (after saving data)
      var outputCountTasks = pipelineNode.Outputs.Select(entry => entry.GetCountAsync());
      var outputCounts = await Task.WhenAll(outputCountTasks);
      var totalOutputCount = outputCounts.Sum();

      Logger?.LogInformation(
        "Node {NodeName} completed: {InputCount} observations in → {OutputCount} observations out ({ElapsedMs}ms)",
        pipelineNode.Name,
        totalInputCount,
        totalOutputCount,
        stopwatch.ElapsedMilliseconds);

      return NodeResult.CreateSuccess(
        pipelineNode.Name,
        stopwatch.Elapsed,
        totalInputCount,
        totalOutputCount);
    } catch (Exception ex) {
      stopwatch.Stop();
      Logger?.LogError(ex, "Node {NodeName} failed: {ErrorMessage}", pipelineNode.Name, ex.Message);
      return NodeResult.CreateFailure(pipelineNode.Name, stopwatch.Elapsed, ex);
    }
  }

  /// <summary>
  /// Executes a single node by loading its inputs, invoking the transformation,
  /// and saving its outputs.
  /// </summary>
  /// <param name="pipelineNode">The node to execute</param>
  private async Task ExecuteNodeAsync(PipelineNode pipelineNode) {
    Logger?.LogInformation("Executing node: {NodeName}", pipelineNode.Name);

    try {
      // Load inputs from catalog entries
      var inputTasks = pipelineNode.Inputs.Select(entry => entry.LoadUntyped());
      var inputs = await Task.WhenAll(inputTasks);

      // TODO: Invoke node transformation
      // This requires either:
      // 1. Reflection to call ExecuteAsync on the NodeBase instance
      // 2. A non-generic INode interface with ExecuteUntyped method
      // 3. Compiled expression to invoke the generic method
      // 
      // For now, we'll need to implement this in the next iteration
      // when we have the full execution context ready

      Logger?.LogWarning(
        "Node execution not yet implemented. Node {NodeName} would process {InputCount} inputs",
        pipelineNode.Name,
        inputs.Length);

      // TODO: Save outputs to catalog entries
    } catch (Exception ex) {
      Logger?.LogError(ex, "Node {NodeName} failed: {ErrorMessage}", pipelineNode.Name, ex.Message);
      throw;
    }
  }
}
