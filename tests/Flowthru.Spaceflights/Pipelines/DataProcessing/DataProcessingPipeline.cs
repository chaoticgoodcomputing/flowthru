using Flowthru.Data.Implementations;
using Flowthru.Nodes;
using Flowthru.Pipelines;
using Flowthru.Pipelines.Mapping;
using Flowthru.Spaceflights.Data;
using Flowthru.Spaceflights.Data.Schemas.Processed;
using Flowthru.Spaceflights.Data.Schemas.Raw;
using Flowthru.Spaceflights.Pipelines.DataProcessing.Nodes;

namespace Flowthru.Spaceflights.Pipelines.DataProcessing;

/// <summary>
/// Data processing pipeline that preprocesses raw data and creates model input table.
/// Follows Kedro's pattern: nodes are pure functions, pipeline declares the data flow.
/// 
/// <para><strong>Compile-Time Type Safety:</strong></para>
/// <para>
/// This pipeline uses a strongly-typed catalog (SpaceflightsCatalog) to ensure:
/// - All catalog references are validated at compile-time
/// - Node input/output types must match catalog entry types
/// - CatalogMap enforces property-to-catalog type consistency
/// - Refactoring tools work seamlessly (rename, find references)
/// - IntelliSense shows available catalog entries with their types
/// </para>
/// </summary>
public static class DataProcessingPipeline
{
  public static Pipeline Create(SpaceflightsCatalog catalog)
  {
    return PipelineBuilder.CreatePipeline(pipeline =>
    {
      // Node 1: Preprocess companies (simple: single input → single output)
      pipeline.AddNode<PreprocessCompaniesNode, CompanyRawSchema, CompanySchema, NoParams>(
        input: catalog.Companies,                    // ✅ Type-checked: ICatalogEntry<IEnumerable<CompanyRawSchema>>
        output: catalog.PreprocessedCompanies,       // ✅ Type-checked: ICatalogEntry<IEnumerable<CompanySchema>>
        name: "preprocess_companies_node"
      );

      // Node 2: Preprocess shuttles (simple: single input → single output)
      pipeline.AddNode<PreprocessShuttlesNode, ShuttleRawSchema, ShuttleSchema, NoParams>(
        input: catalog.Shuttles,                     // ✅ Type-checked: ICatalogEntry<IEnumerable<ShuttleRawSchema>>
        output: catalog.PreprocessedShuttles,        // ✅ Type-checked: ICatalogEntry<IEnumerable<ShuttleSchema>>
        name: "preprocess_shuttles_node"
      );

      // Node 3: Create model input table (multi-input: 3 inputs → single output)
      var createModelInputs = new CatalogMap<CreateModelInputTableInputs>()
        .Map(x => x.Shuttles, catalog.PreprocessedShuttles)   // ✅ Both IEnumerable<ShuttleSchema>
        .Map(x => x.Companies, catalog.PreprocessedCompanies) // ✅ Both IEnumerable<CompanySchema>
        .Map(x => x.Reviews, catalog.Reviews);                // ✅ Both IEnumerable<ReviewRawSchema>

      pipeline.AddNode<CreateModelInputTableNode, CreateModelInputTableInputs, ModelInputSchema, NoParams>(
        input: createModelInputs,                    // ✅ Type-checked via CatalogMap
        output: catalog.ModelInputTable,             // ✅ Type-checked: ICatalogEntry<IEnumerable<ModelInputSchema>>
        name: "create_model_input_table_node"
      );

      // Diagnostic Nodes: Export Parquet data as CSV for inspection
      pipeline.AddNode<ExportToCsvNode<CompanySchema>, CompanySchema, CompanySchema, NoParams>(
        input: catalog.PreprocessedCompanies,
        output: catalog.PreprocessedCompaniesCsv,
        name: "export_companies_csv_node"
      );

      pipeline.AddNode<ExportToCsvNode<ShuttleSchema>, ShuttleSchema, ShuttleSchema, NoParams>(
        input: catalog.PreprocessedShuttles,
        output: catalog.PreprocessedShuttlesCsv,
        name: "export_shuttles_csv_node"
      );

      pipeline.AddNode<ExportToCsvNode<ModelInputSchema>, ModelInputSchema, ModelInputSchema, NoParams>(
        input: catalog.ModelInputTable,
        output: catalog.ModelInputTableCsv,
        name: "export_model_input_csv_node"
      );

      // Validation Node: Compare Flowthru output against Kedro reference
      var validationInputs = new CatalogMap<ValidateAgainstKedroInputs>()
        .Map(x => x.FlowthruData, catalog.ModelInputTable)
        .Map(x => x.KedroData, catalog.KedroModelInputTable);

      // Use throwaway MemoryCatalogEntry for diagnostic node output (not persisted)
      var throwawayOutput = new MemoryCatalogEntry<IEnumerable<ModelInputSchema>>("_validation_throwaway");

      pipeline.AddNode<ValidateAgainstKedroNode, ValidateAgainstKedroInputs, ModelInputSchema, NoParams>(
        input: validationInputs,
        output: throwawayOutput,
        name: "validate_against_kedro_node"
      );
    });
  }
}
