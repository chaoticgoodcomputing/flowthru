using Flowthru.Data;
using UmapReferenceComparisons.Data._01_Raw.Schemas;

namespace UmapReferenceComparisons.Data;

public partial class Catalog
{
  // ============================================================================
  // Intermediate - In-memory transformations and universal UMAP inputs
  // ============================================================================

  /// <summary>
  /// Iris dataset in universal UMAP input format (in-memory).
  /// </summary>
  public ICatalogEntry<IEnumerable<UmapInput>> IrisUmapInput =>
    GetOrCreateEntry(() => CatalogEntries.Enumerable.Memory<UmapInput>(label: "IrisUmapInput"));

  /// <summary>
  /// Digits dataset in universal UMAP input format (in-memory).
  /// </summary>
  public ICatalogEntry<IEnumerable<UmapInput>> DigitsUmapInput =>
    GetOrCreateEntry(() => CatalogEntries.Enumerable.Memory<UmapInput>(label: "DigitsUmapInput"));

  /// <summary>
  /// MNIST dataset in universal UMAP input format (in-memory).
  /// </summary>
  public ICatalogEntry<IEnumerable<UmapInput>> MnistUmapInput =>
    GetOrCreateEntry(() => CatalogEntries.Enumerable.Memory<UmapInput>(label: "MnistUmapInput"));

  /// <summary>
  /// Fashion-MNIST dataset in universal UMAP input format (in-memory).
  /// </summary>
  public ICatalogEntry<IEnumerable<UmapInput>> FashionMnistUmapInput =>
    GetOrCreateEntry(
      () => CatalogEntries.Enumerable.Memory<UmapInput>(label: "FashionMnistUmapInput")
    );
}
