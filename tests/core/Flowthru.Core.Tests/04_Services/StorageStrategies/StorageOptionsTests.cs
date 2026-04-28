using Flowthru.Core.Data.Storage.Strategies;

namespace Flowthru.Core.Tests.Services.StorageStrategies;

/// <summary>
/// Tests for the small fluent-builder methods on storage option types. These are public API
/// surface used by extension authors to wire up custom storage strategies.
/// </summary>
[TestFixture]
[Category("Services")]
[Category("StorageStrategies")]
public class StorageOptionsTests
{
  [Test]
  public void WithPath_StaticFactory_ProducesOptionsWithSpecifiedPath()
  {
    var options = StorageOptions.WithPath("data/file.csv");

    Assert.That(options.Path, Is.EqualTo("data/file.csv"));
  }
}
