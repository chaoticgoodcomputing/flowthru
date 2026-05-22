using System.Collections.Concurrent;
using Flowthru.Validation.Runtime;

namespace Flowthru.Step;

/// <summary>
/// Process-wide registry of <c>[FlowthruStep]</c> classes to their
/// source-generated identity record — the <c>CodeVersion</c> string
/// and the array of declared <see cref="ServiceRef"/>s discovered from
/// the step's <c>Create</c>-overload parameters. Populated at module
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
/// stores that value verbatim alongside the discovered service refs;
/// the runtime only needs to map a <see cref="Type"/> to its recorded
/// identity, no attribute parsing at lookup time.
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
  /// <summary>
  /// Composite identity record stored per step class. Source-generator
  /// emissions populate both fields; legacy two-arg registrations get
  /// <see cref="Services"/> defaulted to the empty array.
  /// </summary>
  public sealed record Entry(string CodeVersion, IReadOnlyList<ServiceRef> Services);

  private static readonly ConcurrentDictionary<Type, Entry> _entriesByType = new();
  private static readonly IReadOnlyList<ServiceRef> _noServices = Array.Empty<ServiceRef>();

  /// <summary>
  /// Register a (<paramref name="stepType"/>, <paramref name="codeVersion"/>,
  /// <paramref name="services"/>) triple. Subsequent calls for the same
  /// type overwrite — the source generator emits the registration once
  /// per step class, but defensive overwriting keeps the contract simple
  /// if multiple assemblies independently register the same type (test
  /// fixtures, etc.). The <paramref name="services"/> parameter defaults
  /// to <c>null</c> (empty array) to preserve source compatibility with
  /// any caller still using the two-arg form.
  /// </summary>
  public static void Register(
    Type stepType,
    string codeVersion,
    IReadOnlyList<ServiceRef>? services = null)
  {
    if (stepType is null) throw new ArgumentNullException(nameof(stepType));
    if (codeVersion is null) throw new ArgumentNullException(nameof(codeVersion));
    _entriesByType[stepType] = new Entry(codeVersion, services ?? _noServices);
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
    return _entriesByType.TryGetValue(stepType, out var entry) ? entry.CodeVersion : null;
  }

  /// <summary>
  /// Look up the recorded service refs for <paramref name="stepType"/>.
  /// Returns the empty array when the type was never registered or has
  /// no declared services — symmetric with <see cref="TryGet"/>'s
  /// null-on-miss contract but on a non-nullable collection so callers
  /// can iterate without a null check.
  /// </summary>
  public static IReadOnlyList<ServiceRef> TryGetServices(Type stepType)
  {
    if (stepType is null) throw new ArgumentNullException(nameof(stepType));
    return _entriesByType.TryGetValue(stepType, out var entry) ? entry.Services : _noServices;
  }

  /// <summary>
  /// Look up the full <see cref="Entry"/> for <paramref name="stepType"/>.
  /// Returns null on miss. Convenience for callers needing both fields
  /// without two dictionary probes.
  /// </summary>
  public static Entry? TryGetEntry(Type stepType)
  {
    if (stepType is null) throw new ArgumentNullException(nameof(stepType));
    return _entriesByType.TryGetValue(stepType, out var entry) ? entry : null;
  }

  /// <summary>
  /// True iff <paramref name="stepType"/> has a recorded entry.
  /// Convenience over <see cref="TryGet"/> for callers that only need
  /// the presence bit.
  /// </summary>
  public static bool Contains(Type stepType) => TryGetEntry(stepType) is not null;
}
