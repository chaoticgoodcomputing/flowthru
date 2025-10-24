using System.Text.Json;
using Flowthru.Meta.Models;

namespace Flowthru.Meta;

/// <summary>
/// Extension methods for serializing metadata to JSON.
/// </summary>
public static class MetadataJsonExtensions {
  /// <summary>
  /// Shared JSON serialization options for all metadata.
  /// </summary>
  private static readonly JsonSerializerOptions _jsonOptions = new() {
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    WriteIndented = true,
    DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
    Converters = {
      new System.Text.Json.Serialization.JsonStringEnumConverter(JsonNamingPolicy.CamelCase)
    }
  };

  /// <summary>
  /// Serializes DagMetadata to pretty-printed JSON string.
  /// </summary>
  /// <param name="metadata">The DAG metadata to serialize</param>
  /// <returns>JSON string representation</returns>
  /// <remarks>
  /// <para>
  /// Output format uses:
  /// </para>
  /// <list type="bullet">
  /// <item>camelCase property names (pipelineName, not PipelineName)</item>
  /// <item>Indented formatting for readability</item>
  /// <item>Null properties omitted</item>
  /// <item>Enums serialized as strings</item>
  /// </list>
  /// <para>
  /// This format is optimized for Flowthru.Viz consumption and human readability.
  /// </para>
  /// </remarks>
  public static string ToJson(this DagMetadata metadata) {
    if (metadata == null) {
      throw new ArgumentNullException(nameof(metadata));
    }

    return JsonSerializer.Serialize(metadata, _jsonOptions);
  }

  /// <summary>
  /// Deserializes DagMetadata from JSON string.
  /// </summary>
  /// <param name="json">JSON string to deserialize</param>
  /// <returns>Deserialized DagMetadata object</returns>
  /// <exception cref="JsonException">Thrown if JSON is invalid or doesn't match schema</exception>
  public static DagMetadata FromJson(string json) {
    if (string.IsNullOrWhiteSpace(json)) {
      throw new ArgumentException("JSON string cannot be null or empty", nameof(json));
    }

    var metadata = JsonSerializer.Deserialize<DagMetadata>(json, _jsonOptions);

    if (metadata == null) {
      throw new JsonException("Failed to deserialize DagMetadata from JSON");
    }

    return metadata;
  }

  /// <summary>
  /// Serializes DagMetadata to compact JSON string (no indentation).
  /// </summary>
  /// <param name="metadata">The DAG metadata to serialize</param>
  /// <returns>Compact JSON string representation</returns>
  /// <remarks>
  /// Use this for minimizing file size when human readability is not a concern,
  /// such as API responses or embedded metadata.
  /// </remarks>
  public static string ToCompactJson(this DagMetadata metadata) {
    if (metadata == null) {
      throw new ArgumentNullException(nameof(metadata));
    }

    var compactOptions = new JsonSerializerOptions(_jsonOptions) {
      WriteIndented = false
    };

    return JsonSerializer.Serialize(metadata, compactOptions);
  }
}
