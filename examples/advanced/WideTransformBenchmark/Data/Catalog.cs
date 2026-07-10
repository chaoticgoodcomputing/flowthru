using Flowthru.Data.Catalog;

namespace WideTransformBenchmark.Data;

/// <summary>
/// Catalog for the benchmark's self-analysis: the measurement CSV the harness
/// stages, the checked-in report template, and the Analyze Flow's two
/// deliverables (summary CSV and Markdown report). The per-size benchmark
/// endpoints live on <see cref="SizedBenchmarkCatalog"/> — one instance per
/// fabricated dataset size, following the shard-catalog pattern from
/// RetailDataSplitFlow.
/// </summary>
public partial class Catalog : CatalogAbstract
{
  private readonly string _basePath;

  public Catalog(string basePath)
  {
    _basePath = basePath ?? throw new ArgumentNullException(nameof(basePath));
  }

  /// <summary>Root of the <c>Data/</c> tree this catalog binds under (used by the harness).</summary>
  public string DataPath => _basePath;
}
