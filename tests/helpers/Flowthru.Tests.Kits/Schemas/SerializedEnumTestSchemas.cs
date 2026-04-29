using Flowthru.Core.Abstractions;

namespace Flowthru.Tests.Kits.Schemas;

/// <summary>
/// Test enum demonstrating <c>[SerializedEnum]</c> string-mapping support.
/// </summary>
/// <remarks>
/// Each member declares its serialized string mapping explicitly. Round-tripping a value
/// through any catalog format exercises the full Core enum infrastructure:
/// <c>SerializedEnumJsonConverter</c>, <c>EnumMetadataCache</c>, <c>EnumMetadataRegistry.Create</c>,
/// and <c>EnumSerializationHelper.ParseEnumFromString</c>.
/// </remarks>
public enum CheckStatus
{
  /// <summary>Check has been completed.</summary>
  [SerializedEnum("t")]
  Complete,

  /// <summary>Check has not been completed.</summary>
  [SerializedEnum("f")]
  Incomplete,
}

/// <summary>
/// Larger test enum with more varied serialized representations. Useful for stressing the
/// enum metadata cache with multiple distinct values and for verifying lookups across
/// abbreviated, lowercase, and snake_case mappings.
/// </summary>
public enum Rarity
{
  /// <summary>Common rarity tier.</summary>
  [SerializedEnum("common")]
  Common,

  /// <summary>Uncommon rarity tier.</summary>
  [SerializedEnum("uncommon")]
  Uncommon,

  /// <summary>Rare rarity tier.</summary>
  [SerializedEnum("rare")]
  Rare,

  /// <summary>Mythic rarity tier (snake_case to demonstrate non-trivial mappings).</summary>
  [SerializedEnum("mythic_rare")]
  MythicRare,
}

/// <summary>
/// Test schema with a single enum-valued field. Round-tripping this schema through a JSON
/// catalog item exercises the full <c>[SerializedEnum]</c> chain end-to-end.
/// </summary>
[FlowthruSchema]
public partial record CheckStatusSchema
{
  /// <summary>Stable identifier.</summary>
  [SerializedLabel("id")]
  public required Guid Id { get; init; }

  /// <summary>Status flag using the <see cref="CheckStatus"/> enum.</summary>
  [SerializedLabel("status")]
  public required CheckStatus Status { get; init; }
}

/// <summary>
/// Test schema with multiple enum fields. Verifies the metadata cache handles distinct enum
/// types in the same schema and that converters compose correctly when a row references
/// more than one enum.
/// </summary>
[FlowthruSchema]
public partial record MultiEnumSchema
{
  /// <summary>Stable identifier.</summary>
  [SerializedLabel("id")]
  public required Guid Id { get; init; }

  /// <summary>Primary status flag.</summary>
  [SerializedLabel("primary_status")]
  public required CheckStatus PrimaryStatus { get; init; }

  /// <summary>Secondary status flag.</summary>
  [SerializedLabel("secondary_status")]
  public required CheckStatus SecondaryStatus { get; init; }

  /// <summary>Rarity tier (different enum type).</summary>
  [SerializedLabel("rarity")]
  public required Rarity Rarity { get; init; }
}

/// <summary>
/// Test schema with an optional (nullable) enum field. Verifies serializer handling of
/// null/missing enum values without falling through to a default.
/// </summary>
[FlowthruSchema]
public partial record OptionalEnumSchema
{
  /// <summary>Stable identifier.</summary>
  [SerializedLabel("id")]
  public required Guid Id { get; init; }

  /// <summary>Optional status flag — may be null.</summary>
  [SerializedLabel("status")]
  public CheckStatus? Status { get; init; }
}
