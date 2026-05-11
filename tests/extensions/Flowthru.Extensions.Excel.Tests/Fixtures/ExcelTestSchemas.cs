using Flowthru.Data.Schema;

namespace Flowthru.Extensions.Excel.Tests.Fixtures;

/// <summary>Flat row, simple scalar properties, no <c>[SerializedLabel]</c> overrides.</summary>
[FlowthruSchema]
public partial record ProductRow
{
  public required int Id { get; init; }
  public required string Name { get; init; }
  public required double Price { get; init; }
}

/// <summary>Flat row with <c>[SerializedLabel]</c> attributes mapping to snake_case headers.</summary>
[FlowthruSchema]
public partial record LabeledProductRow
{
  [SerializedLabel("product_id")]
  public required int ProductId { get; init; }

  [SerializedLabel("product_name")]
  public required string ProductName { get; init; }
}

/// <summary>Schema with nullable members exercising the null-sentinel surface.</summary>
[FlowthruSchema]
public partial record NullableProductRow
{
  public required int Id { get; init; }
  public string? OptionalName { get; init; }
  public int? OptionalCount { get; init; }
}

/// <summary>
/// Enum demonstrating <c>[SerializedEnum]</c> mapping. Round-tripping a row that
/// carries this enum exercises the planner-emitted enum mappings consumed by Excel.
/// </summary>
public enum CheckStatus
{
  [SerializedEnum("t")]
  Complete,

  [SerializedEnum("f")]
  Incomplete,
}

/// <summary>Flat row with a <c>[SerializedEnum]</c>-annotated enum property.</summary>
[FlowthruSchema]
public partial record CheckStatusRow
{
  public required Guid Id { get; init; }
  public required CheckStatus Status { get; init; }
}
