using Flowthru.Extensions.Python.Runtime;

namespace Flowthru.Extensions.Python.Services;

/// <summary>
/// Lookup surface for Python service ↔ sidecar inspector registrations.
/// Populated by <see cref="Flowthru.Extensions.Python.Runtime.PythonRuntimeOptions.RegisterService(string, System.Action{PythonServiceBuilder})"/>
/// at options-configuration time; consumed by the preflight loop to dispatch
/// inspector calls for each declared Python service dependency.
/// </summary>
/// <remarks>
/// <para>
/// This interface is the Python analogue of looking up an
/// <see cref="Flowthru.Core.Effects.IFlowthruInspector{TService}"/>
/// registration on the C# side. Both paths feed the same preflight
/// aggregation loop in <c>Flow.RunAsync</c> after the
/// <c>ServiceRef</c> migration in core.
/// </para>
/// <para>
/// Lookups are by exact <see cref="PythonServiceRegistration.ServiceClassPath"/>
/// match against the fully-qualified class path captured from the Python
/// step's <c>@step(services=[...])</c> decorator. Unregistered class paths
/// return <see langword="false"/>; the preflight loop logs a warning in that
/// case (mirroring the C#-side warning when no <c>AddFlowthruInspect&lt;T&gt;</c>
/// is registered for a declared service).
/// </para>
/// </remarks>
public interface IPythonServiceInspectorRegistry
{
  /// <summary>
  /// Gets all known registrations. Used by the preflight loop to enumerate
  /// services across all flow steps.
  /// </summary>
  IReadOnlyCollection<PythonServiceRegistration> Registrations { get; }

  /// <summary>
  /// Attempts to resolve a registration by service class path.
  /// </summary>
  /// <param name="serviceClassPath">
  /// Fully-qualified Python class path (e.g. <c>"Services.PyannoteDiarizer"</c>).
  /// </param>
  /// <param name="registration">
  /// On success, the matching registration. <see langword="default"/> on miss.
  /// </param>
  /// <returns>
  /// <see langword="true"/> when a registration exists; <see langword="false"/> otherwise.
  /// </returns>
  bool TryGet(string serviceClassPath, out PythonServiceRegistration? registration);
}
