using Flowthru.Core.Data;
using Flowthru.Extensions.GQL.Data;
using KedroSpaceflightsGQL.Data._01_Raw.Schemas;
using KedroSpaceflightsGQL.Infra.GqlClient;

namespace KedroSpaceflightsGQL.Data;

public partial class Catalog
{
  // ── Seed entries (CSV / Excel) — consumed by the Ingest flow ────────────

  /// <summary>
  /// Raw company data read from CSV. Used by the Ingest flow to seed the GQL server.
  /// </summary>
  public IItem<IEnumerable<CompanySchema>> SeedCompanies =>
    CreateItem(
      () =>
        ItemFactory.Enumerable.Csv<CompanySchema>(
          label: "SeedCompanies",
          filePath: $"{_basePath}/_01_Raw/Datasets/companies.csv"
        )
    );

  /// <summary>
  /// Raw review data read from CSV. Used by the Ingest flow to seed the GQL server.
  /// </summary>
  public IItem<IEnumerable<ReviewSchema>> SeedReviews =>
    CreateItem(
      () =>
        ItemFactory.Enumerable.Csv<ReviewSchema>(
          label: "SeedReviews",
          filePath: $"{_basePath}/_01_Raw/Datasets/reviews.csv"
        )
    );

  /// <summary>
  /// Raw shuttle data read from Excel. Used by the Ingest flow to seed the GQL server.
  /// </summary>
  public IItem<IEnumerable<ShuttleSchema>> SeedShuttles =>
    CreateItem(
      () =>
        ItemFactory.Enumerable.Excel<ShuttleSchema>(
          label: "SeedShuttles",
          filePath: $"{_basePath}/_01_Raw/Datasets/shuttles.xlsx",
          sheetName: "Sheet1"
        )
    );

  // /// <summary>
  // /// In-memory flag written by the Ingest flow after all mutations succeed.
  // /// Downstream flows depend on this via the DAG.
  // /// </summary>
  public IItem<bool> GqlDatabaseSeeded =>
    CreateItem(() => ItemFactory.Single.Memory<bool>("GqlDatabaseSeeded"));

  // ── Raw GQL entries — consumed by the DataProcessing flow ───────────────

  /// <summary>
  /// Raw company data queried from the GQL server.
  /// </summary>
  public IItem<IEnumerable<IGetCompanies_Companies>> Companies =>
    CreateItem(
      () =>
        GqlItemFactory.Enumerable.Query<IGetCompaniesResult, IGetCompanies_Companies>(
          label: "Companies",
          queryFunc: ct => _client.GetCompanies.ExecuteAsync(ct),
          selectData: r => r.Companies,
          allowEmptyData: true
        )
    );

  /// <summary>
  /// Raw shuttle data queried from the GQL server.
  /// </summary>
  public IItem<IEnumerable<IGetShuttles_Shuttles>> Shuttles =>
    CreateItem(
      () =>
        GqlItemFactory.Enumerable.Query<IGetShuttlesResult, IGetShuttles_Shuttles>(
          label: "Shuttles",
          queryFunc: ct => _client.GetShuttles.ExecuteAsync(ct),
          selectData: r => r.Shuttles,
          allowEmptyData: true
        )
    );

  /// <summary>
  /// Raw review data queried from the GQL server.
  /// </summary>
  public IItem<IEnumerable<IGetReviews_Reviews>> Reviews =>
    CreateItem(
      () =>
        GqlItemFactory.Enumerable.Query<IGetReviewsResult, IGetReviews_Reviews>(
          label: "Reviews",
          queryFunc: ct => _client.GetReviews.ExecuteAsync(ct),
          selectData: r => r.Reviews,
          allowEmptyData: true
        )
    );
}
