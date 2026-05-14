using Flowthru.Data.Catalog;
using Flowthru.Data.Catalog.Configuration;
using FlowthruCoverage.Flows.Reporting.Steps;
using Microsoft.Extensions.Configuration;

namespace FlowthruCoverage.Data;

public partial class Catalog : CatalogAbstract
{
  private readonly string _basePath;
  private readonly IConfiguration _configuration;

  public Catalog(string basePath, IConfiguration configuration)
  {
    _basePath = basePath;
    _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
  }

  /// <summary>
  /// Unit-coverage report options sourced from
  /// <c>Flowthru:Flows:Reporting:UnitCoverageReportOptions</c>.
  /// Changing the threshold in <c>appsettings.json</c> invalidates the
  /// BuildUnitCoverageReport step's cached output automatically.
  /// </summary>
  public IItem<BuildUnitCoverageReportStep.Options> UnitCoverageReportOptions =>
    CreateItem(() =>
      Item.Of<BuildUnitCoverageReportStep.Options>("UnitCoverageReportOptions")
        .FromConfiguration(_configuration)
        .AtSection("Flowthru:Flows:Reporting:UnitCoverageReportOptions")
        .Build());
}
