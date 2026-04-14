using Flowthru.Core.Flows;
using KedroSpaceflightsSpark.Data;
using KedroSpaceflightsSpark.Flows.DataProcessing.Steps;

namespace KedroSpaceflightsSpark.Flows.DataProcessing;

public static class DataProcessingFlow
{
  public static Flow Create(Catalog catalog)
  {
    return FlowBuilder.CreateFlow(pipeline =>
    {
      pipeline.AddStep(
        label: "PreprocessCompanies",
        description: "Parses raw company strings into a typed Spark DataFrame.",
        transform: PreprocessCompaniesStep.Create(catalog._provider),
        input: catalog.Companies,
        output: catalog.PreprocessedCompanies
      );

      pipeline.AddStep(
        label: "PreprocessShuttles",
        description: "Parses raw shuttle strings into a typed Spark DataFrame.",
        transform: PreprocessShuttlesStep.Create(catalog._provider),
        input: catalog.Shuttles,
        output: catalog.PreprocessedShuttles
      );

      pipeline.AddStep(
        label: "PreprocessReviews",
        description: "Filters reviews to valid numeric scores and loads them into a Spark DataFrame.",
        transform: PreprocessReviewsStep.Create(catalog._provider),
        input: catalog.Reviews,
        output: catalog.ParsedReviews
      );

      pipeline.AddStep(
        label: "CreateModelInputTable",
        description: """
          Joins preprocessed shuttle and company TypedFrames with parsed review scores using
          Spark distributed joins, then materializes the result to Parquet.
        """,
        transform: CreateModelInputTableStep.Create(),
        input: (catalog.PreprocessedShuttles, catalog.PreprocessedCompanies, catalog.ParsedReviews),
        output: catalog.ModelInputTable
      );
    });
  }
}
