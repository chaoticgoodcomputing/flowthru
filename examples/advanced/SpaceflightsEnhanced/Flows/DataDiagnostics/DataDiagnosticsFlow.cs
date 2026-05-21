using Flowthru.Flow;
using SpaceflightsEnhanced.Data;
using SpaceflightsEnhanced.Data._01_Raw.Schemas;
using SpaceflightsEnhanced.Data._02_Intermediate.Schemas;
using SpaceflightsEnhanced.Data._03_Primary.Schemas;
using SpaceflightsEnhanced.Flows.DataDiagnostics.Steps;

namespace SpaceflightsEnhanced.Flows.DataDiagnostics;

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
        inputs: (catalog.ModelInputTable, catalog.KedroModelInputTable)
      );

      pipeline.AddStep<IEnumerable<CompanySchema>, IEnumerable<CompanySchema>>(
        label: "ExportCompaniesToDiagnosticCsv",
        transform: PassthroughInputToOutputStep<CompanySchema>.Create(),
        inputs: catalog.CleanedCompanies,
        outputs: catalog.CleanedCompaniesCsv
      );

      pipeline.AddStep<IEnumerable<ShuttleSchema>, IEnumerable<ShuttleSchema>>(
        label: "ExportShuttlesToDiagnosticCsv",
        transform: PassthroughInputToOutputStep<ShuttleSchema>.Create(),
        inputs: catalog.CleanedShuttles,
        outputs: catalog.CleanedShuttlesCsv
      );

      pipeline.AddStep<IEnumerable<ModelInputSchema>, IEnumerable<ModelInputSchema>>(
        label: "ExportModelInputTableToDiagnosticCsv",
        transform: PassthroughInputToOutputStep<ModelInputSchema>.Create(),
        inputs: catalog.ModelInputTable,
        outputs: catalog.ModelInputTableCsv
      );

      pipeline.AddStep<IEnumerable<ModelInputSchema>, IEnumerable<ModelInputSchema>>(
        label: "ExportModelInputTableToMinifiedJson",
        transform: PassthroughInputToOutputStep<ModelInputSchema>.Create(),
        inputs: catalog.ModelInputTable,
        outputs: catalog.ModelInputTableJsonMinified
      );
    });
  }
}
