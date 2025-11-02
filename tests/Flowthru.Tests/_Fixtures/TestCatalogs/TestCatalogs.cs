using Flowthru.Data;
using Flowthru.Data.Implementations;

namespace Flowthru.Tests.Fixtures.TestCatalogs;

/// <summary>
/// Simple test catalog with three sequential processing steps.
/// </summary>
public class SimpleThreeNodeCatalog : DataCatalogBase
{
  public ICatalogEntry<IEnumerable<TestData>> Input =>
    GetOrCreateEntry(() => new MemoryCatalogEntry<IEnumerable<TestData>>("input"));

  public ICatalogEntry<IEnumerable<TestData>> StepOne =>
    GetOrCreateEntry(() => new MemoryCatalogEntry<IEnumerable<TestData>>("step_one"));

  public ICatalogEntry<IEnumerable<TestData>> StepTwo =>
    GetOrCreateEntry(() => new MemoryCatalogEntry<IEnumerable<TestData>>("step_two"));

  public ICatalogEntry<IEnumerable<TestData>> Output =>
    GetOrCreateEntry(() => new MemoryCatalogEntry<IEnumerable<TestData>>("output"));
}

/// <summary>
/// Empty catalog for testing edge cases.
/// </summary>
public class EmptyCatalog : DataCatalogBase { }

/// <summary>
/// Complex catalog with multiple layers for testing DAG construction.
/// </summary>
public class ComplexMultiLayerCatalog : DataCatalogBase
{
  // Layer 0 inputs
  public ICatalogEntry<IEnumerable<TestData>> InputA =>
    GetOrCreateEntry(() => new MemoryCatalogEntry<IEnumerable<TestData>>("input_a"));

  public ICatalogEntry<IEnumerable<TestData>> InputB =>
    GetOrCreateEntry(() => new MemoryCatalogEntry<IEnumerable<TestData>>("input_b"));

  public ICatalogEntry<IEnumerable<TestData>> InputC =>
    GetOrCreateEntry(() => new MemoryCatalogEntry<IEnumerable<TestData>>("input_c"));

  // Layer 1 intermediates
  public ICatalogEntry<IEnumerable<TestData>> ProcessedA =>
    GetOrCreateEntry(() => new MemoryCatalogEntry<IEnumerable<TestData>>("processed_a"));

  public ICatalogEntry<IEnumerable<TestData>> ProcessedB =>
    GetOrCreateEntry(() => new MemoryCatalogEntry<IEnumerable<TestData>>("processed_b"));

  // Layer 2 merged
  public ICatalogEntry<IEnumerable<TestData>> Merged =>
    GetOrCreateEntry(() => new MemoryCatalogEntry<IEnumerable<TestData>>("merged"));

  // Layer 3 final
  public ICatalogEntry<IEnumerable<TestData>> Final =>
    GetOrCreateEntry(() => new MemoryCatalogEntry<IEnumerable<TestData>>("final"));
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
