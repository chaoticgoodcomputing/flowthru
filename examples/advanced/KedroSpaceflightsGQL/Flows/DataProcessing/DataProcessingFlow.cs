using Flowthru.Core.Flows;
using KedroSpaceflightsGQL.Data;
using KedroSpaceflightsGQL.Flows.DataProcessing.Steps;

namespace KedroSpaceflightsGQL.Flows.DataProcessing;

/// <summary>
/// Data processing pipeline. Reads typed data from the GQL server and joins it into a
/// model input table. Depends on Ingest completing first via the GqlDatabaseSeeded gate.
/// </summary>
public static class DataProcessingFlow
{
    public static Flow Create(Catalog catalog)
    {
        return FlowBuilder.CreateFlow(pipeline =>
        {
            pipeline.AddStep(
          label: "CreateModelInputTable",
          description: """
          Joins typed shuttle, company, and review data queried from the GQL server
          into a unified model input table. GqlDatabaseSeeded is consumed as an explicit
          DAG gate ensuring Ingest has completed before this step executes.
        """,
          transform: CreateModelInputTableStep.Create(),
          input: (catalog.GqlDatabaseSeeded, catalog.Shuttles, catalog.Companies, catalog.Reviews),
          output: catalog.ModelInputTable
        );
        });
    }
}
