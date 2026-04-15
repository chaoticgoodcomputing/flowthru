using Flowthru.Core.Data;

namespace KedroSpaceflights.Custom.Data;

/// <summary>
/// Data catalog for the Spaceflights project, providing compile-time type-safe access to datasets.
/// </summary>
/// <remarks>
/// <para>
/// This catalog follows Kedro's layered data engineering convention with numbered prefixes:
/// </para>
/// <list type="bullet">
/// <item>_01_Raw: Immutable source data from external sources</item>
/// <item>_02_Intermediate: Preprocessed/cleaned data</item>
/// <item>_03_Primary: Model input tables (training data)</item>
/// <item>_04_Models: Trained ML models</item>
/// <item>_05_ModelOutput: Model predictions and evaluation metrics</item>
/// <item>_06_Reporting: Visualizations and reports</item>
/// <item>_99_Reference: Reference data for validation</item>
/// </list>
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
    InitializeCatalogProperties();
  }
}
