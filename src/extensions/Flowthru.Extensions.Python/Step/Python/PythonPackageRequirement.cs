namespace Flowthru.Step.Python;

/// <summary>
/// One Python-side package requirement declared by a Python-extension
/// capability. Aggregated across all capabilities present in the DI
/// container at flow-construction time and enforced via the design-time
/// analyzer (FTPY1501) and pre-flight hook described in ADR-0013.
/// </summary>
/// <param name="Package">
/// PyPI package name (e.g. <c>"accelerate"</c>, <c>"pyarrow"</c>).
/// </param>
/// <param name="VersionConstraint">
/// Optional PEP 440 version constraint string (e.g. <c>"&gt;=0.30"</c>,
/// <c>"&gt;=14,&lt;16"</c>). <c>null</c> declares "any version".
/// </param>
/// <param name="Reason">
/// Human-readable provenance — surfaced in error messages when the
/// requirement is missing or conflicting (e.g.
/// <c>"Required by AccelerateLauncher"</c>). The reason is what makes a
/// conflict diagnostic actionable: it names *which* capability needs
/// the constraint.
/// </param>
/// <remarks>
/// <para>
/// The full requirements-algebra implementation — PEP 440 constraint
/// intersection, conflict detection, the analyzer (FTPY1501–FTPY15xx)
/// and the pre-flight hook — lands in Slice 2. This record is the
/// shared shape both sides consume; declaring it in Slice 1 lets
/// <see cref="IPythonLauncher.Requirements"/> reach its final form
/// without a breaking interface change later.
/// </para>
/// </remarks>
public sealed record PythonPackageRequirement(
  string Package,
  string? VersionConstraint,
  string Reason
);
