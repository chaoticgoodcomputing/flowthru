using Flowthru.Data;
using Flowthru.Integrations.MLNet.Storage;

namespace Flowthru.Integrations.MLNet;

/// <summary>
/// Factory methods for creating ML.NET-related catalog entries.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Discovery Pattern:</strong> Import this class alongside <see cref="CatalogEntries"/>
/// for ML.NET-specific catalog entry factory methods.
/// </para>
/// <para>
/// <strong>Use Cases:</strong>
/// </para>
/// <list type="bullet">
/// <item>Loading pre-trained ONNX models for inference</item>
/// <item>Working with ML.NET IDataView data structures</item>
/// </list>
/// <para>
/// <strong>Usage:</strong>
/// </para>
/// <code>
/// using Flowthru.Data;
/// using Flowthru.Integrations.MLNet;
/// 
/// // Core entries
/// var csvEntry = CatalogEntries.Enumerable.Csv&lt;MySchema&gt;("data", "data.csv");
/// 
/// // MLNet entries
/// var modelEntry = CatalogEntriesMLNet.OnnxModel("model", "model.onnx");
/// </code>
/// </remarks>
public static class CatalogEntriesMLNet
{
  /// <summary>
  /// Creates a catalog entry for an ONNX model file.
  /// </summary>
  /// <param name="label">Human-readable label for the catalog entry</param>
  /// <param name="filePath">Path to the .onnx model file</param>
  /// <returns>A catalog entry wrapping an ONNX model storage adapter</returns>
  /// <example>
  /// <code>
  /// var entry = CatalogEntriesMLNet.OnnxModel(
  ///     label: "BertModel",
  ///     filePath: "models/bert-base.onnx"
  /// );
  /// </code>
  /// </example>
  public static ICatalogEntry<byte[]> OnnxModel(string label, string filePath)
  {
    return new CatalogEntry<byte[]>(label, new OnnxModelStorageAdapter(filePath));
  }
}
