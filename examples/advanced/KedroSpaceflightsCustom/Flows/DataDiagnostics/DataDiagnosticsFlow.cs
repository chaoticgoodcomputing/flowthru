using Flowthru.Core.Flows;
using KedroSpaceflightsCustom.Data;
using KedroSpaceflightsCustom.Data._02_Intermediate.Schemas;
using KedroSpaceflightsCustom.Data._03_Primary.Schemas;
using KedroSpaceflightsCustom.Flows.DataDiagnostics.Steps;

namespace KedroSpaceflightsCustom.Flows.DataDiagnostics;

/// <summary>
/// Data validation pipeline that performs diagnostic and validation operations on pipeline outputs.
///
/// <para>
/// This pipeline contains all diagnostic nodes that validate Flowthru's implementation against
/// the original Kedro spaceflights example, as well as nodes that export data to CSV for
/// manual inspection.
/// </para>
///
/// <para><strong>Diagnostic Steps:</strong></para>
/// <list type="bullet">
/// <item>ValidateAgainstKedroStep - Compares Flowthru vs Kedro model input table (demonstrates 2-input, 0-output side-effect nodes)</item>
/// <item>ExportToCsvStep - Exports intermediate datasets to CSV for debugging</item>
/// <item>CrossValidateModelStep - Performs k-fold cross-validation and comparison to Kedro</item>
/// </list>
///
/// <para>
/// Most nodes in this pipeline are pass-through nodes that output their inputs unchanged,
/// making this pipeline safe to run alongside production pipelines without affecting results.
/// </para>
/// </summary>
public static class DataDiagnosticsFlow
{
  public static Flow Create(Catalog catalog)
  {
    return FlowBuilder.CreateFlow(pipeline =>
    {
      // Step 1: Validate model input table against Kedro reference output (2-input, 0-output diagnostic step)
      pipeline.AddStep(
        label: "ValidateModelInputTableAgainstKedroSource",
        transform: ValidateAgainstKedroStep.Create(),
        input: (catalog.ModelInputTable, catalog.KedroModelInputTable)
      );

      // Step 2: Export cleaned companies to CSV for manual inspection
      pipeline.AddStep(
        label: "ExportCompaniesToDiagnosticCsv",
        transform: PassthroughInputToOutputStep<CompanySchema>.Create(),
        input: catalog.CleanedCompanies,
        output: catalog.CleanedCompaniesCsv
      );

      // Step 3: Export cleaned shuttles to CSV for manual inspection
      pipeline.AddStep(
        label: "ExportShuttlesToDiagnosticCsv",
        transform: PassthroughInputToOutputStep<ShuttleSchema>.Create(),
        input: catalog.CleanedShuttles,
        output: catalog.CleanedShuttlesCsv
      );

      // Step 4: Export model input table to CSV for manual inspection
      pipeline.AddStep(
        label: "ExportModelInputTableToDiagnosticCsv",
        transform: PassthroughInputToOutputStep<ModelInputSchema>.Create(),
        input: catalog.ModelInputTable,
        output: catalog.ModelInputTableCsv
      );

      // Step 5: Export model input table to minified JSON for production/compact storage
      pipeline.AddStep(
        label: "ExportModelInputTableToMinifiedJson",
        transform: PassthroughInputToOutputStep<ModelInputSchema>.Create(),
        input: catalog.ModelInputTable,
        output: catalog.ModelInputTableJsonMinified
      );
    });
  }
}
