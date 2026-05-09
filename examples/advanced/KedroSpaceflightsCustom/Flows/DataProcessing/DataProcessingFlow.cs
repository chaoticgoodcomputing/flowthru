using Flowthru.Flow;
using KedroSpaceflightsCustom.Data;
using KedroSpaceflightsCustom.Data._01_Raw.Schemas;
using KedroSpaceflightsCustom.Data._02_Intermediate.Schemas;
using KedroSpaceflightsCustom.Data._03_Primary.Schemas;
using KedroSpaceflightsCustom.Flows.DataProcessing.Steps;

namespace KedroSpaceflightsCustom.Flows.DataProcessing;

/// <summary>
/// Data processing pipeline that preprocesses raw data and creates model input table.
/// </summary>
public static class DataProcessingFlow
{
  public static BuiltFlow Create(Catalog catalog)
  {
    return FlowBuilder.CreateFlow("DataProcessing", pipeline =>
    {
      pipeline.AddStep<IEnumerable<CompanyRawSchema>, IEnumerable<CompanySchema>>(
        label: "PreprocessCompanies",
        transform: PreprocessCompaniesStep.Create(),
        input1: catalog.Companies,
        output1: catalog.CleanedCompanies
      );

      pipeline.AddStep<IEnumerable<ShuttleRawSchema>, IEnumerable<ShuttleSchema>>(
        label: "PreprocessShuttles",
        transform: PreprocessShuttlesStep.Create(),
        input1: catalog.Shuttles,
        output1: catalog.CleanedShuttles
      );

      pipeline.AddStep<IEnumerable<ReviewRawSchema>, IEnumerable<ReviewSchema>>(
        label: "PreprocessReviews",
        transform: PreprocessReviewsStep.Create(),
        input1: catalog.Reviews,
        output1: catalog.CleanedReviews
      );

      pipeline.AddStep<
        IEnumerable<ShuttleSchema>,
        IEnumerable<CompanySchema>,
        IEnumerable<ReviewSchema>,
        IEnumerable<ModelInputSchema>
      >(
        label: "CreateModelInputTable",
        transform: CreateModelInputTableStep.Create(),
        input1: catalog.CleanedShuttles,
        input2: catalog.CleanedCompanies,
        input3: catalog.CleanedReviews,
        output1: catalog.ModelInputTable
      );
    });
  }
}
