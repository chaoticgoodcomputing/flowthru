using Flowthru.Core.Abstractions;

namespace Flowthru.Tests.Kits.Schemas;

/// <summary>
/// Schema exercising <c>byte[]</c> columns. Tier 3 of the property-classification
/// cascade treats <c>byte[]</c> as a single opaque scalar (not an enumerable) — formats
/// claiming <c>SupportsByteArrays</c> must round-trip the bytes intact.
/// </summary>
/// <remarks>
/// JSON serializes <c>byte[]</c> as base64; CSV emits base64 strings; Excel typically
/// stores as cell strings; Parquet uses native binary columns. The fixture's JSON
/// representation uses base64 strings — the kit's JSON-fixture-loader infrastructure
/// drives the canonical interpretation.
/// </remarks>
[FlowthruSchema]
public partial record BinaryBlobSchema
{
  /// <summary>Stable identifier.</summary>
  [SerializedLabel("id")]
  public required System.Guid Id { get; init; }

  /// <summary>Opaque binary payload — typically a serialized blob, image, or document.</summary>
  [SerializedLabel("payload")]
  public required byte[] Payload { get; init; }

  /// <summary>Optional human-readable label alongside the blob.</summary>
  [SerializedLabel("label")]
  public string? Label { get; init; }
}
