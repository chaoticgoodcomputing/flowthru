using Flowthru.Data.Schema;

namespace Flowthru.Extensions.Parquet.Tests.Fixtures;

/// <summary>Flat row, simple scalar properties, no <c>[SerializedLabel]</c> overrides.</summary>
[FlowthruSchema]
public partial record FlatRow
{
  public required int Id { get; init; }
  public required string Name { get; init; }
  public required double Value { get; init; }
}

/// <summary>Flat row with <c>[SerializedLabel]</c> attributes mapping to snake_case columns.</summary>
[FlowthruSchema]
public partial record LabeledRow
{
  [SerializedLabel("company_id")]
  public required int CompanyId { get; init; }

  [SerializedLabel("company_name")]
  public required string CompanyName { get; init; }
}

/// <summary>Schema with nullable members exercising the null-contract enforcement path.</summary>
[FlowthruSchema]
public partial record NullableRow
{
  public required int Id { get; init; }
  public string? OptionalName { get; init; }
  public int? OptionalCount { get; init; }
}

/// <summary>Wide row used by the row-group streaming guardrail to amplify per-row cost.</summary>
[FlowthruSchema]
public partial record PerfRow
{
  [SerializedLabel("id")]
  public required int Id { get; init; }

  [SerializedLabel("name")]
  public required string Name { get; init; }

  [SerializedLabel("score")]
  public required double Score { get; init; }
}
