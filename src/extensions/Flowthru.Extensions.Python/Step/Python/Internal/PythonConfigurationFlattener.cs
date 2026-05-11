using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace Flowthru.Step.Python.Internal;

/// <summary>
/// Default <see cref="IPythonConfigurationFlattener"/> implementation. Walks
/// the section identified by <see cref="PythonRuntimeOptions.ConfigurationSection"/>
/// recursively and emits one env-var entry per leaf, joining the path with
/// <c>__</c> per the .NET subprocess convention.
/// </summary>
internal sealed class PythonConfigurationFlattener : IPythonConfigurationFlattener
{
  private readonly IConfiguration _configuration;
  private readonly IOptions<PythonRuntimeOptions> _options;

  public PythonConfigurationFlattener(
    IConfiguration configuration,
    IOptions<PythonRuntimeOptions> options
  )
  {
    _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    _options = options ?? throw new ArgumentNullException(nameof(options));
  }

  /// <inheritdoc />
  public IReadOnlyDictionary<string, string> Flatten()
  {
    var sectionName = _options.Value.ConfigurationSection;
    if (string.IsNullOrWhiteSpace(sectionName))
    {
      return _emptyEnv;
    }

    var section = _configuration.GetSection(sectionName);
    if (!section.Exists())
    {
      return _emptyEnv;
    }

    var result = new Dictionary<string, string>(StringComparer.Ordinal);

    // The Python-side env-var prefix matches the section path so consumers
    // can scope by prefix (e.g., pydantic-settings env_prefix="Diarization__").
    // We pass the section name as the initial prefix, transformed using the
    // :→__ rule.
    var rootPrefix = sectionName.Replace(':', '_').Replace('_', '_'); // identity, but explicit
    rootPrefix = sectionName.Replace(":", "__");

    Walk(section, rootPrefix, result);
    return result;
  }

  /// <summary>
  /// Recursive walk: each child contributes either a leaf (Value != null) or
  /// a nested section. Composite keys are joined with <c>__</c>.
  /// </summary>
  /// <remarks>
  /// IConfiguration represents arrays as sections whose child keys are
  /// stringified non-negative integers ("0", "1", ...). The flattener does
  /// not need to special-case this — emitting <c>Foo__0=...</c> and
  /// <c>Foo__1=...</c> is exactly what the Python <c>flowthru.config</c>
  /// re-nester expects to materialize back into a list.
  /// </remarks>
  private static void Walk(
    IConfigurationSection section,
    string prefix,
    Dictionary<string, string> output
  )
  {
    foreach (var child in section.GetChildren())
    {
      var key = $"{prefix}__{child.Key}";
      if (child.Value is not null)
      {
        output[key] = child.Value;
      }
      else
      {
        Walk(child, key, output);
      }
    }
  }

  private static readonly IReadOnlyDictionary<string, string> _emptyEnv =
    new Dictionary<string, string>(StringComparer.Ordinal);
}
