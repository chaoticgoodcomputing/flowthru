using Microsoft.Extensions.Configuration;
using System.ComponentModel.DataAnnotations;

namespace Flowthru.Configuration;

/// <summary>
/// Extension methods for configuration-related operations.
/// </summary>
public static class ConfigurationExtensions {
  /// <summary>
  /// Binds a configuration section to a strongly-typed object and validates it.
  /// </summary>
  /// <typeparam name="T">The type to bind to</typeparam>
  /// <param name="configuration">The configuration instance</param>
  /// <param name="sectionPath">The configuration section path (e.g., "DataScience:ModelParams")</param>
  /// <returns>The bound and validated object</returns>
  /// <exception cref="ValidationException">Thrown if DataAnnotations validation fails</exception>
  public static T GetValidated<T>(this IConfiguration configuration, string sectionPath) where T : new() {
    var section = configuration.GetSection(sectionPath);
    if (!section.Exists()) {
      throw new InvalidOperationException(
        $"Configuration section '{sectionPath}' not found. " +
        $"Ensure {sectionPath} is defined in your configuration files.");
    }

    var instance = new T();
    section.Bind(instance);

    // Validate using DataAnnotations
    var validationContext = new ValidationContext(instance);
    var validationResults = new List<ValidationResult>();

    if (!Validator.TryValidateObject(instance, validationContext, validationResults, validateAllProperties: true)) {
      var errors = string.Join(Environment.NewLine,
        validationResults.Select(r => $"  - {r.ErrorMessage}"));
      throw new ValidationException(
        $"Configuration validation failed for '{sectionPath}':{Environment.NewLine}{errors}");
    }

    return instance;
  }

  /// <summary>
  /// Attempts to bind and validate a configuration section, returning null if not found.
  /// </summary>
  /// <typeparam name="T">The type to bind to</typeparam>
  /// <param name="configuration">The configuration instance</param>
  /// <param name="sectionPath">The configuration section path</param>
  /// <returns>The bound and validated object, or null if section doesn't exist</returns>
  /// <exception cref="ValidationException">Thrown if DataAnnotations validation fails</exception>
  public static T? GetValidatedOrDefault<T>(this IConfiguration configuration, string sectionPath) where T : class, new() {
    var section = configuration.GetSection(sectionPath);
    if (!section.Exists()) {
      return null;
    }

    return GetValidated<T>(configuration, sectionPath);
  }

  /// <summary>
  /// Binds a configuration section to a strongly-typed object of a specific runtime type and validates it.
  /// </summary>
  /// <param name="configuration">The configuration instance</param>
  /// <param name="sectionPath">The configuration section path</param>
  /// <param name="type">The runtime type to bind to</param>
  /// <returns>The bound and validated object</returns>
  /// <exception cref="ValidationException">Thrown if DataAnnotations validation fails</exception>
  internal static object GetValidated(this IConfiguration configuration, string sectionPath, Type type) {
    var section = configuration.GetSection(sectionPath);
    if (!section.Exists()) {
      throw new InvalidOperationException(
        $"Configuration section '{sectionPath}' not found. " +
        $"Ensure {sectionPath} is defined in your configuration files.");
    }

    var instance = Activator.CreateInstance(type);
    if (instance == null) {
      throw new InvalidOperationException(
        $"Could not create instance of type '{type.Name}'. Ensure it has a parameterless constructor.");
    }

    section.Bind(instance);

    // Validate using DataAnnotations
    var validationContext = new ValidationContext(instance);
    var validationResults = new List<ValidationResult>();

    if (!Validator.TryValidateObject(instance, validationContext, validationResults, validateAllProperties: true)) {
      var errors = string.Join(Environment.NewLine,
        validationResults.Select(r => $"  - {r.ErrorMessage}"));
      throw new ValidationException(
        $"Configuration validation failed for '{sectionPath}':{Environment.NewLine}{errors}");
    }

    return instance;
  }
}
