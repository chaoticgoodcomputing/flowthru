using System.Reflection;
using Flowthru.Core.Effects;

namespace Flowthru.Core.Steps;

/// <summary>
/// Runtime resolver that locates source-generated <c>{StepClassName}_Metadata</c> sibling
/// types and extracts step capability metadata from them. Used by
/// <c>FlowBuilder.AddStep</c> at flow-construction time to populate
/// <see cref="Graph.FlowStep.ServiceDependencies"/> from compile-time emitted metadata.
/// </summary>
/// <remarks>
/// <para>
/// The resolver intentionally returns safe defaults (empty list / default
/// <see cref="StepTraits"/>) when metadata is absent. Three cases produce no metadata:
/// </para>
/// <list type="bullet">
/// <item><strong>Inline lambdas</strong> — the synthesized closure type has no sibling
///   <c>_Metadata</c>; the lambda itself declares no service dependencies.</item>
/// <item><strong>Method groups from un-attributed classes</strong> — separate
///   <c>FT4001</c> diagnostic warns about these at the AddStep call site.</item>
/// <item><strong>Generic / nested step classes</strong> — Phase 4 limitation; metadata
///   is only emitted for top-level concrete step classes.</item>
/// </list>
/// </remarks>
public static class StepMetadataResolver
{
  private const string MetadataTypeSuffix = "_Metadata";
  private const string ServiceDependenciesFieldName = "ServiceDependencies";
  private const string TraitsFieldName = "Traits";

  /// <summary>
  /// Looks up the service-dependency list for the step class declaring the given
  /// transform delegate. Returns an empty list when no metadata is found.
  /// </summary>
  public static IReadOnlyList<ServiceRef> GetServiceDependencies(Delegate transform)
  {
    if (transform is null)
    {
      return Array.Empty<ServiceRef>();
    }

    var metadataType = ResolveMetadataType(transform);
    if (metadataType is null)
    {
      return Array.Empty<ServiceRef>();
    }

    var field = metadataType.GetField(
      ServiceDependenciesFieldName,
      BindingFlags.Public | BindingFlags.Static
    );
    if (field?.GetValue(null) is IReadOnlyList<ServiceRef> refs)
    {
      return refs;
    }

    return Array.Empty<ServiceRef>();
  }

  /// <summary>
  /// Looks up the <see cref="StepTraits"/> for the step class declaring the given
  /// transform delegate. Returns the default (all flags false) when no metadata is found.
  /// </summary>
  public static StepTraits GetTraits(Delegate transform)
  {
    if (transform is null)
    {
      return default;
    }

    var metadataType = ResolveMetadataType(transform);
    if (metadataType is null)
    {
      return default;
    }

    var field = metadataType.GetField(
      TraitsFieldName,
      BindingFlags.Public | BindingFlags.Static
    );
    if (field?.GetValue(null) is StepTraits traits)
    {
      return traits;
    }

    return default;
  }

  private static Type? ResolveMetadataType(Delegate transform)
  {
    // The transform is typically a lambda returned from Step.Create(), so
    // transform.Method.DeclaringType points at the compiler-synthesized closure
    // class nested inside the step (e.g., 'MyStep+<>c__DisplayClass0_0'). Walk up
    // the declaring-type chain trying each level for a sibling _Metadata type;
    // this finds metadata both for method-group transforms (level 0) and for
    // closure-wrapped lambdas (level 1+).
    var current = transform.Method.DeclaringType;
    while (current is not null)
    {
      var metadataTypeName = current.FullName + MetadataTypeSuffix;
      var metadataType = current.Assembly.GetType(metadataTypeName);
      if (metadataType is not null)
      {
        return metadataType;
      }
      current = current.DeclaringType;
    }
    return null;
  }
}
