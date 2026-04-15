using System.Text.Json.Serialization;

namespace Flowthru.Core.Graph.Meta.Models;

/// <summary>
/// Metadata describing an edge in the pipeline DAG.
/// </summary>
/// <remarks>
/// <para>
/// Edges represent data Flow between catalog entries and nodes. The DAG contains
/// two types of edges:
/// </para>
/// <list type="bullet">
/// <item><strong>Catalog → Step:</strong> A node reads from a catalog entry</item>
/// <item><strong>Step → Catalog:</strong> A node writes to a catalog entry</item>
/// </list>
/// <para>
/// Together, these edges form the complete data flow:
/// <c>Item → Step → Item → Step → ...</c>
/// </para>
/// </remarks>
public class EdgeMetadata
{
  /// <summary>
  /// Source identifier (either a catalog entry key or node ID).
  /// </summary>
  /// <remarks>
  /// For Catalog → Step edges, this is a catalog entry key.
  /// For Step → Catalog edges, this is a node ID.
  /// </remarks>
  [JsonPropertyName("source")]
  public required string Source { get; init; }

  /// <summary>
  /// Target identifier (either a node ID or catalog entry key).
  /// </summary>
  /// <remarks>
  /// For Catalog → Step edges, this is a node ID.
  /// For Step → Catalog edges, this is a catalog entry key.
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
