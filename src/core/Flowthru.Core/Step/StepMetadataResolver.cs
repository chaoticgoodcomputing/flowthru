using Flowthru.Validation.Runtime;

namespace Flowthru.Step;

/// <summary>
/// Maps a <see cref="Delegate"/> (typically the <c>transform:</c>
/// argument passed to <c>FlowBuilder.AddStep</c>) back to its enclosing
/// <c>[FlowthruStep]</c> class's recorded identity — <c>CodeVersion</c>
/// for the cache plan and the declared <see cref="ServiceDependency"/> list
/// for the engine — by walking up the lambda's compiler-generated
/// declaring chain.
/// </summary>
/// <remarks>
/// <para>
/// Non-capturing lambdas inside <c>SomeStep.Create()</c> typically
/// compile to a method on either <c>SomeStep</c> itself or a hidden
/// <c>SomeStep.&lt;&gt;c</c> companion. Capturing lambdas live on a
/// nested <c>SomeStep.&lt;&gt;c__DisplayClassN</c>. In every case the
/// step class is some ancestor of <c>transform.Method.DeclaringType</c>;
/// this resolver walks <see cref="Type.DeclaringType"/> upward,
/// probing <see cref="StepMetadataRegistry"/> at each level. The first
/// ancestor with a recorded entry wins.
/// </para>
/// <para>
/// <strong>Returns null / empty on miss.</strong> Inline anonymous
/// lambdas, foreign-assembly delegates, and classes without a
/// <c>[FlowthruStep]</c> attribute all return null
/// (<see cref="ResolveFromDelegate"/>) or the empty array
/// (<see cref="ResolveServicesFromDelegate"/>). A null
/// <c>CodeVersion</c> makes the consuming step uncacheable; an empty
/// service list makes the engine skip pre-flight service probes. Both
/// fail-safe: if we cannot identify the step, we cannot certify a
/// cached output is still correct (so we run), and we cannot enumerate
/// its services (so the host's DI container backstops resolution at
/// step-execution time as before).
/// </para>
/// </remarks>
public static class StepMetadataResolver
{
  private static readonly IReadOnlyList<ServiceDependency> _noServices = Array.Empty<ServiceDependency>();

  /// <summary>
  /// Walk the delegate's declaring-type chain looking for a registered
  /// step type. Returns the recorded <c>CodeVersion</c> on the first
  /// match, or null if no ancestor type is in the registry.
  /// </summary>
  public static string? ResolveFromDelegate(Delegate? transform) =>
    WalkForEntry(transform)?.CodeVersion;

  /// <summary>
  /// Walk the delegate's declaring-type chain looking for a registered
  /// step type. Returns the recorded <see cref="ServiceDependency"/> list on
  /// the first match, or the empty array if no ancestor type is in the
  /// registry.
  /// </summary>
  public static IReadOnlyList<ServiceDependency> ResolveServicesFromDelegate(Delegate? transform) =>
    WalkForEntry(transform)?.Services ?? _noServices;

  private static StepMetadataRegistry.Entry? WalkForEntry(Delegate? transform)
  {
    if (transform is null) return null;
    var declaringType = transform.Method.DeclaringType;
    while (declaringType is not null)
    {
      // Canonicalize closed generic types to their open generic definition
      // — the registry is keyed on typedef, so every instantiation of
      // SomeStep<T> shares one entry.
      var lookupType = declaringType.IsGenericType && !declaringType.IsGenericTypeDefinition
        ? declaringType.GetGenericTypeDefinition()
        : declaringType;
      if (StepMetadataRegistry.TryGetEntry(lookupType) is { } entry)
      {
        return entry;
      }
      declaringType = declaringType.DeclaringType;
    }
    return null;
  }
}
