namespace Flowthru.Step.Python;

/// <summary>
/// Declarative form of <see cref="PythonPackageRequirement"/> — attach
/// this attribute to a class (typically an
/// <see cref="IPythonCapability"/> implementation or an
/// <see cref="IPythonLauncher"/> implementation) to declare one
/// Python-side package the type's behaviour depends on. The attribute
/// is the source the
/// <c>PythonRequirementsAnalyzer</c> (FTPY1501 / FTPY1502) reads
/// statically; the same declaration can also be returned from
/// <see cref="IPythonCapability.Requirements"/> /
/// <see cref="IPythonLauncher.Requirements"/> for pre-flight
/// enforcement. Per ADR-0013.
/// </summary>
/// <remarks>
/// <para>
/// Why an attribute rather than just the runtime property? The
/// analyzer runs at compile-time and cannot invoke arbitrary code on
/// the consumer's types. An attribute is metadata Roslyn reads
/// directly — design-time enforcement falls out for free.
/// </para>
/// <para>
/// The attribute and the runtime <c>Requirements</c> property are
/// independent declarations in slice 3; keep them in sync manually
/// (or implement <see cref="IPythonCapability.Requirements"/> to read
/// attributes via reflection if you prefer a single source of truth).
/// A drift test in
/// <c>tests/extensions/Flowthru.Extensions.Python.Tests</c> pins
/// <see cref="Flowthru.Step.Python.Internal.BasePythonExtensionCapability"/>'s
/// own attribute/property pair so the framework's base capability
/// cannot drift.
/// </para>
/// <para>
/// Targets classes (and structs) and allows multiple, so a launcher
/// or capability can declare every package it needs by stacking the
/// attribute:
/// </para>
/// <example>
/// <code>
/// [PythonPackageRequirement("accelerate", "&gt;=0.30", "Required by AccelerateLauncher")]
/// [PythonPackageRequirement("torch", "&gt;=2.0", "Required by AccelerateLauncher (DDP backend)")]
/// public sealed class AccelerateLauncher : IPythonLauncher { ... }
/// </code>
/// </example>
/// </remarks>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = true, Inherited = false)]
public sealed class PythonPackageRequirementAttribute : Attribute
{
  /// <summary>
  /// PyPI package name (e.g. <c>"accelerate"</c>, <c>"pyarrow"</c>).
  /// </summary>
  public string Package { get; }

  /// <summary>
  /// Optional PEP 440 version constraint string
  /// (e.g. <c>"&gt;=0.30"</c>, <c>"&gt;=14,&lt;16"</c>). Null declares
  /// "any version".
  /// </summary>
  public string? VersionConstraint { get; }

  /// <summary>
  /// Human-readable provenance for diagnostic messages
  /// (e.g. <c>"Required by AccelerateLauncher"</c>). Surfaced verbatim
  /// in FTPY1501 / FTPY1502 / FTPY3011 / FTPY3012 messages so users
  /// can identify the capability driving the requirement.
  /// </summary>
  public string Reason { get; }

  /// <summary>
  /// Declare a Python-side package requirement.
  /// </summary>
  /// <param name="package">PyPI package name. Required.</param>
  /// <param name="versionConstraint">
  /// PEP 440 constraint string, or <c>null</c> for "any version".
  /// </param>
  /// <param name="reason">
  /// Human-readable provenance. Surfaced in error messages.
  /// </param>
  public PythonPackageRequirementAttribute(
    string package,
    string? versionConstraint,
    string reason
  )
  {
    if (string.IsNullOrWhiteSpace(package))
      throw new ArgumentException("Package name cannot be null or whitespace.", nameof(package));
    if (string.IsNullOrWhiteSpace(reason))
      throw new ArgumentException("Reason cannot be null or whitespace.", nameof(reason));

    Package = package;
    VersionConstraint = versionConstraint;
    Reason = reason;
  }

  /// <summary>
  /// Convenience overload for "any version" requirements without
  /// having to pass a null constraint explicitly.
  /// </summary>
  public PythonPackageRequirementAttribute(string package, string reason)
    : this(package, null, reason) { }
}
