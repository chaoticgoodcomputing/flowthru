using System.Reflection;
using Flowthru.Data;
using Flowthru.Flows;
using Microsoft.Extensions.Configuration;

namespace Flowthru.Configuration;

/// <summary>
/// Discovers and registers flows from configuration.
/// </summary>
internal class FlowDiscoveryService
{
  /// <summary>
  /// Discovers flows from configuration and returns factory functions.
  /// </summary>
  /// <param name="configuration">Configuration instance</param>
  /// <param name="catalogType">The catalog type to use</param>
  /// <returns>Dictionary of Flow label to factory function</returns>
  public static Dictionary<string, FlowFactoryInfo> DiscoverFlows(
    IConfiguration configuration,
    Type catalogType
  )
  {
    var flowthruConfig = configuration.GetSection(FlowthruOptions.SectionName);
    var flowsSection = flowthruConfig.GetSection("Flows");

    if (!flowsSection.Exists())
    {
      return new Dictionary<string, FlowFactoryInfo>();
    }

    var flows = new Dictionary<string, FlowFactoryInfo>();

    foreach (var flowSection in flowsSection.GetChildren())
    {
      var label = flowSection.Key;
      var options = new FlowOptions();
      flowSection.Bind(options);

      if (string.IsNullOrWhiteSpace(options.Type))
      {
        throw new InvalidOperationException(
          $"Flow '{label}' is missing required 'Type' configuration. "
            + $"Example: \"Flows\": {{ \"{label}\": {{ \"Type\": \"MyApp.Flows.MyFlow\" }} }}"
        );
      }

      var factoryInfo = CreateFactoryInfo(label, options, catalogType, flowSection);
      flows[label] = factoryInfo;
    }

    return flows;
  }

  private static FlowFactoryInfo CreateFactoryInfo(
    string label,
    FlowOptions options,
    Type catalogType,
    IConfigurationSection flowSection
  )
  {
    // Find the Flow factory type
    Type? factoryType = null;

    // Try Type.GetType first (supports fully qualified names with assembly)
    factoryType = Type.GetType(options.Type!);

    // If not found, search all loaded assemblies
    if (factoryType == null)
    {
      foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
      {
        factoryType = assembly.GetType(options.Type!);
        if (factoryType != null)
        {
          break;
        }
      }
    }

    if (factoryType == null)
    {
      throw new InvalidOperationException(
        $"Could not find flow type '{options.Type}' for flow '{label}'. "
          + $"Ensure the type name is fully qualified (e.g., 'MyApp.Flows.MyFlow')."
      );
    }

    // Find the factory method (usually a static Create method)
    var factoryMethod = factoryType.GetMethod(
      options.FactoryMethod,
      BindingFlags.Public | BindingFlags.Static
    );

    if (factoryMethod == null)
    {
      throw new InvalidOperationException(
        $"Could not find static method '{options.FactoryMethod}' on type '{options.Type}' for Flow '{label}'. "
          + $"Expected signature: public static Flow Create({catalogType.Name} catalog) or "
          + $"public static Flow Create({catalogType.Name} catalog, TParams parameters)"
      );
    }

    var parameters = factoryMethod.GetParameters();
    if (parameters.Length == 0 || !catalogType.IsAssignableFrom(parameters[0].ParameterType))
    {
      throw new InvalidOperationException(
        $"Factory method '{options.FactoryMethod}' on type '{options.Type}' must have "
          + $"a first parameter of type {catalogType.Name} (or compatible)."
      );
    }

    // Check if this is a parameterless or parameterized flow
    Type? parameterType = null;
    object? parameterInstance = null;

    if (parameters.Length > 1)
    {
      // Parameterized flow
      parameterType = parameters[1].ParameterType;

      // Load and validate parameters from configuration
      var parametersSection = flowSection.GetSection("Parameters");
      if (!parametersSection.Exists() && options.Parameters == null)
      {
        throw new InvalidOperationException(
          $"Flow '{label}' requires parameters of type '{parameterType.Name}', "
            + $"but no 'Parameters' section was found in configuration."
        );
      }

      parameterInstance = ConfigurationExtensions.GetValidated(
        flowSection,
        "Parameters",
        parameterType
      );
    }

    return new FlowFactoryInfo
    {
      Label = label,
      FactoryType = factoryType,
      FactoryMethod = factoryMethod,
      ParameterType = parameterType,
      ParameterInstance = parameterInstance,
      Description = options.Description,
      ValidationOptions = options.Validation,
    };
  }
}

/// <summary>
/// Information about a discovered Flow factory.
/// </summary>
internal class FlowFactoryInfo
{
  public required string Label { get; init; }
  public required Type FactoryType { get; init; }
  public required MethodInfo FactoryMethod { get; init; }
  public Type? ParameterType { get; init; }
  public object? ParameterInstance { get; init; }
  public string? Description { get; init; }
  public FlowValidationOptions? ValidationOptions { get; init; }

  /// <summary>
  /// Invokes the factory method to create a Flow instance.
  /// </summary>
  public Flow CreateFlow(CatalogAbstract catalog)
  {
    var args =
      ParameterInstance != null
        ? new object[] { catalog, ParameterInstance }
        : new object[] { catalog };

    if (FactoryMethod.Invoke(null, args) is not Flow flow)
    {
      throw new InvalidOperationException(
        $"Factory method '{FactoryMethod.Name}' on type '{FactoryType.Name}' returned null or non-Flow value."
      );
    }

    return flow;
  }
}
