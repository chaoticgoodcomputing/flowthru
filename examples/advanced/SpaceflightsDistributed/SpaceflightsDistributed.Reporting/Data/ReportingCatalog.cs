using Flowthru.Data.Catalog;
using Flowthru.Data.Catalog.Configuration;
using Microsoft.Extensions.Configuration;
using SpaceflightsDistributed.Reporting.Flows.Reporting.Steps;

namespace SpaceflightsDistributed.Reporting.Data;

/// <summary>
/// Data catalog for the Reporting pipeline library.
/// Owns reporting outputs — capacity reports and visualization charts —
/// plus configuration-bound option records that flow into steps as
/// ordinary inputs.
/// </summary>
public partial class ReportingCatalog : CatalogAbstract
{
  private readonly string _basePath;
  private readonly IConfiguration _configuration;

  public ReportingCatalog(string basePath, IConfiguration configuration)
  {
    _basePath = basePath;
    _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
  }

  /// <summary>Confusion-matrix options sourced from <c>Flowthru:Flows:Reporting:ConfusionMatrixOptions</c>.</summary>
  public IItem<CreateConfusionMatrixStep.Options> ConfusionMatrixOptions =>
    CreateItem(() =>
      Item.Of<CreateConfusionMatrixStep.Options>("ConfusionMatrixOptions")
        .FromConfiguration(_configuration)
        .AtSection("Flowthru:Flows:Reporting:ConfusionMatrixOptions")
        .Build());
}
