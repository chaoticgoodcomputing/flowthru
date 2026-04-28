using Flowthru.Core.Abstractions;

namespace Flowthru.Tests.Helpers.Schemas;

/// <summary>
/// Test schema demonstrating required properties support.
/// </summary>
/// <remarks>
/// This schema uses C# 11 required members to enforce that critical identity
/// fields must be set when constructing instances. The compiler prevents
/// nodes from forgetting to set these fields. Lands on
/// <c>SchemaActivator.CreateUninitializedObject</c>'s slow path during deserialization.
/// </remarks>
[FlowthruSchema]
public partial record RequiredMembersSchema
{
  /// <summary>Unique identifier for this record — must be set during construction.</summary>
  [SerializedLabel("id")]
  public required Guid Id { get; init; }

  /// <summary>Name field — required, cannot be null.</summary>
  [SerializedLabel("name")]
  public required string Name { get; init; }

  /// <summary>Value field — required, always has a value.</summary>
  [SerializedLabel("value")]
  public required int Value { get; init; }

  /// <summary>Optional timestamp — can be null.</summary>
  [SerializedLabel("timestamp")]
  public DateTime? Timestamp { get; init; }

  /// <summary>Optional description — can be null.</summary>
  [SerializedLabel("description")]
  public string? Description { get; init; }
}

/// <summary>
/// Test schema demonstrating positional record support.
/// </summary>
/// <remarks>
/// Uses primary constructor syntax which requires all parameters to be provided at
/// construction time, enforcing completeness via constructor signature. Also lands
/// on the <c>SchemaActivator</c> slow path.
/// </remarks>
public record PositionalRecordSchema(
  [property: SerializedLabel("entity_id")] Guid EntityId,
  [property: SerializedLabel("entity_name")] string EntityName,
  [property: SerializedLabel("score")] double Score,
  [property: SerializedLabel("created_at")] DateTime CreatedAt
) : IFlatSchema, IBinarySerializable, ITextSerializable, IStructuredSerializable;

/// <summary>
/// Test schema demonstrating mixed required and optional properties.
/// </summary>
/// <remarks>
/// Combines required members (identity fields) with optional members (metadata fields),
/// allowing flexible data while enforcing critical invariants.
/// </remarks>
[FlowthruSchema]
public partial record MixedRequirementsSchema
{
  // Required — critical identity fields
  [SerializedLabel("record_id")]
  public required Guid RecordId { get; init; }

  [SerializedLabel("category")]
  public required string Category { get; init; }

  // Optional — metadata fields
  [SerializedLabel("tags")]
  public string? Tags { get; init; }

  [SerializedLabel("priority")]
  public int? Priority { get; init; }

  [SerializedLabel("is_active")]
  public bool IsActive { get; init; } // Not required, defaults to false
}

/// <summary>
/// Traditional schema without required members for comparison.
/// </summary>
/// <remarks>
/// Uses the traditional approach with parameterless constructor and all optional
/// properties. Lands on <c>SchemaActivator</c>'s fast path.
/// </remarks>
[FlowthruSchema]
public partial record TraditionalSchema
{
  [SerializedLabel("id")]
  public Guid Id { get; init; }

  [SerializedLabel("name")]
  public string? Name { get; init; }

  [SerializedLabel("value")]
  public int Value { get; init; }
}
