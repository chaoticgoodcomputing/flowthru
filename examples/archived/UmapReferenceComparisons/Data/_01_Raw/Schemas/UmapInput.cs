using Flowthru.Core.Abstractions;

namespace UmapReferenceComparisons.Data._01_Raw.Schemas;

/// <summary>
/// Universal input schema for UMAP transformations.
/// </summary>
/// <remarks>
/// This schema provides a standardized format for any dataset being fed into UMAP,
/// regardless of the original feature structure. Dataset-specific schemas should
/// be converted to this format before UMAP transformation.
/// </remarks>
[FlowthruSchema]
public partial record UmapInput
{
    /// <summary>
    /// Unique observation identifier.
    /// </summary>
    [SerializedLabel("id")]
    public string Id { get; init; } = null!;

    /// <summary>
    /// Class label or category (for visualization/validation).
    /// </summary>
    [SerializedLabel("label")]
    public string Label { get; init; } = null!;

    /// <summary>
    /// Feature vector as array of floats.
    /// </summary>
    /// <remarks>
    /// This is the raw feature data that UMAP will transform.
    /// Dimensionality depends on the source dataset.
    /// </remarks>
    [SerializedLabel("features")]
    public float[] Features { get; init; } = null!;
}
