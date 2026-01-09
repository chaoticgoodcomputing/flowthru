using Flowthru.Data;
using Flowthru.Integrations.MLNet.Storage;

namespace Flowthru.Data;

public static partial class CatalogEntries
{
  /// <summary>
  /// Factory methods for ML.NET-related catalog entries.
  /// </summary>
  /// <remarks>
  /// <para>
  /// <strong>ML.NET Integration:</strong> Provides catalog entries for ML.NET primitives
  /// like ONNX models and IDataView containers.
  /// </para>
  /// <para>
  /// <strong>Use Cases:</strong>
  /// </para>
  /// <list type="bullet">
  /// <item>Loading pre-trained ONNX models for inference</item>
  /// <item>Working with ML.NET IDataView data structures</item>
  /// </list>
  /// </remarks>
  public static class MLNet
  {
    /// <summary>
    /// Creates a catalog entry for an ONNX model file.
    /// </summary>
    /// <typeparam name="TRow">Row schema type (not used for raw ONNX, included for future extensibility)</typeparam>
    /// <param name="label">Human-readable label for the catalog entry</param>
    /// <param name="filePath">Path to the .onnx model file</param>
    /// <returns>A catalog entry wrapping an ONNX model storage adapter</returns>
    /// <example>
    /// <code>
    /// var entry = CatalogEntries.MLNet.OnnxModel(
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
}
