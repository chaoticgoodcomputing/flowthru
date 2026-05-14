namespace Flowthru.Step;

/// <summary>
/// Maps a <see cref="Delegate"/> (typically the <c>transform:</c>
/// argument passed to <c>FlowBuilder.AddStep</c>) back to its enclosing
/// <c>[FlowthruStep]</c> class's recorded <c>CodeVersion</c> by walking
/// up the lambda's compiler-generated declaring chain.
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
/// ancestor with a recorded <c>CodeVersion</c> wins.
/// </para>
/// <para>
/// <strong>Returns null on miss.</strong> Inline anonymous lambdas, foreign-
/// assembly delegates, and classes without a <c>[FlowthruStep]</c>
/// attribute all return null — and a null <c>CodeVersion</c> makes the
/// consuming step uncacheable. Fail-safe: if we cannot identify the
/// step, we cannot certify a cached output is still correct, so we run.
/// </para>
/// </remarks>
public static class StepMetadataResolver
{
  /// <summary>
  /// Walk the delegate's declaring-type chain looking for a registered
  /// step type. Returns the recorded <c>CodeVersion</c> on the first
  /// match, or null if no ancestor type is in the registry.
  /// </summary>
  public static string? ResolveFromDelegate(Delegate? transform)
  {
    if (transform is null) return null;
    var declaringType = transform.Method.DeclaringType;
    while (declaringType is not null)
    {
      // Canonicalize closed generic types to their open generic definition
      // — the registry is keyed on typedef, so every instantiation of
      // SomeStep<T> shares one CodeVersion entry.
      var lookupType = declaringType.IsGenericType && !declaringType.IsGenericTypeDefinition
        ? declaringType.GetGenericTypeDefinition()
        : declaringType;
      if (StepMetadataRegistry.TryGet(lookupType) is { } version)
      {
        return version;
      }
      declaringType = declaringType.DeclaringType;
    }
    return null;
  }
}
