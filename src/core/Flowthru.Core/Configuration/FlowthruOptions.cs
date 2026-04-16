namespace Flowthru.Core.Configuration;

/// <summary>
/// Root configuration options for Flowthru applications.
/// </summary>
/// <remarks>
/// This class represents the top-level "Flowthru" section in configuration files.
/// All Flowthru-specific configuration should be nested under this section.
/// </remarks>
public class FlowthruOptions
{
  /// <summary>
  /// Configuration section name in appsettings.json.
  /// </summary>
  public const string SectionName = "Flowthru";

  /// <summary>
  /// Data catalog configuration.
  /// </summary>
  public CatalogOptions Catalog { get; set; } = new();

  /// <summary>
  /// Flow registration and configuration.
  /// </summary>
  public Dictionary<string, FlowOptions> Flows { get; set; } = new();

  /// <summary>
  /// Logging configuration (extends standard .NET logging configuration).
  /// </summary>
  public LoggingOptions? Logging { get; set; }
}

/// <summary>
/// Configuration options for data catalog construction.
/// </summary>
public class CatalogOptions
{
  /// <summary>
  /// The fully-qualified type name of the catalog class (e.g., "MyApp.Data.MyCatalog").
  /// </summary>
  public string? Type { get; set; }

  /// <summary>
  /// Constructor arguments for the catalog (mapped to constructor parameters by name).
  /// </summary>
  public Dictionary<string, object> ConstructorArgs { get; set; } = new();

  /// <summary>
  /// Base path for dataset files (common constructor parameter).
  /// </summary>
  public string? BasePath { get; set; }

  /// <summary>
  /// Connection string for database catalogs (common constructor parameter).
  /// </summary>
  public string? ConnectionString { get; set; }

  /// <summary>
  /// Environment-specific catalog configuration (e.g., local vs. remote).
  /// </summary>
  public string? Environment { get; set; }
}

/// <summary>
/// Configuration options for a single flow.
/// </summary>
public class FlowOptions
{
  /// <summary>
  /// The fully-qualified type name of the Flow factory class.
  /// Must have a static Create method that accepts (catalog, parameters?).
  /// </summary>
  public string? Type { get; set; }

  /// <summary>
  /// The name of the static factory method (default: "Create").
  /// </summary>
  public string FactoryMethod { get; set; } = "Create";

  /// <summary>
  /// Human-readable description of the Flow.
  /// </summary>
  public string? Description { get; set; }

  /// <summary>
  /// Flow-specific parameters (nested configuration section).
  /// The structure must match the Flow's parameter type.
  /// </summary>
  public Dictionary<string, object>? Parameters { get; set; }

  /// <summary>
  /// Validation configuration for this flow.
  /// </summary>
  public FlowValidationOptions? Validation { get; set; }
}

/// <summary>
/// Configuration options for Flow validation behavior.
/// </summary>
public class FlowValidationOptions
{
  /// <summary>
  /// Default inspection level for all Layer 0 inputs.
  /// </summary>
  public string? DefaultInspectionLevel { get; set; }

  /// <summary>
  /// Per-catalog-entry inspection level overrides.
  /// Key: catalog entry key, Value: inspection level (None, Shallow, Deep).
  /// </summary>
  public Dictionary<string, string> InspectionLevels { get; set; } = new();
}

/// <summary>
/// Logging configuration options (extends standard .NET logging).
/// </summary>
public class LoggingOptions
{
  /// <summary>
  /// Minimum log level (Trace, Debug, Information, Warning, Error, Critical).
  /// </summary>
  public string MinimumLevel { get; set; } = "Information";

  /// <summary>
  /// Whether console logging is enabled.
  /// </summary>
  public bool EnableConsole { get; set; } = true;

  /// <summary>
  /// Per-category log level overrides.
  /// </summary>
  public Dictionary<string, string> LogLevel { get; set; } = new();
}

/// <summary>
/// Configuration for timestamp generation in metadata filenames.
/// </summary>
public class TimestampConfiguration
{
  /// <summary>
  /// Whether to include a timestamp in the filename.
  /// </summary>
  public bool IncludeTimestamp { get; set; } = true;

  /// <summary>
  /// Timestamp format string (see .NET DateTime formatting).
  /// </summary>
  public string Format { get; set; } = "yyyyMMdd-HHmmss";

  /// <summary>
  /// Time zone for the timestamp (e.g., "UTC", "Local").
  /// </summary>
  public string TimeZone { get; set; } = "UTC";
}
