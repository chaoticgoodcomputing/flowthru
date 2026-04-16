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

  /// <summary>
  /// In-memory flag written by the Ingest flow after all mutations succeed.
  /// Downstream flows depend on this via the DAG.
  /// </summary>
  public IItem<bool> GqlDatabaseSeeded =>
    CreateItem(() => ItemFactory.Single.Memory<bool>("GqlDatabaseSeeded"));

  // ── Raw GQL entries — consumed by the DataProcessing flow ───────────────
  //
  // These are DEFERRED query handles, not materialized collections. No network I/O happens
  // when the catalog is constructed or during pre-flight (beyond a lightweight connectivity
  // probe). The step that consumes these entries calls .ToList() to trigger the actual
  // GQL fetch — see CreateModelInputTableStep for the materialization point.

  /// <summary>
  /// Deferred GQL query handle for company data. The consuming step materializes this
  /// via <c>ToList()</c>, which fires the <c>GetCompanies</c> query against the server.
  /// </summary>
  public IItem<GqlQuery<IGetCompaniesResult, IGetCompanies_Companies>> Companies =>
    CreateItem(
      () =>
        GqlItemFactory.Query.NonPaged<IGetCompaniesResult, IGetCompanies_Companies>(
          label: "GQLCompanies",
          queryFunc: ct => _client.GetCompanies.ExecuteAsync(ct),
          selectData: r => r.Companies,
          allowEmptyData: true
        )
    );

  /// <summary>
  /// Deferred GQL query handle for shuttle data. The consuming step materializes this
  /// via <c>ToList()</c>, which fires the <c>GetShuttles</c> query against the server.
  /// </summary>
  public IItem<GqlQuery<IGetShuttlesResult, IGetShuttles_Shuttles>> Shuttles =>
    CreateItem(
      () =>
        GqlItemFactory.Query.NonPaged<IGetShuttlesResult, IGetShuttles_Shuttles>(
          label: "GQLShuttles",
          queryFunc: ct => _client.GetShuttles.ExecuteAsync(ct),
          selectData: r => r.Shuttles,
          allowEmptyData: true
        )
    );

  /// <summary>
  /// Deferred GQL query handle for review data. The consuming step materializes this
  /// via <c>ToList()</c>, which fires the <c>GetReviews</c> query against the server.
  /// </summary>
  public IItem<GqlQuery<IGetReviewsResult, IGetReviews_Reviews>> Reviews =>
    CreateItem(
      () =>
        GqlItemFactory.Query.NonPaged<IGetReviewsResult, IGetReviews_Reviews>(
          label: "GQLReviews",
          queryFunc: ct => _client.GetReviews.ExecuteAsync(ct),
          selectData: r => r.Reviews,
          allowEmptyData: true
        )
    );
}
