using Flowthru.Data;
using Flowthru.Integrations.MLNet.Storage;

namespace Flowthru.Integrations.MLNet;

/// <summary>
/// ML.NET-specific catalog entry factory methods.
/// </summary>
/// <remarks>
/// <para>
/// Extends Flowthru's catalog entry system with ML.NET-specific data types:
/// </para>
/// <list type="bullet">
/// <item>ONNX models for deep learning inference</item>
/// <item>IDataView for ML.NET pipeline integration</item>
/// </list>
/// </remarks>
public static class CatalogEntriesMLNet
{
  /// <summary>
  /// ML.NET-specific catalog entry factories.
  /// </summary>
  public static class MLNet
  {
    /// <summary>
    /// Creates an ONNX model catalog entry for ML.NET inference.
    /// </summary>
    /// <param name="label">Unique catalog label for DAG resolution</param>
    /// <param name="filePath">Path to .onnx model file</param>
    /// <returns>Catalog entry for ONNX model binary data</returns>
    /// <remarks>
    /// <para>
    /// <strong>Use Case:</strong> Pre-trained deep learning models (BERT, ResNet, YOLO, etc.)
    /// </para>
    /// <para>
    /// <strong>Layer:</strong> Typically Layer 0 (seed data) - models provided before pipeline execution
    /// </para>
    /// <para>
    /// <strong>Validation:</strong> Implements IShallowInspectable for early file validation
    /// </para>
    /// <para>
    /// <strong>Read-Only:</strong> Models cannot be written by pipelines
    /// </para>
    /// <para>
    /// <strong>Capabilities:</strong>
    /// </para>
    /// <list type="bullet">
    /// <item>ISeedable: true (models are Layer 0 inputs)</item>
    /// <item>IShallowInspectable: true (validates file before execution)</item>
    /// <item>IReadOnly: true (Save operation throws exception)</item>
    /// </list>
    /// </remarks>
    /// <example>
    /// <code>
    /// public partial class MyCatalog : DataCatalogBase
    /// {
    ///     public ICatalogEntry&lt;byte[]&gt; BertModel =>
    ///         GetOrCreateEntry(() =>
    ///             CatalogEntriesMLNet.MLNet.OnnxModel(
    ///                 label: "BertModel",
    ///                 filePath: $"{_basePath}/Models/bert-base-uncased.onnx"
    ///             )
    ///         );
    /// }
    /// </code>
    /// </example>
    public static ICatalogEntry<byte[]> OnnxModel(string label, string filePath)
    {
      var storage = new OnnxModelStorageAdapter(filePath);
      return new CatalogEntry<byte[]>(label, storage);
    }
  }
}
