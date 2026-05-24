namespace Flowthru.Step.Python;

/// <summary>
/// Marker interface for any Python-extension capability that declares
/// Python-side package requirements. Folded into a single requirements
/// closure at flow-construction time by
/// <see cref="Flowthru.Validation.PreFlight.Python.PythonRequirementsValidationHook"/>,
/// per the requirements algebra in ADR-0013.
/// </summary>
/// <remarks>
/// <para>
/// Concrete declarers in v1:
/// <list type="bullet">
///   <item>
///     <c>BasePythonExtensionCapability</c> — the floor of what the
///     Python extension itself needs in the venv (pyarrow for Arrow
///     IPC, the <c>flowthru</c> Python companion package).
///   </item>
///   <item>
///     <see cref="IPythonLauncher"/> implementations — declare via
///     <see cref="IPythonLauncher.Requirements"/>. Launchers do NOT
///     implement <see cref="IPythonCapability"/> directly so bespoke
///     user launchers stay minimal (the hook fetches the active
///     launcher separately and reads its <c>Requirements</c>).
///   </item>
///   <item>
///     <c>PythonServiceRegistration</c> — future. Service inspectors
///     that depend on a specific Python library can declare it on
///     registration; the algebra absorbs the same way.
///   </item>
/// </list>
/// </para>
/// <para>
/// Custom <see cref="IPythonCapability"/> implementations register
/// themselves as additional services in the user's host setup
/// (<c>services.AddSingleton&lt;IPythonCapability, MyCorpCapability&gt;()</c>)
/// — the hook resolves <c>IEnumerable&lt;IPythonCapability&gt;</c> and
/// folds every declared requirement into the closure.
/// </para>
/// </remarks>
public interface IPythonCapability
{
  /// <summary>
  /// Python-side packages this capability declares as required in the
  /// venv. Empty list is allowed (declares nothing); the algebra
  /// treats empty as the identity element.
  /// </summary>
  IReadOnlyList<PythonPackageRequirement> Requirements { get; }
}
