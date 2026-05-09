using Flowthru.Flow;
using KedroSpaceflightsGQL.Data;
using KedroSpaceflightsGQL.Data._01_Raw.Schemas;
using KedroSpaceflightsGQL.Data._02_Intermediate.Schemas;
using KedroSpaceflightsGQL.Flows.Ingest.Steps;
using KedroSpaceflightsGQL.Infra.GqlClient;

namespace KedroSpaceflightsGQL.Flows.Ingest;

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
        input1: catalog.SeedCompanies,
        output1: catalog.PreprocessedCompanies
      );

      pipeline.AddStep<IEnumerable<ShuttleSchema>, IEnumerable<PreprocessedShuttleSchema>>(
        label: "PreprocessShuttles",
        transform: PreprocessShuttlesStep.Create(),
        input1: catalog.SeedShuttles,
        output1: catalog.PreprocessedShuttles
      );

      pipeline.AddStep<IEnumerable<ReviewSchema>, IEnumerable<PreprocessedReviewSchema>>(
        label: "PreprocessReviews",
        transform: PreprocessReviewsStep.Create(),
        input1: catalog.SeedReviews,
        output1: catalog.PreprocessedReviews
      );

      pipeline.AddStep<
        IEnumerable<PreprocessedCompanySchema>,
        IEnumerable<PreprocessedShuttleSchema>,
        IEnumerable<PreprocessedReviewSchema>,
        bool
      >(
        label: "SeedGqlDatabase",
        transform: SeedGqlDatabaseStep.Create(client),
        input1: catalog.PreprocessedCompanies,
        input2: catalog.PreprocessedShuttles,
        input3: catalog.PreprocessedReviews,
        output1: catalog.GqlDatabaseSeeded
      );
    });
  }
}
