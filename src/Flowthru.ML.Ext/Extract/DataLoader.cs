using LanguageExt;
using LanguageExt.Common;
using Microsoft.ML;
using Microsoft.ML.Data;
using Flowthru.ML.Ext.Core.Schema;

namespace Flowthru.ML.Ext.Extract;

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
}
