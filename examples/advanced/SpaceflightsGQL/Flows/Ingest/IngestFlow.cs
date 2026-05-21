using Flowthru.Flow;
using SpaceflightsGQL.Data;
using SpaceflightsGQL.Data._01_Raw.Schemas;
using SpaceflightsGQL.Data._02_Intermediate.Schemas;
using SpaceflightsGQL.Flows.Ingest.Steps;
using SpaceflightsGQL.Infra.GqlClient;

namespace SpaceflightsGQL.Flows.Ingest;

/// <summary>
/// Ingest pipeline: preprocesses raw CSV/Excel files and seeds the GQL server via mutations.
/// Preprocessing runs first so the GQL server stores typed values (int, decimal, bool)
/// rather than raw strings. DataProcessing reads back through the GQL API and gets
/// first-class C# types straight from the generated StrawberryShake interfaces.
/// </summary>
public static class IngestFlow
{
  public static BuiltFlow Create(Catalog catalog, ISpaceflightsClient client)
  {
    return FlowBuilder.CreateFlow("Ingest", pipeline =>
    {
      pipeline.AddStep<IEnumerable<CompanySchema>, IEnumerable<PreprocessedCompanySchema>>(
        label: "PreprocessCompanies",
        transform: PreprocessCompaniesStep.Create(),
        inputs: catalog.SeedCompanies,
        outputs: catalog.PreprocessedCompanies
      );

      pipeline.AddStep<IEnumerable<ShuttleSchema>, IEnumerable<PreprocessedShuttleSchema>>(
        label: "PreprocessShuttles",
        transform: PreprocessShuttlesStep.Create(),
        inputs: catalog.SeedShuttles,
        outputs: catalog.PreprocessedShuttles
      );

      pipeline.AddStep<IEnumerable<ReviewSchema>, IEnumerable<PreprocessedReviewSchema>>(
        label: "PreprocessReviews",
        transform: PreprocessReviewsStep.Create(),
        inputs: catalog.SeedReviews,
        outputs: catalog.PreprocessedReviews
      );

      pipeline.AddStep<
        IEnumerable<PreprocessedCompanySchema>,
        IEnumerable<PreprocessedShuttleSchema>,
        IEnumerable<PreprocessedReviewSchema>,
        bool
      >(
        label: "SeedGqlDatabase",
        transform: SeedGqlDatabaseStep.Create(client),
        inputs: (catalog.PreprocessedCompanies, catalog.PreprocessedShuttles, catalog.PreprocessedReviews),
        outputs: catalog.GqlDatabaseSeeded
      );
    });
  }
}
