using System.Text.Json.Serialization;

namespace Flowthru.Meta.Models;

/// <summary>
/// Metadata describing an edge in the pipeline DAG.
/// </summary>
/// <remarks>
/// <para>
/// Edges represent data flow between catalog entries and nodes. The DAG contains
/// two types of edges:
/// </para>
/// <list type="bullet">
/// <item><strong>Catalog → Node:</strong> A node reads from a catalog entry</item>
/// <item><strong>Node → Catalog:</strong> A node writes to a catalog entry</item>
/// </list>
/// <para>
/// Together, these edges form the complete data flow: 
/// <c>CatalogEntry → Node → CatalogEntry → Node → ...</c>
/// </para>
/// </remarks>
public class EdgeMetadata {
  /// <summary>
  /// Source identifier (either a catalog entry key or node ID).
  /// </summary>
  /// <remarks>
  /// For Catalog → Node edges, this is a catalog entry key.
  /// For Node → Catalog edges, this is a node ID.
  /// </remarks>
  [JsonPropertyName("source")]
  public required string Source { get; init; }

  /// <summary>
  /// Target identifier (either a node ID or catalog entry key).
  /// </summary>
  /// <remarks>
  /// For Catalog → Node edges, this is a node ID.
  /// For Node → Catalog edges, this is a catalog entry key.
  /// </remarks>
  [JsonPropertyName("target")]
  public required string Target { get; init; }

  /// <summary>
  /// C# type name of data flowing through this edge.
  /// </summary>
  /// <remarks>
  /// Simple type name without namespace.
  /// Example: "Company", "Shuttle", "ModelInput"
  /// </remarks>
  [JsonPropertyName("dataType")]
  public required string DataType { get; init; }
}
