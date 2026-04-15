using Flowthru.Core.Data;

namespace DroppedNeuralNet.Data;

/// <summary>
/// Data catalog for the DroppedNeuralNet pipeline.
/// Tracks layer pieces through progressive enrichment: raw blobs → classified layers →
/// legal pairings → validated permutation solution.
/// </summary>
public partial class Catalog : CatalogAbstract
{
    private readonly string _basePath;

    public Catalog(string basePath)
    {
        _basePath = basePath;
        InitializeCatalogProperties();
    }
}
