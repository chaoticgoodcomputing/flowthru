using Flowthru.Meta.Providers;

namespace Flowthru.Configuration;

/// <summary>
/// Root configuration options for Flowthru applications.
/// </summary>
/// <remarks>
/// This class represents the top-level "Flowthru" section in configuration files.
/// All Flowthru-specific configuration should be nested under this section.
/// </remarks>
public class FlowthruOptions {
  /// <summary>
  /// Configuration section name in appsettings.json.
  /// </summary>
  public const string SectionName = "Flowthru";

  /// <summary>
  /// Metadata collection and export configuration.
  /// </summary>
  public MetadataOptions Metadata { get; set; } = new();

  /// <summary>
  /// Data catalog configuration.
  /// </summary>
  public CatalogOptions Catalog { get; set; } = new();

  /// <summary>
  /// Pipeline registration and configuration.
  /// </summary>
  public Dictionary<string, PipelineOptions> Pipelines { get; set; } = new();

  /// <summary>
  /// Logging configuration (extends standard .NET logging configuration).
  /// </summary>
  public LoggingOptions? Logging { get; set; }
}

/// <summary>
/// Configuration options for metadata collection and export.
/// </summary>
public class MetadataOptions {
  /// <summary>
  /// Whether metadata collection is enabled.
  /// </summary>
  public bool Enabled { get; set; } = true;

  /// <summary>
  /// Directory where metadata files will be written.
  /// </summary>
  public string OutputDirectory { get; set; } = "metadata";

  /// <summary>
  /// List of metadata providers to enable (e.g., "Json", "Mermaid", "Csv").
  /// </summary>
  public List<string> Providers { get; set; } = new() { "Json", "Mermaid" };

  /// <summary>
  /// Configuration specific to the JSON metadata provider.
  /// </summary>
  public JsonMetadataOptions? Json { get; set; }

  /// <summary>
  /// Configuration specific to the Mermaid metadata provider.
  /// </summary>
  public MermaidMetadataOptions? Mermaid { get; set; }
}

/// <summary>
/// Configuration options for JSON metadata export.
/// </summary>
public class JsonMetadataOptions {
  /// <summary>
  /// Whether to use compact (minified) JSON format.
  /// </summary>
  public bool UseCompactFormat { get; set; } = false;

  /// <summary>
  /// Whether to include full type information in the export.
  /// </summary>
  public bool IncludeTypeInfo { get; set; } = true;
}

/// <summary>
/// Configuration options for Mermaid diagram export.
/// </summary>
public class MermaidMetadataOptions {
  /// <summary>
  /// Flowchart direction (TopToBottom, LeftToRight, etc.).
  /// </summary>
  public string Direction { get; set; } = "LeftToRight";

  /// <summary>
  /// Whether to include dataset details in nodes.
  /// </summary>
  public bool ShowDatasetDetails { get; set; } = true;

  /// <summary>
  /// Whether to include parameter information in nodes.
  /// </summary>
  public bool ShowParameters { get; set; } = true;
}

/// <summary>
/// Configuration options for data catalog construction.
/// </summary>
public class CatalogOptions {
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
/// Configuration options for a single pipeline.
/// </summary>
public class PipelineOptions {
  /// <summary>
  /// The fully-qualified type name of the pipeline factory class.
  /// Must have a static Create method that accepts (catalog, parameters?).
  /// </summary>
  public string? Type { get; set; }

  /// <summary>
  /// The name of the static factory method (default: "Create").
  /// </summary>
  public string FactoryMethod { get; set; } = "Create";

  /// <summary>
  /// Human-readable description of the pipeline.
  /// </summary>
  public string? Description { get; set; }

  /// <summary>
  /// Tags for categorizing the pipeline.
  /// </summary>
  public List<string> Tags { get; set; } = new();

  /// <summary>
  /// Pipeline-specific parameters (nested configuration section).
  /// The structure must match the pipeline's parameter type.
  /// </summary>
  public Dictionary<string, object>? Parameters { get; set; }

  /// <summary>
  /// Validation configuration for this pipeline.
  /// </summary>
  public PipelineValidationOptions? Validation { get; set; }
}

/// <summary>
/// Configuration options for pipeline validation behavior.
/// </summary>
public class PipelineValidationOptions {
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
public class LoggingOptions {
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
