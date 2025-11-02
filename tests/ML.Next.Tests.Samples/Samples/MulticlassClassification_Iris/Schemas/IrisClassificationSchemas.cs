using ML.Next.Core.Columns;
using ML.Next.Core.Schema;

namespace ML.Next.Tests.Samples.Samples.MulticlassClassification_Iris.Schemas;

/// <summary>
/// Phantom type schemas for type-safe multiclass classification pipeline composition.
/// These interfaces never get instantiated - they exist purely for compile-time checking.
/// </summary>
public static class IrisClassificationSchemas
{
  /// <summary>
  /// Raw schema: columns as loaded from iris-train.txt / iris-test.txt
  /// </summary>
  public interface IRawSchema : ISchemaDefinition
  {
    ColumnSpec<float> Label { get; }
    ColumnSpec<float> SepalLength { get; }
    ColumnSpec<float> SepalWidth { get; }
    ColumnSpec<float> PetalLength { get; }
    ColumnSpec<float> PetalWidth { get; }
  }

  /// <summary>
  /// After MapValueToKey transformation (Label converted to key type for training)
  /// </summary>
  public interface IKeyedSchema : IRawSchema
  {
    ColumnSpec<uint> KeyColumn { get; }
  }

  /// <summary>
  /// After feature concatenation (all measurements combined into Features vector)
  /// </summary>
  public interface IFeaturesSchema : IKeyedSchema
  {
    ColumnSpec<float[]> Features { get; }
  }

  /// <summary>
  /// After multiclass classification training/prediction
  /// </summary>
  public interface IModelSchema : IFeaturesSchema
  {
    ColumnSpec<float> PredictedLabel { get; }
    ColumnSpec<float[]> Score { get; } // Probability scores for each class
  }
}
