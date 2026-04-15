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

  /// <summary>
  /// Raw company data queried from the GQL server.
  /// </summary>
  public IItem<IEnumerable<IGetCompanies_Companies>> Companies =>
    CreateItem(
      () =>
        GqlItemFactory.Enumerable.Query<IGetCompaniesResult, IGetCompanies_Companies>(
          label: "GQLCompanies",
          queryFunc: ct => _client.GetCompanies.ExecuteAsync(ct),
          selectData: r => r.Companies,
          allowEmptyData: true
        )
    );

  /// <summary>
  /// Raw shuttle data queried from the GQL server (all shuttles, unfiltered).
  /// </summary>
  public IItem<IEnumerable<IGetShuttles_Shuttles>> Shuttles =>
    CreateItem(
      () =>
        GqlItemFactory.Enumerable.Query<IGetShuttlesResult, IGetShuttles_Shuttles>(
          label: "GQLShuttles",
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
          label: "GQLReviews",
          queryFunc: ct => _client.GetReviews.ExecuteAsync(ct),
          selectData: r => r.Reviews,
          allowEmptyData: true
        )
    );

  // ── Parameterized GQL entries — consumed by the Analytics flow ──────────

  /// <summary>
  /// The company ID of the highest-rated company. Written by FindTopRatedCompany;
  /// used as the parameter source for <see cref="TopRatedCompanyShuttles"/>.
  /// </summary>
  public IItem<string> TopRatedCompanyId =>
    CreateItem(() => ItemFactory.Single.Memory<string>("TopRatedCompanyId"));

  /// <summary>
  /// Shuttles operated by the top-rated company — a parameterized GQL catalog entry.
  /// At load time, the adapter reads <see cref="TopRatedCompanyId"/> and fires a
  /// filtered <c>GetShuttlesByCompanyId</c> query. Only that company's shuttles are
  /// transferred; the full shuttle dataset is never pulled.
  /// </summary>
  /// <remarks>
  /// The dependency analyzer discovers that this item's adapter depends on
  /// <see cref="TopRatedCompanyId"/>. Any step consuming <see cref="TopRatedCompanyShuttles"/>
  /// is automatically scheduled after the step that produces
  /// <see cref="TopRatedCompanyId"/>, with no explicit ordering required in the flow
  /// definition.
  /// </remarks>
  public IItem<IEnumerable<IGetShuttlesByCompanyId_Shuttles>> TopRatedCompanyShuttles =>
    CreateItem(
      () =>
        GqlItemFactory.Enumerable.Query<
          string,
          IGetShuttlesByCompanyIdResult,
          IGetShuttlesByCompanyId_Shuttles
        >(
          label: "GQLTopRatedCompanyShuttles",
          parameterSource: TopRatedCompanyId,
          queryFunc: (companyId, ct) => _client.GetShuttlesByCompanyId.ExecuteAsync(companyId, ct),
          selectData: r => r.Shuttles,
          allowEmptyData: true
        )
    );
}
