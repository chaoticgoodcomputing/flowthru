using Flowthru.Core.Flows;
using KedroSpaceflightsGQL.Data;
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
    /// <summary>
    /// Creates the ingest pipeline.
    /// </summary>
    public static Flow Create(Catalog catalog, ISpaceflightsClient client)
    {
        return FlowBuilder.CreateFlow(pipeline =>
        {
            pipeline.AddStep(
          label: "PreprocessCompanies",
          description: "Parses raw company CSV data (rating percentages, IATA flags) into typed records.",
          transform: PreprocessCompaniesStep.Create(),
          input: catalog.SeedCompanies,
          output: catalog.PreprocessedCompanies
        );

            pipeline.AddStep(
          label: "PreprocessShuttles",
          description: "Parses raw shuttle Excel data (numeric fields, currency, boolean flags) into typed records.",
          transform: PreprocessShuttlesStep.Create(),
          input: catalog.SeedShuttles,
          output: catalog.PreprocessedShuttles
        );

            pipeline.AddStep(
          label: "PreprocessReviews",
          description: "Parses raw review CSV data (rating strings) into typed decimal records.",
          transform: PreprocessReviewsStep.Create(),
          input: catalog.SeedReviews,
          output: catalog.PreprocessedReviews
        );

            pipeline.AddStep(
          label: "SeedGqlDatabase",
          description: """
          Seeds the GraphQL server with preprocessed typed data via addCompany / addShuttle /
          addReview mutations. Replace the in-process HotChocolate server with your own GQL
          endpoint in Program.cs to point at production data.
        """,
          transform: SeedGqlDatabaseStep.Create(client),
          input: (
            catalog.PreprocessedCompanies,
            catalog.PreprocessedShuttles,
            catalog.PreprocessedReviews
          ),
          output: catalog.GqlDatabaseSeeded
        );
        });
    }
}
