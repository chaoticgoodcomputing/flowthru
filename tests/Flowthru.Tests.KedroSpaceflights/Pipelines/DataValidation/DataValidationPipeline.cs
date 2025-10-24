using Flowthru.Data.Implementations;
using Flowthru.Nodes;
using Flowthru.Pipelines;
using Flowthru.Pipelines.Mapping;
using Flowthru.Tests.KedroSpaceflights.Data;
using Flowthru.Tests.KedroSpaceflights.Data.Schemas.Processed;
using Flowthru.Tests.KedroSpaceflights.Pipelines.DataValidation.Nodes;

namespace Flowthru.Tests.KedroSpaceflights.Pipelines.DataValidation;

/// <summary>
/// Data validation pipeline that performs diagnostic and validation operations on pipeline outputs.
/// 
/// <para>
/// This pipeline contains all diagnostic nodes that validate Flowthru's implementation against
/// the original Kedro spaceflights example, as well as nodes that export data to CSV for
/// manual inspection.
/// </para>
/// 
/// <para><strong>Diagnostic Nodes:</strong></para>
/// <list type="bullet">
/// <item>GenerateSyntheticDataNode - Generates test data with NoData input (demonstrates no-input nodes)</item>
/// <item>ValidateAgainstKedroNode - Compares Flowthru vs Kedro model input table (demonstrates no-output nodes)</item>
/// <item>ExportToCsvNode - Exports intermediate datasets to CSV for debugging</item>
/// <item>CrossValidateModelNode - Performs k-fold cross-validation and comparison to Kedro</item>
/// </list>
/// 
/// <para>
/// Most nodes in this pipeline are pass-through nodes that output their inputs unchanged,
/// making this pipeline safe to run alongside production pipelines without affecting results.
/// </para>
/// </summary>
public static class DataValidationPipeline {
  public static Pipeline Create(SpaceflightsCatalog catalog) {
    return PipelineBuilder.CreatePipeline(pipeline => {

      // Node 1: Validate model input table against Kedro reference output (demonstrates NoData output pattern)
      pipeline.AddNode<ValidateAgainstKedroNode>(
        name: "ValidateModelInputTableAgainstKedroSource",
        input: new CatalogMap<ValidateAgainstKedroInputs>()
          .Map(x => x.FlowthruData, catalog.ModelInputTable)
          .Map(x => x.KedroData, catalog.KedroModelInputTable),
        output: NoData.Discard
      );

      // Node 2: Export cleaned companies to CSV for manual inspection
      pipeline.AddNode<PassthroughInputToOutputNode<CompanySchema>>(
        name: "ExportCompaniesToDiagnosticCsv",
        input: catalog.CleanedCompanies,
        output: catalog.CleanedCompaniesCsv
      );

      // Node 3: Export cleaned shuttles to CSV for manual inspection
      pipeline.AddNode<PassthroughInputToOutputNode<ShuttleSchema>>(
        name: "ExportShuttlesToDiagnosticCsv",
        input: catalog.CleanedShuttles,
        output: catalog.CleanedShuttlesCsv
      );

      // Node 4: Export model input table to CSV for manual inspection
      pipeline.AddNode<PassthroughInputToOutputNode<ModelInputSchema>>(
        name: "ExportModelInputTableToDiagnosticCsv",
        input: catalog.ModelInputTable,
        output: catalog.ModelInputTableCsv
      );

      // Node 5: Export model input table to minified JSON for production/compact storage
      pipeline.AddNode<PassthroughInputToOutputNode<ModelInputSchema>>(
        name: "ExportModelInputTableToMinifiedJson",
        input: catalog.ModelInputTable,
        output: catalog.ModelInputTableJsonMinified
      );
    });
  }
}
