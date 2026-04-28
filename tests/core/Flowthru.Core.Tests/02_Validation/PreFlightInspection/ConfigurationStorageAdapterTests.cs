using Flowthru.Core.Data.Storage;
using Flowthru.Core.Data.Validation;
using Flowthru.Tests.Helpers.Adapters;
using Microsoft.Extensions.Configuration;

namespace Flowthru.Core.Tests.Validation.PreFlightInspection;

/// <summary>
/// Coverage tests for <see cref="ConfigurationStorageAdapter{T}"/> via the
/// <see cref="StorageAdapterAssertions"/> harness. Configuration adapters are read-only
/// and bind a configuration section to a POCO.
/// </summary>
[TestFixture]
[Category("Validation")]
[Category("PreFlightInspection")]
public class ConfigurationStorageAdapterTests
{
  public sealed class ConfigOptions
  {
    public string Name { get; set; } = "";
    public int Threshold { get; set; }
  }

  private static IConfiguration BuildConfig(IDictionary<string, string?> values) =>
    new ConfigurationBuilder().AddInMemoryCollection(values).Build();

  [Test]
  public Task InspectShallow_SectionPresent_Succeeds()
  {
    var config = BuildConfig(
      new Dictionary<string, string?>
      {
        ["Flowthru:Test:Name"] = "config",
        ["Flowthru:Test:Threshold"] = "42",
      }
    );
    var adapter = new ConfigurationStorageAdapter<ConfigOptions>("Flowthru:Test", config);

    return StorageAdapterAssertions.InspectShallowSucceeds(adapter);
  }

  [Test]
  public Task InspectShallow_SectionMissing_FailsWithNotFound()
  {
    var config = BuildConfig(new Dictionary<string, string?>());
    var adapter = new ConfigurationStorageAdapter<ConfigOptions>("Flowthru:Missing", config);

    return StorageAdapterAssertions.InspectShallowFails(adapter, ValidationErrorType.NotFound);
  }

  [Test]
  public Task InspectDeep_SectionPresent_Succeeds()
  {
    var config = BuildConfig(
      new Dictionary<string, string?>
      {
        ["Flowthru:Test:Name"] = "config",
        ["Flowthru:Test:Threshold"] = "42",
      }
    );
    var adapter = new ConfigurationStorageAdapter<ConfigOptions>("Flowthru:Test", config);

    return StorageAdapterAssertions.InspectDeepSucceeds(adapter);
  }

  [Test]
  public Task InspectTarget_AlwaysSucceeds()
  {
    var config = BuildConfig(new Dictionary<string, string?>());
    var adapter = new ConfigurationStorageAdapter<ConfigOptions>("Flowthru:Test", config);

    return StorageAdapterAssertions.InspectTargetSucceeds(adapter);
  }

  [Test]
  public Task Exists_SectionPresent_ReturnsTrue()
  {
    var config = BuildConfig(
      new Dictionary<string, string?> { ["Flowthru:Test:Name"] = "x" }
    );
    var adapter = new ConfigurationStorageAdapter<ConfigOptions>("Flowthru:Test", config);

    return StorageAdapterAssertions.ExistsReturns(adapter, expected: true);
  }

  [Test]
  public Task Exists_SectionMissing_ReturnsFalse()
  {
    var config = BuildConfig(new Dictionary<string, string?>());
    var adapter = new ConfigurationStorageAdapter<ConfigOptions>("Flowthru:Missing", config);

    return StorageAdapterAssertions.ExistsReturns(adapter, expected: false);
  }

  [Test]
  public async Task Load_BindsSectionToTypedPoco()
  {
    var config = BuildConfig(
      new Dictionary<string, string?>
      {
        ["Flowthru:Test:Name"] = "loaded",
        ["Flowthru:Test:Threshold"] = "99",
      }
    );
    var adapter = new ConfigurationStorageAdapter<ConfigOptions>("Flowthru:Test", config);

    var result = await adapter.Load().Run();

    Assert.That(result.Name, Is.EqualTo("loaded"));
    Assert.That(result.Threshold, Is.EqualTo(99));
  }

  [Test]
  public void Save_ThrowsNotSupported()
  {
    var config = BuildConfig(new Dictionary<string, string?>());
    var adapter = new ConfigurationStorageAdapter<ConfigOptions>("Flowthru:Test", config);

    Assert.That(
      async () => await adapter.Save(new ConfigOptions()).Run(),
      Throws.TypeOf<NotSupportedException>()
    );
  }
}
