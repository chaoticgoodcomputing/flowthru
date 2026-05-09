using Flowthru.Flow;
using KedroSpaceflightsCustom.Data;
using KedroSpaceflightsCustom.Data._01_Raw.Schemas;
using KedroSpaceflightsCustom.Data._02_Intermediate.Schemas;
using KedroSpaceflightsCustom.Data._03_Primary.Schemas;
using KedroSpaceflightsCustom.Flows.DataDiagnostics.Steps;

namespace KedroSpaceflightsCustom.Flows.DataDiagnostics;

/// <summary>
/// Data validation pipeline that performs diagnostic and validation operations on pipeline outputs.
/// </summary>
public static class DataDiagnosticsFlow
{
  public static BuiltFlow Create(Catalog catalog)
  {
    return FlowBuilder.CreateFlow("DataDiagnostics", pipeline =>
    {
      pipeline.AddStep<IEnumerable<ModelInputSchema>, IEnumerable<KedroModelInputSchema>>(
        label: "ValidateModelInputTableAgainstKedroSource",
        transform: ValidateAgainstKedroStep.Create(),
        input1: catalog.ModelInputTable,
        input2: catalog.KedroModelInputTable
      );

      pipeline.AddStep<IEnumerable<CompanySchema>, IEnumerable<CompanySchema>>(
        label: "ExportCompaniesToDiagnosticCsv",
        transform: PassthroughInputToOutputStep<CompanySchema>.Create(),
        input1: catalog.CleanedCompanies,
        output1: catalog.CleanedCompaniesCsv
      );

      pipeline.AddStep<IEnumerable<ShuttleSchema>, IEnumerable<ShuttleSchema>>(
        label: "ExportShuttlesToDiagnosticCsv",
        transform: PassthroughInputToOutputStep<ShuttleSchema>.Create(),
        input1: catalog.CleanedShuttles,
        output1: catalog.CleanedShuttlesCsv
      );

      pipeline.AddStep<IEnumerable<ModelInputSchema>, IEnumerable<ModelInputSchema>>(
        label: "ExportModelInputTableToDiagnosticCsv",
        transform: PassthroughInputToOutputStep<ModelInputSchema>.Create(),
        input1: catalog.ModelInputTable,
        output1: catalog.ModelInputTableCsv
      );

      pipeline.AddStep<IEnumerable<ModelInputSchema>, IEnumerable<ModelInputSchema>>(
        label: "ExportModelInputTableToMinifiedJson",
        transform: PassthroughInputToOutputStep<ModelInputSchema>.Create(),
        input1: catalog.ModelInputTable,
        output1: catalog.ModelInputTableJsonMinified
      );
    });
  }
}
