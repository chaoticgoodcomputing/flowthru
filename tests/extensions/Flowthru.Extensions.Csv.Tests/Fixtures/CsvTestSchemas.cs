using Flowthru.Data.Schema;

namespace Flowthru.Extensions.Csv.Tests.Fixtures;

/// <summary>Flat row, simple scalar properties, no <c>[SerializedLabel]</c> overrides.</summary>
[FlowthruSchema]
public partial record FlatRow
{
  public required int Id { get; init; }
  public required string Name { get; init; }
  public required double Value { get; init; }
}

/// <summary>Flat row with <c>[SerializedLabel]</c> attributes mapping to snake_case headers.</summary>
[FlowthruSchema]
public partial record LabeledRow
{
  [SerializedLabel("company_id")]
  public required int CompanyId { get; init; }

  [SerializedLabel("company_name")]
  public required string CompanyName { get; init; }
}

/// <summary>Schema with both nullable and non-nullable members exercising the null-sentinel surface.</summary>
[FlowthruSchema]
public partial record NullableRow
{
  [SerializedLabel("id")]
  public required int Id { get; init; }

  [SerializedLabel("nullable_name")]
  public string? NullableName { get; init; }

  [SerializedLabel("non_nullable_name")]
  public string NonNullableName { get; init; } = string.Empty;

  [SerializedLabel("nullable_value")]
  public int? NullableValue { get; init; }
}

/// <summary>
/// Enum demonstrating <c>[SerializedEnum]</c> mapping. Round-tripping a row that
/// carries this enum exercises the converter, the metadata cache, and the planner's
/// per-property dispatch.
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
  public required int Id { get; init; }
  public required CheckStatus Status { get; init; }
}
