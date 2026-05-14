using Flowthru.Data.Catalog;
using Flowthru.Data.Catalog.Configuration;
using KedroSpaceflightsCustom.Flows.DataEvaluation.Steps;
using KedroSpaceflightsCustom.Flows.DataScience.Steps;
using Microsoft.Extensions.Configuration;

namespace KedroSpaceflightsCustom.Data;

/// <summary>
/// Data catalog for the Spaceflights project, providing compile-time type-safe access to datasets
/// plus configuration-bound option records that flow into steps as ordinary inputs.
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
  private readonly IConfiguration _configuration;

  /// <summary>
  /// Initializes a new instance of the <see cref="Catalog"/> class.
  /// </summary>
  /// <param name="basePath">The base path for data storage.</param>
  /// <param name="configuration">
  /// The host's <see cref="IConfiguration"/>, sourced from <c>appsettings.json</c>.
  /// Option records are bound from the <c>Flowthru:Flows:*</c> sections via
  /// <see cref="ConfigurationItem{T}"/> so a change in the file invalidates the
  /// affected downstream cache automatically.
  /// </param>
  public Catalog(string basePath, IConfiguration configuration)
  {
    _basePath = basePath;
    _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
  }

  /// <summary>Train/test split options sourced from <c>Flowthru:Flows:DataScience:ModelParams</c>.</summary>
  public IItem<CreateTestTrainSplitStep.TestTrainSplitParams> ModelParams =>
    CreateItem(() =>
      Item.Of<CreateTestTrainSplitStep.TestTrainSplitParams>("ModelParams")
        .FromConfiguration(_configuration)
        .AtSection("Flowthru:Flows:DataScience:ModelParams")
        .Build());

  /// <summary>Cross-validation options sourced from <c>Flowthru:Flows:DataEvaluation:CrossValidationParams</c>.</summary>
  public IItem<CrossValidateModelStep.Params> CrossValidationParams =>
    CreateItem(() =>
      Item.Of<CrossValidateModelStep.Params>("CrossValidationParams")
        .FromConfiguration(_configuration)
        .AtSection("Flowthru:Flows:DataEvaluation:CrossValidationParams")
        .Build());
}
