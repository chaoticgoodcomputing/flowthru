using Flowthru.Pipelines;
using Flowthru.Pipelines.Mapping;
using Flowthru.Tests.KedroSpaceflights.Data;
using Flowthru.Tests.KedroSpaceflights.Pipelines.DataProcessing.Nodes;

namespace Flowthru.Tests.KedroSpaceflights.Pipelines.DataProcessing;

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
/// 
/// <para><strong>Matches Kedro 1:1:</strong></para>
/// <para>
/// This pipeline now matches the original Kedro spaceflights data_processing pipeline,
/// with the addition of PreprocessReviewsNode (a minor refactor for better data handling).
/// All diagnostic nodes have been moved to the DataValidation pipeline.
/// </para>
/// </summary>
public static class DataProcessingPipeline {
    public static Pipeline Create(SpaceflightsCatalog catalog) {
        return PipelineBuilder.CreatePipeline(pipeline => {
            // Node 1: Preprocess companies (simple: single input → single output)
            pipeline.AddNode<PreprocessCompaniesNode>(
                input: catalog.Companies,
                output: catalog.CleanedCompanies,
                name: "PreprocessCompanies"
            );

            // Node 2: Preprocess shuttles (simple: single input → single output)
            pipeline.AddNode<PreprocessShuttlesNode>(
                input: catalog.Shuttles,
                output: catalog.CleanedShuttles,
                name: "PreprocessShuttles"
            );

            // Node 3: Preprocess reviews (simple: single input → single output)
            // Note: Minor refactor compared to Kedro - we preprocess reviews separately
            // rather than handling raw reviews in create_model_input_table
            pipeline.AddNode<PreprocessReviewsNode>(
                input: catalog.Reviews,
                output: catalog.CleanedReviews,
                name: "PreprocessReviews"
            );

            // Node 4: Create model input table (multi-input: 3 inputs → single output)
            pipeline.AddNode<CreateModelInputTableNode>(
                name: "CreateModelInputTable",
                input: new CatalogMap<CreateModelInputTableInputs>()
                    .Map(x => x.Shuttles, catalog.CleanedShuttles)
                    .Map(x => x.Companies, catalog.CleanedCompanies)
                    .Map(x => x.Reviews, catalog.CleanedReviews),
                output: catalog.ModelInputTable
            );
        });
    }
}
