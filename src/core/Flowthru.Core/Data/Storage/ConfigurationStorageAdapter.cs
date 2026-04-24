using System.ComponentModel.DataAnnotations;
using Flowthru.Core.Data.Capabilities;
using Flowthru.Core.Effects;
using Microsoft.Extensions.Configuration;

namespace Flowthru.Core.Data.Storage;

/// <summary>
/// Read-only storage adapter that binds an <see cref="IConfiguration"/> section to a typed POCO.
/// </summary>
/// <typeparam name="T">
/// Configuration POCO type. Must have a public parameterless constructor so
/// <see cref="IConfiguration.Get{T}"/> can materialize it.
/// </typeparam>
/// <remarks>
/// <para>
/// Treats a configuration section as a first-class catalog item so that flow steps can
/// declare configuration dependencies as DAG inputs. This gives configuration the same
/// fail-fast guarantees as other catalog items:
/// </para>
/// <list type="bullet">
/// <item>Missing section detected during pre-flight (<see cref="InspectShallow"/>).</item>
/// <item>Binding/validation failures detected during pre-flight, not at step execution.</item>
/// <item>Type mismatches between step input and config POCO are caught at compile time via
///     the <see cref="IStorageAdapter{T}"/> generic parameter.</item>
/// </list>
/// <para>
/// <strong>Storage traits:</strong> read-only, non-persistent. <see cref="Save"/> always
/// throws <see cref="NotSupportedException"/>.
/// </para>
/// </remarks>
public sealed class ConfigurationStorageAdapter<T> : IStorageAdapter<T>
  where T : class, new()
{
  private readonly string _sectionPath;
  private readonly IConfiguration _configuration;

  /// <summary>
  /// Initializes a new instance of <see cref="ConfigurationStorageAdapter{T}"/>.
  /// </summary>
  /// <param name="sectionPath">Dot-separated configuration section path (e.g. <c>Flowthru:Flows:DataScience:ModelOptions</c>).</param>
  /// <param name="configuration">The root or scoped <see cref="IConfiguration"/> instance.</param>
  public ConfigurationStorageAdapter(string sectionPath, IConfiguration configuration)
  {
    _sectionPath = sectionPath ?? throw new ArgumentNullException(nameof(sectionPath));
    _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
  }

  /// <inheritdoc/>
  /// <remarks>Read-only (<c>CanWrite = false</c>), non-persistent (<c>IsPersistent = false</c>).</remarks>
  public StorageTraits Traits { get; } =
    new StorageTraits
    {
      CanRead = true,
      CanWrite = false,
      IsPersistent = false,
    };

  /// <inheritdoc/>
  /// <remarks>Binds the configuration section to <typeparamref name="T"/>. Returns a failure effect if the section is missing.</remarks>
  public FlowIO<T> Load() =>
    FlowIO.Lift(() =>
    {
      var section = _configuration.GetSection(_sectionPath);
      if (!section.Exists())
      {
        throw new InvalidOperationException(
          $"Configuration section '{_sectionPath}' not found. "
            + $"Ensure it is defined in your configuration files."
        );
      }

      var instance = section.Get<T>() ?? new T();
      section.Bind(instance);
      return instance;
    });

  /// <inheritdoc/>
  /// <remarks>Always throws <see cref="NotSupportedException"/>. Configuration is read-only.</remarks>
  public FlowIO<FlowUnit> Save(T data) =>
    FlowIO.Lift<FlowUnit>(
      () =>
        throw new NotSupportedException(
          $"ConfigurationStorageAdapter<{typeof(T).Name}> is read-only. "
            + "Configuration sections cannot be written to."
        )
    );

  /// <inheritdoc/>
  public FlowIO<bool> Exists() =>
    FlowIO.Lift(() => _configuration.GetSection(_sectionPath).Exists());

  /// <inheritdoc/>
  /// <remarks>
  /// Verifies the section exists, binds successfully to <typeparamref name="T"/>, and
  /// passes DataAnnotations validation. Config binding is atomic — there is no
  /// row-level concept — so shallow and deep inspection are equivalent.
  /// </remarks>
  public FlowIO<Data.Validation.ValidationResult> InspectShallow(int sampleSize) =>
    FlowIO.Lift(() => Validate());

  /// <inheritdoc/>
  /// <remarks>Equivalent to <see cref="InspectShallow"/> — configuration binding is atomic.</remarks>
  public FlowIO<Data.Validation.ValidationResult> InspectDeep() => FlowIO.Lift(() => Validate());

  private Data.Validation.ValidationResult Validate()
  {
    var section = _configuration.GetSection(_sectionPath);

    if (!section.Exists())
    {
      return Data.Validation.ValidationResult.Failure(
        catalogKey: _sectionPath,
        errorType: Data.Validation.ValidationErrorType.NotFound,
        message: $"Configuration section '{_sectionPath}' not found.",
        details: $"Ensure '{_sectionPath}' is defined in your appsettings.json or equivalent."
      );
    }

    T instance;
    try
    {
      instance = section.Get<T>() ?? new T();
      section.Bind(instance);
    }
    catch (Exception ex)
    {
      return Data.Validation.ValidationResult.Failure(
        catalogKey: _sectionPath,
        errorType: Data.Validation.ValidationErrorType.InvalidFormat,
        message: $"Configuration section '{_sectionPath}' could not be bound to '{typeof(T).Name}'.",
        details: ex.Message
      );
    }

    // DataAnnotations validation — same checks as the old GetValidated() helper
    var validationContext = new ValidationContext(instance);
    var validationResults = new List<ValidationResult>();
    if (
      !Validator.TryValidateObject(
        instance,
        validationContext,
        validationResults,
        validateAllProperties: true
      )
    )
    {
      var details = string.Join(
        Environment.NewLine,
        validationResults.Select(r => $"  - {r.ErrorMessage}")
      );
      return Data.Validation.ValidationResult.Failure(
        catalogKey: _sectionPath,
        errorType: Data.Validation.ValidationErrorType.SchemaMismatch,
        message: $"Configuration section '{_sectionPath}' failed DataAnnotations validation.",
        details: details
      );
    }

    return Data.Validation.ValidationResult.Success();
  }

  /// <inheritdoc/>
  /// <remarks>Configuration adapters are read-only — no write destination to validate.</remarks>
  public FlowIO<Data.Validation.ValidationResult> InspectTarget() =>
    FlowIO.Pure(Data.Validation.ValidationResult.Success());
}
