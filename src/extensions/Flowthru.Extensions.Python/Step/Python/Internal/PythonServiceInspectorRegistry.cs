using Flowthru.Step.Python;
using Microsoft.Extensions.Options;

namespace Flowthru.Step.Python.Internal;

/// <summary>
/// Default <see cref="IPythonServiceInspectorRegistry"/> implementation. Reads
/// registrations from the configured <see cref="PythonRuntimeOptions"/> and
/// exposes them by class path. Registered as a DI singleton in
/// <c>FlowthruServiceBuilderExtensions.UsePython</c>.
/// </summary>
internal sealed class PythonServiceInspectorRegistry : IPythonServiceInspectorRegistry
{
  private readonly Dictionary<string, PythonServiceRegistration> _byPath;

  public PythonServiceInspectorRegistry(IOptions<PythonRuntimeOptions> options)
  {
    if (options is null)
    {
      throw new ArgumentNullException(nameof(options));
    }

    // Snapshot at construction. The options object's ServiceRegistrations
    // dictionary is populated during PostConfigure (i.e. on first IOptions
    // resolution), and this registry is itself resolved as a singleton —
    // so the snapshot reflects the user's complete UsePython lambda.
    var registrations = options.Value.ServiceRegistrations;
    _byPath = new Dictionary<string, PythonServiceRegistration>(
      registrations,
      StringComparer.Ordinal
    );
  }

  /// <inheritdoc />
  public IReadOnlyCollection<PythonServiceRegistration> Registrations => _byPath.Values;

  /// <inheritdoc />
  public bool TryGet(
    string serviceClassPath,
    out PythonServiceRegistration? registration
  )
  {
    if (_byPath.TryGetValue(serviceClassPath, out var found))
    {
      registration = found;
      return true;
    }
    registration = null;
    return false;
  }
}
