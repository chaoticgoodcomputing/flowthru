using Flowthru.Data.Catalog;

namespace SpaceflightsDuckDB.Data;

/// <summary>
/// Data catalog for the Spaceflights DuckDB pipeline, providing access to datasets
/// across the data categories the project uses.
/// </summary>
/// <remarks>
/// The Items the engine-side SQL steps read and write are ordinary Parquet Items —
/// nothing on the Catalog is DuckDB-specific. Swapping a step between C# and SQL
/// never touches this class.
/// </remarks>
public partial class Catalog : CatalogAbstract
{
  private readonly string _basePath;

  /// <summary>
  /// Initializes a new instance of the <see cref="Catalog"/> class.
  /// </summary>
  /// <param name="basePath">The base path for data storage.</param>
  public Catalog(string basePath)
  {
    _basePath = basePath;
  }
}
