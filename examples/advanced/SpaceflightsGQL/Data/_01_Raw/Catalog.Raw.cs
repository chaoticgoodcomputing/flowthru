using Flowthru.Data.Catalog;
using Flowthru.Data.Storage.Gql;
using SpaceflightsGQL.Data._01_Raw.Schemas;
using SpaceflightsGQL.Infra.GqlClient;

namespace SpaceflightsGQL.Data;

public partial class Catalog
{
  // ── Seed entries (CSV / Excel) — consumed by the Ingest flow ────────────

  /// <summary>
  /// Raw company data read from CSV. Used by the Ingest flow to seed the GQL server.
  /// </summary>
  public IItem<IEnumerable<CompanySchema>> SeedCompanies =>
    CreateItem(() => Item.Of<IEnumerable<CompanySchema>>("SeedCompanies")
      .Csv()
      .AtPath($"{_basePath}/_01_Raw/Datasets/companies.csv")
      .Build());

  /// <summary>
  /// Raw review data read from CSV. Used by the Ingest flow to seed the GQL server.
  /// </summary>
  public IItem<IEnumerable<ReviewSchema>> SeedReviews =>
    CreateItem(() => Item.Of<IEnumerable<ReviewSchema>>("SeedReviews")
      .Csv()
      .AtPath($"{_basePath}/_01_Raw/Datasets/reviews.csv")
      .Build());

  /// <summary>
  /// Raw shuttle data read from Excel. Used by the Ingest flow to seed the GQL server.
  /// </summary>
  public IItem<IEnumerable<ShuttleSchema>> SeedShuttles =>
    CreateItem(() => Item.Of<IEnumerable<ShuttleSchema>>("SeedShuttles")
      .Excel()
      .AtPath($"{_basePath}/_01_Raw/Datasets/shuttles.xlsx")
      .WithSheet("Sheet1")
      .Build());

  /// <summary>
  /// In-memory flag written by the Ingest flow after all mutations succeed.
  /// Downstream flows depend on this via the DAG.
  /// </summary>
  public IItem<bool> GqlDatabaseSeeded =>
    CreateItem(() => Item.Of<bool>("GqlDatabaseSeeded").Memory().Build());

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
    CreateItem(() => Item.Of<GqlQuery<IGetCompaniesResult, IGetCompanies_Companies>>("GQLCompanies")
      .GqlDeferred(
        queryFunc: ct => _client.GetCompanies.ExecuteAsync(ct),
        selectData: r => r.Companies)
      .AllowEmpty()
      .Build());

  /// <summary>
  /// Deferred GQL query handle for shuttle data. The consuming step materializes this
  /// via <c>ToList()</c>, which fires the <c>GetShuttles</c> query against the server.
  /// </summary>
  public IItem<GqlQuery<IGetShuttlesResult, IGetShuttles_Shuttles>> Shuttles =>
    CreateItem(() => Item.Of<GqlQuery<IGetShuttlesResult, IGetShuttles_Shuttles>>("GQLShuttles")
      .GqlDeferred(
        queryFunc: ct => _client.GetShuttles.ExecuteAsync(ct),
        selectData: r => r.Shuttles)
      .AllowEmpty()
      .Build());

  /// <summary>
  /// Deferred GQL query handle for review data. The consuming step materializes this
  /// via <c>ToList()</c>, which fires the <c>GetReviews</c> query against the server.
  /// </summary>
  public IItem<GqlQuery<IGetReviewsResult, IGetReviews_Reviews>> Reviews =>
    CreateItem(() => Item.Of<GqlQuery<IGetReviewsResult, IGetReviews_Reviews>>("GQLReviews")
      .GqlDeferred(
        queryFunc: ct => _client.GetReviews.ExecuteAsync(ct),
        selectData: r => r.Reviews)
      .AllowEmpty()
      .Build());
}
