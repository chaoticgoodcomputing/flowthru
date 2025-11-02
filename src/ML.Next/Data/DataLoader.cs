using LanguageExt;
using LanguageExt.Common;
using Microsoft.ML;
using Microsoft.ML.Data;
using ML.Next.Core.Schema;

namespace ML.Next.Data;

/// <summary>
/// Type-safe data loader with compile-time schema validation.
/// </summary>
public static class DataLoader {
  /// <summary>
  /// Load data from enumerable with compile-time schema tracking.
  /// </summary>
  /// <typeparam name="TSchema">The compile-time schema definition</typeparam>
  /// <typeparam name="T">The source data type (must be a reference type)</typeparam>
  /// <param name="context">ML.NET context</param>
  /// <param name="data">The enumerable data source</param>
  /// <param name="schemaDefinition">Optional schema definition for customization</param>
  /// <returns>Validated DataView with schema tracking, or validation errors</returns>
  public static Fin<DataView<TSchema>> LoadFromEnumerable<TSchema, T>(
      MLContext context,
      IEnumerable<T> data,
      SchemaDefinition? schemaDefinition = null)
      where TSchema : ISchemaDefinition
      where T : class {
    try {
      var mlView = context.Data.LoadFromEnumerable(data, schemaDefinition);
      return Fin<DataView<TSchema>>.Succ(DataView<TSchema>.From(mlView));
    } catch (Exception ex) {
      return Fin<DataView<TSchema>>.Fail(Error.New(ex));
    }
  }

  /// <summary>
  /// Load data from a text file with compile-time schema tracking.
  /// </summary>
  /// <typeparam name="TSchema">The compile-time schema definition</typeparam>
  /// <param name="context">ML.NET context</param>
  /// <param name="path">Path to the text file</param>
  /// <param name="options">Text loader options</param>
  /// <returns>Validated DataView with schema tracking, or validation errors</returns>
  public static Fin<DataView<TSchema>> LoadFromTextFile<TSchema>(
      MLContext context,
      string path,
      TextLoader.Options? options = null)
      where TSchema : ISchemaDefinition {
    try {
      var loader = context.Data.CreateTextLoader(options ?? new TextLoader.Options());
      var mlView = loader.Load(path);
      return Fin<DataView<TSchema>>.Succ(DataView<TSchema>.From(mlView));
    } catch (Exception ex) {
      return Fin<DataView<TSchema>>.Fail(Error.New(ex));
    }
  }

  /// <summary>
  /// Load data from a text file using a typed class with LoadColumn attributes and compile-time schema tracking.
  /// </summary>
  /// <typeparam name="TData">The data class type with LoadColumn attributes</typeparam>
  /// <typeparam name="TSchema">The compile-time schema definition</typeparam>
  /// <param name="context">ML.NET context</param>
  /// <param name="path">Path to the text file</param>
  /// <param name="hasHeader">Whether the file has a header row (default: false)</param>
  /// <param name="separatorChar">Column separator character (default: tab)</param>
  /// <param name="allowQuoting">Allow quoted column values (default: false)</param>
  /// <param name="allowSparse">Allow sparse format (default: false)</param>
  /// <returns>Validated DataView with schema tracking, or validation errors</returns>
  public static Fin<DataView<TSchema>> LoadFromTextFile<TData, TSchema>(
      MLContext context,
      string path,
      bool hasHeader = false,
      char separatorChar = '\t',
      bool allowQuoting = false,
      bool allowSparse = false)
      where TData : class
      where TSchema : ISchemaDefinition {
    try {
      var mlView = context.Data.LoadFromTextFile<TData>(
        path,
        hasHeader: hasHeader,
        separatorChar: separatorChar,
        allowQuoting: allowQuoting,
        allowSparse: allowSparse);
      return Fin<DataView<TSchema>>.Succ(DataView<TSchema>.From(mlView));
    } catch (Exception ex) {
      return Fin<DataView<TSchema>>.Fail(Error.New(ex));
    }
  }

  /// <summary>
  /// Wraps an existing IDataView with compile-time schema tracking.
  /// </summary>
  /// <typeparam name="TSchema">The compile-time schema definition</typeparam>
  /// <param name="view">The existing IDataView</param>
  /// <returns>A strongly-typed DataView</returns>
  /// <remarks>
  /// Use this with caution - it assumes the IDataView matches TSchema.
  /// Consider using with validation for safety.
  /// </remarks>
  public static DataView<TSchema> Wrap<TSchema>(IDataView view)
      where TSchema : ISchemaDefinition =>
      DataView<TSchema>.From(view);

  /// <summary>
  /// Split data into train and test sets while preserving schema type information.
  /// </summary>
  /// <typeparam name="TSchema">The schema type</typeparam>
  /// <param name="context">MLContext for the split operation</param>
  /// <param name="data">The data to split</param>
  /// <param name="testFraction">Fraction of data to use for test set (default: 0.2)</param>
  /// <param name="seed">Random seed for reproducible splits (optional)</param>
  /// <returns>Tuple of (TrainSet, TestSet) both with schema type TSchema</returns>
  /// <example>
  /// <code>
  /// var (trainingData, testData) = DataLoader.TrainTestSplit&lt;IrisRawSchema&gt;(
  ///     mlContext,
  ///     fullData,
  ///     testFraction: 0.2
  /// );
  /// </code>
  /// </example>
  public static (DataView<TSchema> TrainSet, DataView<TSchema> TestSet) TrainTestSplit<TSchema>(
      MLContext context,
      DataView<TSchema> data,
      double testFraction = 0.2,
      int? seed = null)
      where TSchema : ISchemaDefinition {
    var split = context.Data.TrainTestSplit(data.Underlying, testFraction: testFraction, seed: seed);
    return (
      DataView<TSchema>.From(split.TrainSet),
      DataView<TSchema>.From(split.TestSet)
    );
  }
}
