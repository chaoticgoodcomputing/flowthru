using Flowthru.Data;

namespace Flowthru.Tests.Fixtures.TestCatalogs;

/// <summary>
/// Simple test catalog with three sequential processing steps.
/// </summary>
public class SimpleThreeStepCatalog : CatalogAbstract
{
  public SimpleThreeStepCatalog()
  {
    InitializeCatalogProperties();
  }

  public IItem<IEnumerable<TestData>> Input =>
    CreateItem(() => ItemFactory.Enumerable.Memory<TestData>(label: "input"));

  public IItem<IEnumerable<TestData>> StepOne =>
    CreateItem(() => ItemFactory.Enumerable.Memory<TestData>(label: "step_one"));

  public IItem<IEnumerable<TestData>> StepTwo =>
    CreateItem(() => ItemFactory.Enumerable.Memory<TestData>(label: "step_two"));

  public IItem<IEnumerable<TestData>> Output =>
    CreateItem(() => ItemFactory.Enumerable.Memory<TestData>(label: "output"));
}

/// <summary>
/// Empty catalog for testing edge cases.
/// </summary>
public class EmptyCatalog : CatalogAbstract
{
  public EmptyCatalog()
  {
    InitializeCatalogProperties();
  }
}

/// <summary>
/// Complex catalog with multiple layers for testing DAG construction.
/// </summary>
public class ComplexMultiLayerCatalog : CatalogAbstract
{
  public ComplexMultiLayerCatalog()
  {
    InitializeCatalogProperties();
  }

  // Layer 0 inputs
  public IItem<IEnumerable<TestData>> InputA =>
    CreateItem(() => ItemFactory.Enumerable.Memory<TestData>(label: "input_a"));

  public IItem<IEnumerable<TestData>> InputB =>
    CreateItem(() => ItemFactory.Enumerable.Memory<TestData>(label: "input_b"));

  public IItem<IEnumerable<TestData>> InputC =>
    CreateItem(() => ItemFactory.Enumerable.Memory<TestData>(label: "input_c"));

  // Layer 1 intermediates
  public IItem<IEnumerable<TestData>> ProcessedA =>
    CreateItem(() => ItemFactory.Enumerable.Memory<TestData>(label: "processed_a"));

  public IItem<IEnumerable<TestData>> ProcessedB =>
    CreateItem(() => ItemFactory.Enumerable.Memory<TestData>(label: "processed_b"));

  // Layer 2 merged
  public IItem<IEnumerable<TestData>> Merged =>
    CreateItem(() => ItemFactory.Enumerable.Memory<TestData>(label: "merged"));

  // Layer 3 final
  public IItem<IEnumerable<TestData>> Final =>
    CreateItem(() => ItemFactory.Enumerable.Memory<TestData>(label: "final"));
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
public class UpstreamCatalog : CatalogAbstract
{
  public UpstreamCatalog()
  {
    InitializeCatalogProperties();
  }

  public IItem<IEnumerable<TestData>> UpstreamInput =>
    CreateItem(() => ItemFactory.Enumerable.Memory<TestData>(label: "upstream_input"));

  public IItem<IEnumerable<TestData>> UpstreamOutput =>
    CreateItem(() => ItemFactory.Enumerable.Memory<TestData>(label: "upstream_output"));
}

/// <summary>
/// Downstream catalog for multi-catalog pipeline tests.
/// Owns its own output; bridges to UpstreamCatalog.UpstreamOutput are wired via the pipeline.
/// </summary>
public class DownstreamCatalog : CatalogAbstract
{
  public DownstreamCatalog()
  {
    InitializeCatalogProperties();
  }

  public IItem<IEnumerable<TestData>> DownstreamOutput =>
    CreateItem(() => ItemFactory.Enumerable.Memory<TestData>(label: "downstream_output"));
}

/// <summary>
/// Third catalog for 3-arity pipeline tests.
/// </summary>
public class ThirdCatalog : CatalogAbstract
{
  public ThirdCatalog()
  {
    InitializeCatalogProperties();
  }

  public IItem<IEnumerable<TestData>> FinalOutput =>
    CreateItem(() => ItemFactory.Enumerable.Memory<TestData>(label: "final_output"));
}

/// <summary>
/// Per-shard catalog for fan-in and RegisterCatalogs tests.
/// Each instance is keyed by a unique shard label, giving every entry a distinct identity.
/// </summary>
public class ShardCatalog : CatalogAbstract
{
  private readonly string _shardKey;

  public ShardCatalog(string shardKey)
  {
    _shardKey = shardKey;
    InitializeCatalogProperties();
  }

  public IItem<IEnumerable<TestData>> ShardData =>
    CreateItem(() => ItemFactory.Enumerable.Memory<TestData>(label: $"shard_data_{_shardKey}"));
}

/// <summary>
/// Master catalog that aggregates data from multiple ShardCatalogs.
/// </summary>
public class MasterCatalog : CatalogAbstract
{
  public MasterCatalog()
  {
    InitializeCatalogProperties();
  }

  public IItem<IEnumerable<TestData>> AllData =>
    CreateItem(() => ItemFactory.Enumerable.Memory<TestData>(label: "all_data"));
}
