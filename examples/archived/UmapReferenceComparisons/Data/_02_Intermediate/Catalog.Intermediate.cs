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
  public IItem<IEnumerable<UmapInput>> IrisUmapInput =>
    CreateItem(() => Items.Enumerable.Memory<UmapInput>(label: "IrisUmapInput"));

  /// <summary>
  /// Digits dataset in universal UMAP input format (in-memory).
  /// </summary>
  public IItem<IEnumerable<UmapInput>> DigitsUmapInput =>
    CreateItem(() => Items.Enumerable.Memory<UmapInput>(label: "DigitsUmapInput"));

  /// <summary>
  /// MNIST dataset in universal UMAP input format (in-memory).
  /// </summary>
  public IItem<IEnumerable<UmapInput>> MnistUmapInput =>
    CreateItem(() => Items.Enumerable.Memory<UmapInput>(label: "MnistUmapInput"));

  /// <summary>
  /// Fashion-MNIST dataset in universal UMAP input format (in-memory).
  /// </summary>
  public IItem<IEnumerable<UmapInput>> FashionMnistUmapInput =>
    CreateItem(() => Items.Enumerable.Memory<UmapInput>(label: "FashionMnistUmapInput"));
}
