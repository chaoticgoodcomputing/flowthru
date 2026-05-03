using Flowthru.Core.Abstractions;

namespace Flowthru.Tests.Kits.Schemas;

// ─────────────────────────────────────────────────────────────────────────────
// IScalar wrapper types under test
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>
/// Strong-typed identifier wrapping <see cref="string"/>. Distinct at the type level
/// from <see cref="ProductId"/> even though both back through string — the contract
/// every format extension must honor when serializing IScalar columns.
/// </summary>
public readonly record struct CustomerId(string Value) : IScalar;

/// <summary>
/// Strong-typed identifier wrapping <see cref="int"/>. Used to verify formats handle
/// IScalar wrappers around numeric backing types, not just strings.
/// </summary>
public readonly record struct ProductId(int Value) : IScalar;

/// <summary>
/// Strong-typed identifier wrapping <see cref="System.Guid"/>. Verifies IScalar
/// wrapping over BCL scalar structs (Tier 4 of the property classifier cascade).
/// </summary>
public readonly record struct OrderRef(System.Guid Value) : IScalar;

// ─────────────────────────────────────────────────────────────────────────────
// Schemas exercising IScalar columns
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>
/// Single-IScalar-column scenario. Verifies the canonical case: a flat row whose
/// identity field is a strong-typed wrapper around a primitive.
/// </summary>
[FlowthruSchema]
public partial record IScalarSchema
{
  /// <summary>Strong-typed customer identifier.</summary>
  [SerializedLabel("customer_id")]
  public required CustomerId CustomerId { get; init; }

  /// <summary>Plain primitive field alongside the IScalar wrapper.</summary>
  [SerializedLabel("amount")]
  public required decimal Amount { get; init; }
}

/// <summary>
/// Multi-IScalar-column scenario. Verifies that distinct IScalar types (string-backed,
/// int-backed, Guid-backed) coexist in a single schema and round-trip independently —
/// the catalog-developer cross-join-guard pattern that motivated the
/// <c>[FlowthruColumn]</c> initiative.
/// </summary>
[FlowthruSchema]
public partial record MultiIScalarSchema
{
  /// <summary>String-backed wrapper.</summary>
  [SerializedLabel("customer_id")]
  public required CustomerId CustomerId { get; init; }

  /// <summary>Int-backed wrapper.</summary>
  [SerializedLabel("product_id")]
  public required ProductId ProductId { get; init; }

  /// <summary>Guid-backed wrapper.</summary>
  [SerializedLabel("order_ref")]
  public required OrderRef OrderRef { get; init; }

  /// <summary>Plain primitive alongside the wrappers.</summary>
  [SerializedLabel("quantity")]
  public required int Quantity { get; init; }
}
