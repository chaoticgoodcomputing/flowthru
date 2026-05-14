using System.Collections.Concurrent;

namespace Flowthru.Step;

/// <summary>
/// Process-wide registry of <c>[FlowthruStep]</c> classes to their
/// source-generated <c>CodeVersion</c> identities. Populated at module
/// load time via <c>[ModuleInitializer]</c>-attributed companions the
/// <c>StepMetadataGenerator</c> emits alongside each step's
/// <c>_Metadata</c> record. Consumed by
/// <see cref="StepMetadataResolver"/> when
/// <c>FlowBuilder.AddStep</c> resolves a transform delegate's
/// enclosing step class.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Why a registry rather than reflection-on-the-attribute?</strong>
/// The <c>_Metadata.CodeVersion</c> constant is the authoritative
/// identity — it carries either the source generator's whitespace-
/// normalized SHA-256 prefix OR the user's explicit
/// <c>[FlowthruStep(CodeVersion = "...")]</c> override. The registry
/// stores that value verbatim; the runtime only needs to map a
/// <see cref="Type"/> to its recorded identity, no attribute parsing
/// at lookup time.
/// </para>
/// <para>
/// <strong>Thread safety.</strong> Backed by
/// <see cref="ConcurrentDictionary{TKey, TValue}"/>; registrations
/// from any number of module initializers may race without supervision.
/// </para>
/// <para>
/// <strong>Extension authors.</strong> Step extensions that own their
/// own <c>Add{Kind}Step</c> overloads (e.g., the Python extension's
/// <c>AddPythonStep</c>) typically resolve <c>CodeVersion</c> through
/// extension-specific mechanisms (.py source hashing, etc.) and don't
/// touch this registry. The registry is a C#-source-step convenience —
/// it ties the C# type system to the cache plan without requiring the
/// flow author to thread the constant by hand.
/// </para>
/// </remarks>
public static class StepMetadataRegistry
{
  private static readonly ConcurrentDictionary<Type, string> _versionsByType = new();

  /// <summary>
  /// Register a (<paramref name="stepType"/>, <paramref name="codeVersion"/>) pair.
  /// Subsequent calls for the same type overwrite — the source generator
  /// emits the registration once per step class, but defensive overwriting
  /// keeps the contract simple if multiple assemblies independently
  /// register the same type (test fixtures, etc.).
  /// </summary>
  public static void Register(Type stepType, string codeVersion)
  {
    if (stepType is null) throw new ArgumentNullException(nameof(stepType));
    if (codeVersion is null) throw new ArgumentNullException(nameof(codeVersion));
    _versionsByType[stepType] = codeVersion;
  }

  /// <summary>
  /// Look up the recorded <c>CodeVersion</c> for <paramref name="stepType"/>.
  /// Returns null when the type was never registered — the caller
  /// (typically <see cref="StepMetadataResolver"/>) treats that as
  /// "not a known step" and continues walking up the nested-type chain.
  /// </summary>
  public static string? TryGet(Type stepType)
  {
    if (stepType is null) throw new ArgumentNullException(nameof(stepType));
    return _versionsByType.TryGetValue(stepType, out var version) ? version : null;
  }

  /// <summary>
  /// True iff <paramref name="stepType"/> has a recorded version.
  /// Convenience over <see cref="TryGet"/> for callers that only need
  /// the presence bit.
  /// </summary>
  public static bool Contains(Type stepType) => TryGet(stepType) is not null;
}
