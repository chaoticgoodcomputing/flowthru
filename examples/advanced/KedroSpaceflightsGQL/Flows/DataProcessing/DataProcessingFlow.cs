using Flowthru.Flow;
using Flowthru.Data.Storage.Gql;
using KedroSpaceflightsGQL.Data;
using KedroSpaceflightsGQL.Data._03_Primary.Schemas;
using KedroSpaceflightsGQL.Flows.DataProcessing.Steps;
using KedroSpaceflightsGQL.Infra.GqlClient;

namespace KedroSpaceflightsGQL.Flows.DataProcessing;

/// <summary>
/// Data processing pipeline. Reads typed data from the GQL server and joins it into a
/// model input table. Depends on Ingest completing first via the GqlDatabaseSeeded gate.
/// </summary>
public static class DataProcessingFlow
{
  public static BuiltFlow Create(Catalog catalog)
  {
    return FlowBuilder.CreateFlow("DataProcessing", pipeline =>
    {
      pipeline.AddStep<
        bool,
        GqlQuery<IGetShuttlesResult, IGetShuttles_Shuttles>,
        GqlQuery<IGetCompaniesResult, IGetCompanies_Companies>,
        GqlQuery<IGetReviewsResult, IGetReviews_Reviews>,
        IEnumerable<ModelInputTableSchema>
      >(
        label: "CreateModelInputTable",
        transform: CreateModelInputTableStep.Create(),
        inputs: (catalog.GqlDatabaseSeeded, catalog.Shuttles, catalog.Companies, catalog.Reviews),
        outputs: catalog.ModelInputTable
      );
    });
  }
}
