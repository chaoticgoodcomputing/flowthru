using Flowthru.Core.Abstractions;

namespace Flowthru.Extensions.Csv.Tests.Fixtures;

/// <summary>Flat row with simple scalar properties — no <c>[SerializedLabel]</c> overrides.</summary>
public class FlatRow : IFlatSchema, ITextSerializable
{
  public int Id { get; set; }
  public string Name { get; set; } = "";
  public double Value { get; set; }

  public override bool Equals(object? obj) =>
    obj is FlatRow other && Id == other.Id && Name == other.Name && Value == other.Value;

  public override int GetHashCode() => HashCode.Combine(Id, Name, Value);
}

/// <summary>Flat row with <c>[SerializedLabel]</c> attributes mapping to snake_case headers.</summary>
public class LabeledRow : IFlatSchema, ITextSerializable
{
  [SerializedLabel("company_id")]
  public int CompanyId { get; set; }

  [SerializedLabel("company_name")]
  public string CompanyName { get; set; } = "";
}
