using Flowthru.Abstractions;

namespace UmapReferenceComparisons.Data._02_ModelOutputs.Schemas;

/// <summary>
/// UMAP embedding output with 2 components.
/// </summary>
/// <remarks>
/// This schema represents the output of UMAP dimensionality reduction
/// to 2 dimensions. Used for both Python reference embeddings and
/// C# UMAP output embeddings.
/// </remarks>
public record UmapEmbedding2D : IFlatSchema, IBinarySerializable, IStructuredSerializable
{
  /// <summary>
  /// First component of the UMAP embedding.
  /// </summary>
  [SerializedLabel("component_0")]
  public float Component0 { get; init; }

  /// <summary>
  /// Second component of the UMAP embedding.
  /// </summary>
  [SerializedLabel("component_1")]
  public float Component1 { get; init; }
}
