namespace Flowthru.Extensions.Python.Runtime;

/// <summary>
/// Flattens a configured slice of <see cref="Microsoft.Extensions.Configuration.IConfiguration"/>
/// into environment-variable form for injection into the Python subprocess.
/// </summary>
/// <remarks>
/// <para>
/// Mirrors .NET's native subprocess-config convention: the configuration key
/// path's <c>:</c> separator is replaced with <c>__</c> (double underscore)
/// to produce the env-var name. ASP.NET Core, Azure App Service, and every
/// Docker-deployed .NET app already use this rule when shipping config to
/// child processes; the Flowthru Python extension reuses it so Python-side
/// consumers (pydantic-settings, <c>flowthru.config</c>, plain
/// <c>os.environ</c>) all see config values via the path they expect.
/// </para>
/// <para>
/// The flattened set covers only the section named in
/// <see cref="PythonRuntimeOptions.ConfigurationSection"/>. An empty or
/// unset section name returns an empty dictionary — the bridge is opt-in.
/// </para>
/// <para>
/// .NET array semantics (a section whose children have sequential integer
/// keys binds to <c>List&lt;T&gt;</c>) round-trip naturally: the flattener
/// emits <c>Section__0</c>, <c>Section__1</c>, etc., and
/// <c>flowthru.config</c>'s re-nesting reconstructs the list on the Python
/// side. No special-case handling is required here.
/// </para>
/// </remarks>
public interface IPythonConfigurationFlattener
{
  /// <summary>
  /// Produces the env-var pairs that should be set on the Python
  /// subprocess's <see cref="System.Diagnostics.ProcessStartInfo.EnvironmentVariables"/>
  /// before <c>Process.Start</c>.
  /// </summary>
  /// <returns>
  /// A read-only dictionary of <c>Section__Subsection__Key</c> → string
  /// value. Empty when no <see cref="PythonRuntimeOptions.ConfigurationSection"/>
  /// is configured. Values are stringified using the same rules
  /// <c>IConfiguration</c> uses (its values are already strings).
  /// </returns>
  IReadOnlyDictionary<string, string> Flatten();
}
