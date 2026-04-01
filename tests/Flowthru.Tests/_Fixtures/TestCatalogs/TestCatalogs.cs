using Flowthru.Data;

namespace Flowthru.Tests.Fixtures.TestCatalogs;

/// <summary>
/// Simple test catalog with three sequential processing steps.
/// </summary>
public class SimpleThreeNodeCatalog : DataCatalogBase
{
  public SimpleThreeNodeCatalog()
  {
    InitializeCatalogProperties();
  }

  public ICatalogEntry<IEnumerable<TestData>> Input =>
    GetOrCreateEntry(() => CatalogEntries.Enumerable.Memory<TestData>(label: "input"));

  public ICatalogEntry<IEnumerable<TestData>> StepOne =>
    GetOrCreateEntry(() => CatalogEntries.Enumerable.Memory<TestData>(label: "step_one"));

  public ICatalogEntry<IEnumerable<TestData>> StepTwo =>
    GetOrCreateEntry(() => CatalogEntries.Enumerable.Memory<TestData>(label: "step_two"));

  public ICatalogEntry<IEnumerable<TestData>> Output =>
    GetOrCreateEntry(() => CatalogEntries.Enumerable.Memory<TestData>(label: "output"));
}

/// <summary>
/// Empty catalog for testing edge cases.
/// </summary>
public class EmptyCatalog : DataCatalogBase
{
  public EmptyCatalog()
  {
    InitializeCatalogProperties();
  }
}

/// <summary>
/// Complex catalog with multiple layers for testing DAG construction.
/// </summary>
public class ComplexMultiLayerCatalog : DataCatalogBase
{
  public ComplexMultiLayerCatalog()
  {
    InitializeCatalogProperties();
  }

  // Layer 0 inputs
  public ICatalogEntry<IEnumerable<TestData>> InputA =>
    GetOrCreateEntry(() => CatalogEntries.Enumerable.Memory<TestData>(label: "input_a"));

  public ICatalogEntry<IEnumerable<TestData>> InputB =>
    GetOrCreateEntry(() => CatalogEntries.Enumerable.Memory<TestData>(label: "input_b"));

  public ICatalogEntry<IEnumerable<TestData>> InputC =>
    GetOrCreateEntry(() => CatalogEntries.Enumerable.Memory<TestData>(label: "input_c"));

  // Layer 1 intermediates
  public ICatalogEntry<IEnumerable<TestData>> ProcessedA =>
    GetOrCreateEntry(() => CatalogEntries.Enumerable.Memory<TestData>(label: "processed_a"));

  public ICatalogEntry<IEnumerable<TestData>> ProcessedB =>
    GetOrCreateEntry(() => CatalogEntries.Enumerable.Memory<TestData>(label: "processed_b"));

  // Layer 2 merged
  public ICatalogEntry<IEnumerable<TestData>> Merged =>
    GetOrCreateEntry(() => CatalogEntries.Enumerable.Memory<TestData>(label: "merged"));

  // Layer 3 final
  public ICatalogEntry<IEnumerable<TestData>> Final =>
    GetOrCreateEntry(() => CatalogEntries.Enumerable.Memory<TestData>(label: "final"));
}

/// <summary>
/// Simple test data record.
/// </summary>
public record TestData
{
  public int Id { get; init; }
  public string Name { get; init; } = string.Empty;
  public double Value { get; init; }
}

/// <summary>
/// Upstream catalog for multi-catalog pipeline tests.
/// Owns the raw input entry and an output entry that a downstream catalog bridges.
/// </summary>
public class UpstreamCatalog : DataCatalogBase
{
  public UpstreamCatalog()
  {
    InitializeCatalogProperties();
  }

  public ICatalogEntry<IEnumerable<TestData>> UpstreamInput =>
    GetOrCreateEntry(() => CatalogEntries.Enumerable.Memory<TestData>(label: "upstream_input"));

  public ICatalogEntry<IEnumerable<TestData>> UpstreamOutput =>
    GetOrCreateEntry(() => CatalogEntries.Enumerable.Memory<TestData>(label: "upstream_output"));
}

/// <summary>
/// Downstream catalog for multi-catalog pipeline tests.
/// Owns its own output; bridges to UpstreamCatalog.UpstreamOutput are wired via the pipeline.
/// </summary>
public class DownstreamCatalog : DataCatalogBase
{
  public DownstreamCatalog()
  {
    InitializeCatalogProperties();
  }

  public ICatalogEntry<IEnumerable<TestData>> DownstreamOutput =>
    GetOrCreateEntry(() => CatalogEntries.Enumerable.Memory<TestData>(label: "downstream_output"));
}

/// <summary>
/// Third catalog for 3-arity pipeline tests.
/// </summary>
public class ThirdCatalog : DataCatalogBase
{
  public ThirdCatalog()
  {
    InitializeCatalogProperties();
  }

  public ICatalogEntry<IEnumerable<TestData>> FinalOutput =>
    GetOrCreateEntry(() => CatalogEntries.Enumerable.Memory<TestData>(label: "final_output"));
}

/// <summary>
/// Per-shard catalog for fan-in and UseCatalogs tests.
/// Each instance is keyed by a unique shard label, giving every entry a distinct identity.
/// </summary>
public class ShardCatalog : DataCatalogBase
{
  private readonly string _shardKey;

  public ShardCatalog(string shardKey)
  {
    _shardKey = shardKey;
    InitializeCatalogProperties();
  }

  public ICatalogEntry<IEnumerable<TestData>> ShardData =>
    GetOrCreateEntry(
      () => CatalogEntries.Enumerable.Memory<TestData>(label: $"shard_data_{_shardKey}")
    );
}

/// <summary>
/// Master catalog that aggregates data from multiple ShardCatalogs.
/// </summary>
public class MasterCatalog : DataCatalogBase
{
  public MasterCatalog()
  {
    InitializeCatalogProperties();
  }

  public ICatalogEntry<IEnumerable<TestData>> AllData =>
    GetOrCreateEntry(() => CatalogEntries.Enumerable.Memory<TestData>(label: "all_data"));
}
