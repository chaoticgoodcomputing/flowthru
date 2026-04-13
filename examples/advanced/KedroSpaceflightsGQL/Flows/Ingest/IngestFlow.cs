using Flowthru.Core.Flows;
using KedroSpaceflightsGQL.Data;
using KedroSpaceflightsGQL.Flows.Ingest.Steps;
using KedroSpaceflightsGQL.Infra.GqlClient;

namespace KedroSpaceflightsGQL.Flows.Ingest;

/// <summary>
/// Ingest pipeline: reads raw CSV/Excel files and seeds the GQL server via mutations.
/// This flow must run before DataProcessing, which queries the GQL server.
/// </summary>
public static class IngestFlow
{
  /// <summary>
  /// Creates the ingest pipeline.
  /// </summary>
  /// <param name="catalog">Data catalog providing seed (CSV/Excel) and ack catalog entries.</param>
  /// <param name="client">
  /// StrawberryShake GQL client. The same instance used by the DataProcessing catalog entries.
  /// </param>
  /// <remarks>
  /// Both <paramref name="catalog"/> and <paramref name="client"/> are resolved from DI
  /// by Flowthru's delegate-inspection registration — no factory lambda required.
  /// </remarks>
  public static Flow Create(Catalog catalog, ISpaceflightsClient client)
  {
    return FlowBuilder.CreateFlow(pipeline =>
    {
      pipeline.AddStep(
        label: "SeedGqlDatabase",
        description: """
          Reads raw companies (CSV), shuttles (Excel), and reviews (CSV) then seeds the GraphQL
          server via addCompany / addShuttle / addReview mutations. Replace the in-process
          HotChocolate server with your own GQL endpoint in Program.cs to point at production data.
        """,
        transform: SeedGqlDatabaseStep.Create(client),
        input: (catalog.SeedCompanies, catalog.SeedShuttles, catalog.SeedReviews),
        output: catalog.GqlDatabaseSeeded
      );
    });
  }
}
