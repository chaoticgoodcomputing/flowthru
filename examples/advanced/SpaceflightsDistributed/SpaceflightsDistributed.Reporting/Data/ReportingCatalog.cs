using Flowthru.Data;

namespace SpaceflightsDistributed.Reporting.Data;

/// <summary>
/// Data catalog for the Reporting pipeline library.
/// Owns reporting outputs — capacity reports and visualization charts.
/// </summary>
public partial class ReportingCatalog : CatalogAbstract
{
  private readonly string _basePath;

  public ReportingCatalog(string basePath)
  {
    _basePath = basePath;
    InitializeCatalogProperties();
  }
}
