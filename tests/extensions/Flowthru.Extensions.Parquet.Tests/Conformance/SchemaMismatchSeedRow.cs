using Flowthru.Core.Abstractions;

namespace Flowthru.Extensions.Parquet.Tests.Conformance;

/// <summary>
/// Schema deliberately missing the <c>Name</c> field that
/// <see cref="Flowthru.Tests.Kits.Schemas.TraditionalSchema"/> declares. Used by
/// <see cref="ParquetStorageAdapterConformance"/>'s Phase F negative scenario to seed a
/// Parquet file whose on-disk schema diverges from the schema the conformance subject
/// expects, exercising the <see cref="StorageAdapterConformance{T}.CreateAdapterMissingExpectedColumn"/>
/// pre-flight check.
/// </summary>
[FlowthruSchema]
public partial record SchemaMismatchSeedRow
{
  [SerializedLabel("id")]
  public required Guid Id { get; init; }

  [SerializedLabel("value")]
  public required int Value { get; init; }
}
