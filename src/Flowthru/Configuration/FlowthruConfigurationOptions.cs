namespace Flowthru.Configuration;

/// <summary>
/// Options for configuring how Flowthru loads configuration files.
/// </summary>
/// <remarks>
/// <para>
/// Flowthru uses Microsoft.Extensions.Configuration with layered configuration files.
/// By default, configuration is loaded in the following order (later files override earlier):
/// </para>
/// <list type="number">
/// <item><c>appsettings.json</c> - Base configuration (required)</item>
/// <item><c>appsettings.{Environment}.json</c> - Environment-specific overrides (optional)</item>
/// <item><c>appsettings.Local.json</c> - Local/user-specific overrides (optional, gitignored)</item>
/// </list>
/// <para>
/// Both JSON and YAML formats are supported. YAML files follow the same pattern:
/// <c>appsettings.yml</c>, <c>appsettings.{Environment}.yml</c>, <c>appsettings.Local.yml</c>
/// </para>
/// </remarks>
public class FlowthruConfigurationOptions {
  /// <summary>
  /// The base path where configuration files are located.
  /// </summary>
  /// <remarks>
  /// Defaults to the current directory. Can be set to "conf" for Kedro-style projects
  /// or any other directory containing configuration files.
  /// </remarks>
  public string ConfigurationPath { get; set; } = ".";

  /// <summary>
  /// The environment name used to load environment-specific configuration files.
  /// </summary>
  /// <remarks>
  /// <para>
  /// If not explicitly set, Flowthru will attempt to resolve the environment in this order:
  /// </para>
  /// <list type="number">
  /// <item>Value passed to <c>WithEnvironment()</c></item>
  /// <item>Environment variable specified by <see cref="EnvironmentVariable"/></item>
  /// <item><c>DOTNET_ENVIRONMENT</c> environment variable</item>
  /// <item><c>ASPNETCORE_ENVIRONMENT</c> environment variable</item>
  /// <item>"Production" (default)</item>
  /// </list>
  /// </remarks>
  public string? Environment { get; set; }

  /// <summary>
  /// The name of the environment variable to check for environment name.
  /// </summary>
  /// <remarks>
  /// Defaults to "FLOWTHRU_ENV". Set to null to disable environment variable resolution.
  /// Standard .NET environment variables (DOTNET_ENVIRONMENT, ASPNETCORE_ENVIRONMENT) 
  /// are always checked as fallbacks.
  /// </remarks>
  public string? EnvironmentVariable { get; set; } = "FLOWTHRU_ENV";

  /// <summary>
  /// Whether to support YAML configuration files in addition to JSON.
  /// </summary>
  /// <remarks>
  /// When enabled, Flowthru will load both .json and .yml/.yaml files.
  /// Requires NetEscapades.Configuration.Yaml package.
  /// Defaults to true for Kedro compatibility.
  /// </remarks>
  public bool EnableYamlSupport { get; set; } = true;

  /// <summary>
  /// The base filename for configuration files (without extension).
  /// </summary>
  /// <remarks>
  /// Defaults to "appsettings". Change this to use a different naming convention
  /// (e.g., "parameters" to match Kedro's convention).
  /// </remarks>
  public string ConfigurationFileName { get; set; } = "appsettings";

  /// <summary>
  /// Gets the resolved environment name, checking all sources in priority order.
  /// </summary>
  internal string GetResolvedEnvironment() {
    // 1. Explicitly set environment
    if (!string.IsNullOrWhiteSpace(Environment)) {
      return Environment;
    }

    // 2. Custom environment variable
    if (!string.IsNullOrWhiteSpace(EnvironmentVariable)) {
      var customEnv = System.Environment.GetEnvironmentVariable(EnvironmentVariable);
      if (!string.IsNullOrWhiteSpace(customEnv)) {
        return customEnv;
      }
    }

    // 3. Standard .NET environment variables
    var dotnetEnv = System.Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT");
    if (!string.IsNullOrWhiteSpace(dotnetEnv)) {
      return dotnetEnv;
    }

    var aspnetEnv = System.Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
    if (!string.IsNullOrWhiteSpace(aspnetEnv)) {
      return aspnetEnv;
    }

    // 4. Default to Production
    return "Production";
  }

  /// <summary>
  /// Sets the base path where configuration files are located.
  /// </summary>
  /// <param name="path">The configuration directory path</param>
  /// <returns>This options instance for fluent chaining</returns>
  public FlowthruConfigurationOptions WithConfigurationPath(string path) {
    ConfigurationPath = path ?? throw new ArgumentNullException(nameof(path));
    return this;
  }

  /// <summary>
  /// Sets the environment name explicitly.
  /// </summary>
  /// <param name="environment">The environment name (e.g., "Development", "Production")</param>
  /// <returns>This options instance for fluent chaining</returns>
  public FlowthruConfigurationOptions WithEnvironment(string environment) {
    Environment = environment ?? throw new ArgumentNullException(nameof(environment));
    return this;
  }

  /// <summary>
  /// Sets the environment variable name to check for environment resolution.
  /// </summary>
  /// <param name="variableName">The environment variable name</param>
  /// <returns>This options instance for fluent chaining</returns>
  public FlowthruConfigurationOptions WithEnvironmentVariable(string? variableName) {
    EnvironmentVariable = variableName;
    return this;
  }

  /// <summary>
  /// Sets the base filename for configuration files (without extension).
  /// </summary>
  /// <param name="fileName">The base filename (e.g., "parameters", "config")</param>
  /// <returns>This options instance for fluent chaining</returns>
  public FlowthruConfigurationOptions WithConfigurationFileName(string fileName) {
    ConfigurationFileName = fileName ?? throw new ArgumentNullException(nameof(fileName));
    return this;
  }

  /// <summary>
  /// Enables or disables YAML configuration file support.
  /// </summary>
  /// <param name="enabled">Whether to enable YAML support</param>
  /// <returns>This options instance for fluent chaining</returns>
  public FlowthruConfigurationOptions WithYamlSupport(bool enabled = true) {
    EnableYamlSupport = enabled;
    return this;
  }
}
