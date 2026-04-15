namespace Flowthru.Core.Graph;

/// <summary>
/// Base capability metadata for all DAG node types.
/// </summary>
/// <remarks>
/// <para>
/// Describes universal properties that apply to any node in the DAG — data items,
/// effects, or steps. Archetype-specific traits extend this record:
/// </para>
/// <list type="bullet">
/// <item><see cref="Flowthru.Core.Data.Capabilities.StorageTraits"/> for data I/O nodes</item>
/// <item><see cref="Flowthru.Core.Effects.EffectTraits"/> for side-effect nodes</item>
/// </list>
/// </remarks>
public record NodeTraits
{
    /// <summary>
    /// Whether this node requires network access to operate.
    /// </summary>
    public bool RequiresNetwork { get; init; } = false;

    /// <summary>
    /// Whether this node supports pre-flight inspection / validation.
    /// </summary>
    public bool CanInspect { get; init; } = true;
}
