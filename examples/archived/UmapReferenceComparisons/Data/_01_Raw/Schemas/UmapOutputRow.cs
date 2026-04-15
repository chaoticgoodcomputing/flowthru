using Flowthru.Core.Abstractions;

namespace UmapReferenceComparisons.Data._01_Raw.Schemas;

/// <summary>
/// Output row for UMAP dimensionality reduction results.
/// </summary>
/// <remarks>
/// All UMAP reference outputs use 2D embeddings with component_0 and component_1,
/// plus the original class label for validation and visualization.
/// </remarks>
public record UmapOutputRow
  : IFlatSchema,
    IBinarySerializable,
    IStructuredSerializable,
    ITextSerializable
{
  /// <summary>
  /// Unique observation identifier (GUID) matching the input data.
  /// </summary>
  [SerializedLabel("id")]
  public string Id { get; init; } = null!;

  /// <summary>
  /// First UMAP embedding component.
  /// </summary>
  [SerializedLabel("component_0")]
  public float Component0 { get; init; }

  /// <summary>
  /// Second UMAP embedding component.
  /// </summary>
  [SerializedLabel("component_1")]
  public float Component1 { get; init; }

  /// <summary>
  /// Original class label for validation.
  /// </summary>
  [SerializedLabel("label")]
  public int Label { get; init; }
}
