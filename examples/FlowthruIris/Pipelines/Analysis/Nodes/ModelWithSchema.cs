using Microsoft.ML;
using Microsoft.ML.Data;

namespace FlowthruIris.Pipelines.Analysis.Nodes;

/// <summary>
/// Helper record to bundle an ML.NET model with its training data schema.
/// 
/// <para><strong>Purpose:</strong></para>
/// <para>
/// ML.NET's Model.Save() requires the schema from the training data.
/// This record captures both artifacts during training so they can be
/// stored together in the catalog.
/// </para>
/// 
/// <para><strong>Usage in Training Nodes:</strong></para>
/// <code>
/// var model = pipeline.Fit(dataView);
/// var metrics = mlContext.MulticlassClassification.Evaluate(predictions);
/// return (new ModelWithSchema(model, dataView.Schema), metrics);
/// </code>
/// 
/// <para><strong>Usage in Catalog Entries:</strong></para>
/// <para>
/// The catalog entry can unwrap this to call mlContext.Model.Save(Model, Schema, path).
/// </para>
/// </summary>
public record ModelWithSchema(ITransformer Model, DataViewSchema Schema);
