using Flowthru.Pipelines;
using FlowthruIris.Data;
using FlowthruIris.Data.Schemas;
using FlowthruIris.Pipelines.Analysis.Nodes;

namespace FlowthruIris.Pipelines.Analysis;

/// <summary>
/// Analysis pipeline that processes raw data and trains two classification models.
/// 
/// <para><strong>Pipeline Structure:</strong></para>
/// <list type="number">
/// <item>ProcessRawData: Convert CSV strings → typed floats + feature engineering</item>
/// <item>TrainSdcaModel: Train SDCA model → (model.zip, metrics.json)</item>
/// <item>TrainOvaPerceptron: Train OVA+Perceptron model → (model.zip, metrics.json)</item>
/// </list>
/// 
/// <para><strong>Multi-Output Pattern:</strong></para>
/// <para>
/// Training nodes return tuples (ITransformer, MulticlassClassificationMetrics),
/// which map to separate catalog entries:
/// - MLNetModelCatalogEntry persists ITransformer as .zip
/// - JsonCatalogEntry persists metrics as .json
/// This separation provides clean data handling and easy metric inspection.
/// </para>
/// 
/// <para><strong>Compile-Time Type Safety:</strong></para>
/// <para>
/// Flowthru's PipelineBuilder enforces correct data flow at compile time:
/// - ProcessRawDataNode output (IrisSchema) matches TrainSdcaModelNode input
/// - Both model training nodes share the same input type
/// - Catalog entries are strongly typed (compiler catches mismatches)
/// </para>
/// 
/// <para><strong>Execution Order:</strong></para>
/// <para>
/// Flowthru automatically determines execution order based on data dependencies:
/// 1. ProcessRawData executes first (reads from RawIris catalog entry)
/// 2. Both model training nodes execute in parallel (both depend on ProcessRawData output)
/// 3. Models are saved to catalog once training completes
/// </para>
/// 
/// <para><strong>Usage:</strong></para>
/// <code>
/// dotnet run Analysis
/// </code>
/// </summary>
public static class AnalysisPipeline {
  /// <summary>
  /// Creates the Analysis pipeline with all nodes wired to catalog entries.
  /// </summary>
  /// <param name="catalog">The strongly-typed Iris catalog</param>
  /// <returns>Configured pipeline ready for execution</returns>
  public static Pipeline Create(IrisCatalog catalog) {
    // Create a memory catalog entry for intermediate processed data
    // This data exists only during pipeline execution (not persisted to disk)
    var processedData = new Flowthru.Data.Implementations.MemoryCatalogEntry<IEnumerable<IrisSchema>>("processed_iris_data");

    return PipelineBuilder.CreatePipeline(pipeline => {
      // Node 1: Process raw CSV data → typed, validated data with engineered features
      pipeline.AddNode<ProcessRawDataNode>(
          input: catalog.RawIris,
          output: processedData,
          label: "ProcessRawData"
      );

      // Node 2: Train simple SDCA model (returns model + metrics)
      pipeline.AddNode<TrainSdcaModelNode>(
          input: processedData,
          output: (catalog.SdcaModel, catalog.SdcaMetrics),
          label: "TrainSdcaModel"
      );

      // Node 3: Train advanced OVA + Averaged Perceptron model (returns model + metrics)
      pipeline.AddNode<TrainOvaPerceptronModelNode>(
          input: processedData,
          output: (catalog.OvaPerceptronModel, catalog.OvaPerceptronMetrics),
          label: "TrainOvaPerceptron"
      );

      // Note: Nodes 2 and 3 can execute in parallel since they both depend only on Node 1
    });
  }
}
