using Flowthru.Data.Catalog;
using SpaceflightsDuckDB.Data._08_Reporting.Schemas;

namespace SpaceflightsDuckDB.Data;

/// <summary>
/// Reporting data layer: Ad hoc descriptive cuts and summaries.
/// </summary>
public partial class Catalog
{
  /// <summary>Per-company summary rows aggregated from the model input table.</summary>
  public IItem<IEnumerable<CompanySummarySchema>> CompanySummaries =>
    CreateItem(() => Item.Of<IEnumerable<CompanySummarySchema>>("CompanySummaries")
      .Parquet()
      .AtPath($"{_basePath}/_08_Reporting/Datasets/company_summaries.parquet")
      .Build());

  /// <summary>Top-rated companies report.</summary>
  public IItem<IEnumerable<CompanyRatingReport>> CompanyRatingReport =>
    CreateItem(() => Item.Of<IEnumerable<CompanyRatingReport>>("CompanyRatingReport")
      .Json()
      .AtPath($"{_basePath}/_08_Reporting/Datasets/company_rating_report.json")
      .Build());
}
