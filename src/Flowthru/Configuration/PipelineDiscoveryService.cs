using Flowthru.Data;
using Flowthru.Pipelines;
using Microsoft.Extensions.Configuration;
using System.Reflection;

namespace Flowthru.Configuration;

/// <summary>
/// Discovers and registers pipelines from configuration.
/// </summary>
internal class PipelineDiscoveryService {
  /// <summary>
  /// Discovers pipelines from configuration and returns factory functions.
  /// </summary>
  /// <param name="configuration">Configuration instance</param>
  /// <param name="catalogType">The catalog type to use</param>
  /// <returns>Dictionary of pipeline label to factory function</returns>
  public static Dictionary<string, PipelineFactoryInfo> DiscoverPipelines(
    IConfiguration configuration,
    Type catalogType) {

    var flowthruConfig = configuration.GetSection(FlowthruOptions.SectionName);
    var pipelinesSection = flowthruConfig.GetSection("Pipelines");

    if (!pipelinesSection.Exists()) {
      return new Dictionary<string, PipelineFactoryInfo>();
    }

    var pipelines = new Dictionary<string, PipelineFactoryInfo>();

    foreach (var pipelineSection in pipelinesSection.GetChildren()) {
      var label = pipelineSection.Key;
      var options = new PipelineOptions();
      pipelineSection.Bind(options);

      if (string.IsNullOrWhiteSpace(options.Type)) {
        throw new InvalidOperationException(
          $"Pipeline '{label}' is missing required 'Type' configuration. " +
          $"Example: \"Pipelines\": {{ \"{label}\": {{ \"Type\": \"MyApp.Pipelines.MyPipeline\" }} }}");
      }

      var factoryInfo = CreateFactoryInfo(label, options, catalogType, pipelineSection);
      pipelines[label] = factoryInfo;
    }

    return pipelines;
  }

  private static PipelineFactoryInfo CreateFactoryInfo(
    string label,
    PipelineOptions options,
    Type catalogType,
    IConfigurationSection pipelineSection) {

    // Find the pipeline factory type
    Type? factoryType = null;

    // Try Type.GetType first (supports fully qualified names with assembly)
    factoryType = Type.GetType(options.Type!);

    // If not found, search all loaded assemblies
    if (factoryType == null) {
      foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies()) {
        factoryType = assembly.GetType(options.Type!);
        if (factoryType != null) {
          break;
        }
      }
    }

    if (factoryType == null) {
      throw new InvalidOperationException(
        $"Could not find pipeline type '{options.Type}' for pipeline '{label}'. " +
        $"Ensure the type name is fully qualified (e.g., 'MyApp.Pipelines.MyPipeline').");
    }

    // Find the factory method (usually a static Create method)
    var factoryMethod = factoryType.GetMethod(
      options.FactoryMethod,
      BindingFlags.Public | BindingFlags.Static);

    if (factoryMethod == null) {
      throw new InvalidOperationException(
        $"Could not find static method '{options.FactoryMethod}' on type '{options.Type}' for pipeline '{label}'. " +
        $"Expected signature: public static Pipeline Create({catalogType.Name} catalog) or " +
        $"public static Pipeline Create({catalogType.Name} catalog, TParams parameters)");
    }

    var parameters = factoryMethod.GetParameters();
    if (parameters.Length == 0 || !catalogType.IsAssignableFrom(parameters[0].ParameterType)) {
      throw new InvalidOperationException(
        $"Factory method '{options.FactoryMethod}' on type '{options.Type}' must have " +
        $"a first parameter of type {catalogType.Name} (or compatible).");
    }

    // Check if this is a parameterless or parameterized pipeline
    Type? parameterType = null;
    object? parameterInstance = null;

    if (parameters.Length > 1) {
      // Parameterized pipeline
      parameterType = parameters[1].ParameterType;

      // Load and validate parameters from configuration
      var parametersSection = pipelineSection.GetSection("Parameters");
      if (!parametersSection.Exists() && options.Parameters == null) {
        throw new InvalidOperationException(
          $"Pipeline '{label}' requires parameters of type '{parameterType.Name}', " +
          $"but no 'Parameters' section was found in configuration.");
      }

      parameterInstance = ConfigurationExtensions.GetValidated(
        pipelineSection,
        "Parameters",
        parameterType);
    }

    return new PipelineFactoryInfo {
      Label = label,
      FactoryType = factoryType,
      FactoryMethod = factoryMethod,
      ParameterType = parameterType,
      ParameterInstance = parameterInstance,
      Description = options.Description,
      Tags = options.Tags.ToArray(),
      ValidationOptions = options.Validation
    };
  }
}

/// <summary>
/// Information about a discovered pipeline factory.
/// </summary>
internal class PipelineFactoryInfo {
  public required string Label { get; init; }
  public required Type FactoryType { get; init; }
  public required MethodInfo FactoryMethod { get; init; }
  public Type? ParameterType { get; init; }
  public object? ParameterInstance { get; init; }
  public string? Description { get; init; }
  public string[] Tags { get; init; } = Array.Empty<string>();
  public PipelineValidationOptions? ValidationOptions { get; init; }

  /// <summary>
  /// Invokes the factory method to create a pipeline instance.
  /// </summary>
  public Pipeline CreatePipeline(DataCatalogBase catalog) {
    var args = ParameterInstance != null
      ? new object[] { catalog, ParameterInstance }
      : new object[] { catalog };

    if (FactoryMethod.Invoke(null, args) is not Pipeline pipeline) {
      throw new InvalidOperationException(
        $"Factory method '{FactoryMethod.Name}' on type '{FactoryType.Name}' returned null or non-Pipeline value.");
    }

    return pipeline;
  }
}
