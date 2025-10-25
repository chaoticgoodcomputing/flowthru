using Flowthru.Data;

namespace Flowthru.Configuration;

/// <summary>
/// Factory interface for creating data catalog instances from configuration.
/// </summary>
/// <remarks>
/// Implement this interface to enable configuration-based catalog construction.
/// The factory receives the full configuration and can use it to construct
/// environment-specific catalogs (e.g., local files in dev, remote DB in prod).
/// </remarks>
public interface ICatalogFactory {
  /// <summary>
  /// Creates a catalog instance based on configuration.
  /// </summary>
  /// <param name="options">Catalog configuration options</param>
  /// <param name="serviceProvider">Service provider for dependency injection</param>
  /// <returns>The configured catalog instance</returns>
  DataCatalogBase CreateCatalog(CatalogOptions options, IServiceProvider serviceProvider);
}

/// <summary>
/// Default catalog factory that uses reflection to construct catalogs.
/// </summary>
/// <remarks>
/// This factory attempts to create catalog instances by:
/// 1. Finding the catalog type by name
/// 2. Matching constructor parameters to configuration values
/// 3. Invoking the constructor with provided arguments
/// </remarks>
internal class ReflectionCatalogFactory : ICatalogFactory {
  public DataCatalogBase CreateCatalog(CatalogOptions options, IServiceProvider serviceProvider) {
    if (string.IsNullOrWhiteSpace(options.Type)) {
      throw new InvalidOperationException(
        "Catalog.Type must be specified in configuration when using configuration-based catalog construction. " +
        "Example: \"Catalog\": { \"Type\": \"MyApp.Data.MyCatalog\" }");
    }

    // Find the catalog type
    Type? catalogType = null;

    // Try Type.GetType first (supports fully qualified names with assembly)
    catalogType = Type.GetType(options.Type);

    // If not found, search all loaded assemblies
    if (catalogType == null) {
      foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies()) {
        catalogType = assembly.GetType(options.Type);
        if (catalogType != null) {
          break;
        }
      }
    }

    if (catalogType == null) {
      throw new InvalidOperationException(
        $"Could not find catalog type '{options.Type}'. Ensure the type name is fully qualified " +
        $"(e.g., 'MyApp.Data.MyCatalog') or includes the assembly name (e.g., 'MyApp.Data.MyCatalog, MyApp').");
    }

    if (!typeof(DataCatalogBase).IsAssignableFrom(catalogType)) {
      throw new InvalidOperationException(
        $"Type '{options.Type}' does not inherit from DataCatalogBase.");
    }

    // Get all constructors and try to find one that matches our configuration
    var constructors = catalogType.GetConstructors();
    if (constructors.Length == 0) {
      throw new InvalidOperationException(
        $"Catalog type '{options.Type}' has no public constructors.");
    }

    // Prepare constructor arguments
    var args = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

    // Add explicit constructor args
    foreach (var kvp in options.ConstructorArgs) {
      args[kvp.Key] = kvp.Value;
    }

    // Add common arguments if not already present
    if (options.BasePath != null && !args.ContainsKey("basePath")) {
      args["basePath"] = options.BasePath;
    }
    if (options.ConnectionString != null && !args.ContainsKey("connectionString")) {
      args["connectionString"] = options.ConnectionString;
    }

    // Try each constructor until we find one that works
    foreach (var constructor in constructors.OrderByDescending(c => c.GetParameters().Length)) {
      var parameters = constructor.GetParameters();
      var constructorArgs = new object?[parameters.Length];
      var allMatched = true;

      for (int i = 0; i < parameters.Length; i++) {
        var param = parameters[i];
        if (args.TryGetValue(param.Name ?? "", out var value)) {
          // Try to convert the value to the parameter type
          try {
            constructorArgs[i] = Convert.ChangeType(value, param.ParameterType);
          } catch {
            allMatched = false;
            break;
          }
        } else if (param.HasDefaultValue) {
          constructorArgs[i] = param.DefaultValue;
        } else {
          allMatched = false;
          break;
        }
      }

      if (allMatched) {
        var catalog = (DataCatalogBase)constructor.Invoke(constructorArgs);
        return catalog;
      }
    }

    throw new InvalidOperationException(
      $"Could not find a suitable constructor for catalog type '{options.Type}'. " +
      $"Available constructor arguments in configuration: {string.Join(", ", args.Keys)}. " +
      $"Required constructor parameters: {string.Join(", ", constructors[0].GetParameters().Select(p => $"{p.Name} ({p.ParameterType.Name})"))}");
  }
}
