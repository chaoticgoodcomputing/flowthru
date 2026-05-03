using Flowthru.Core.Abstractions;

namespace Flowthru.Tests.Kits.Schemas;

// ─────────────────────────────────────────────────────────────────────────────
// Nested-row test schemas
//
// These exercise the [FlowthruSchema] classifier's INestedSchema branch — schemas
// containing object-typed properties or collections that cannot be flattened to a
// single primitive cell. JSON, Parquet, and XML claim SupportsNested; CSV and Excel
// reject these schemas at compile time via the IFlatSchema generic constraint.
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>
/// Nested address structure used as a sub-property of <see cref="NestedSimpleSchema"/>.
/// The sub-schema itself is flat, but its presence as a property makes the parent
/// nested.
/// </summary>
[FlowthruSchema]
public partial record AddressSchema
{
  [SerializedLabel("street")]
  public required string Street { get; init; }

  [SerializedLabel("city")]
  public required string City { get; init; }

  [SerializedLabel("postal_code")]
  public required string PostalCode { get; init; }
}

/// <summary>
/// Schema with a single nested object property. The canonical "flat schema referenced
/// inside another schema" case from
/// <c>docs/guides/customizing-schema-property-types.md</c>.
/// </summary>
[FlowthruSchema]
public partial record NestedSimpleSchema
{
  [SerializedLabel("name")]
  public required string Name { get; init; }

  [SerializedLabel("address")]
  public required AddressSchema Address { get; init; }
}

/// <summary>
/// Schema with an optional nested object — verifies that a nullable nested-object
/// reference round-trips both with and without the inner record present.
/// </summary>
[FlowthruSchema]
public partial record NestedOptionalSchema
{
  [SerializedLabel("name")]
  public required string Name { get; init; }

  [SerializedLabel("billing_address")]
  public AddressSchema? BillingAddress { get; init; }
}

/// <summary>
/// Schema with an array column — the canonical collection-of-primitives case. Formats
/// claiming <c>SupportsNested</c> must round-trip the array contents.
/// </summary>
[FlowthruSchema]
public partial record NestedArraySchema
{
  [SerializedLabel("name")]
  public required string Name { get; init; }

  [SerializedLabel("tags")]
  public required string[] Tags { get; init; }
}

/// <summary>
/// Schema combining nested objects with IScalar wrappers — verifies the planner's
/// IScalar handling composes correctly inside nested-row formats. Closes the
/// "Nested IScalar" cell of the row-shape feature surface table.
/// </summary>
[FlowthruSchema]
public partial record NestedIScalarSchema
{
  [SerializedLabel("customer_id")]
  public required CustomerId CustomerId { get; init; }

  [SerializedLabel("address")]
  public required AddressSchema Address { get; init; }
}
