using System.Text.Json;
using Flowthru.Core.Data.Storage;
using Flowthru.Core.Data.Storage.Format;
using Flowthru.Tests.Helpers.Schemas;

namespace Flowthru.Core.Tests.Services.Serialization;

/// <summary>
/// Shallow tests for <see cref="JsonFormatSerializer{TRow}"/> verifying option propagation
/// and property-mapping configuration. We don't unit-test System.Text.Json internals — just
/// confirm Flowthru's option flags and mapping mode propagate correctly.
/// </summary>
[TestFixture]
[Category("Services")]
[Category("Serialization")]
public class JsonFormatSerializerTests
{
  [Test]
  public void Options_DefaultConstructor_ReturnsConfiguredOptions()
  {
    var serializer = new JsonFormatSerializer<RequiredMembersSchema>();

    Assert.That(serializer.Options, Is.Not.Null);
    // Default options should preserve readability for human inspection
    Assert.That(serializer.Options.WriteIndented, Is.True);
  }

  [Test]
  public void Options_CustomConstructor_PropagatesUserOptions()
  {
    var customOptions = new JsonSerializerOptions { WriteIndented = false };
    var serializer = new JsonFormatSerializer<RequiredMembersSchema>(customOptions);

    Assert.That(serializer.Options, Is.SameAs(customOptions));
    Assert.That(serializer.Options.WriteIndented, Is.False);
  }

  [Test]
  public void GetPropertyMappingConfiguration_UsesSerializedLabelStrategy()
  {
    var serializer = new JsonFormatSerializer<RequiredMembersSchema>();

    var config = serializer.GetPropertyMappingConfiguration();

    // JSON serializer always uses [SerializedLabel] for property mapping
    Assert.That(config.Strategy, Is.EqualTo(PropertyMappingStrategy.SerializedLabel));
    Assert.That(config.SupportsSerializedLabel, Is.True);
  }
}
