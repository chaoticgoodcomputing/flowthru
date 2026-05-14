using Flowthru.Data.Catalog;
using Flowthru.Data.Configuration;
using Flowthru.Prelude;
using Microsoft.Extensions.Configuration;

namespace Flowthru.Core.Tests.Data.Configuration;

/// <summary>
/// Tests the <c>Item.Of&lt;T&gt;("...").FromConfiguration().AtSection("...")</c>
/// builder chain — the catalog-side authoring surface for
/// <see cref="ConfigurationItem{T}"/>. Mirrors the JSON/Csv builders'
/// shape so authors don't learn a new pattern for config-as-catalog.
/// </summary>
[TestFixture]
public class ConfigurationBuilderTests
{
  public sealed class WidgetConfig
  {
    public int Threshold { get; set; }
    public string? Name { get; set; }
  }

  // ── End-to-end builder chain ──────────────────────────────────────────

  [Test]
  public async Task FromConfiguration_AtSection_Build_ProducesLoadableItem()
  {
    var config = new ConfigurationBuilder()
      .AddInMemoryCollection(new Dictionary<string, string?>
      {
        ["Widget:Threshold"] = "42",
        ["Widget:Name"] = "primary",
      })
      .Build();

    var item = Item.Of<WidgetConfig>("widget")
      .FromConfiguration(config)
      .AtSection("Widget")
      .Build();

    Assert.That(item, Is.Not.Null);
    Assert.That(item.Label, Is.EqualTo("widget"));
    Assert.That(item, Is.InstanceOf<IReadOnlyItem<WidgetConfig>>());

    var loaded = await item.Load().Run();
    Assert.That(loaded, Is.InstanceOf<EffResult<WidgetConfig>.Success>());
    var value = ((EffResult<WidgetConfig>.Success)loaded).Value;
    Assert.That(value.Threshold, Is.EqualTo(42));
    Assert.That(value.Name, Is.EqualTo("primary"));
  }

  [Test]
  public void Build_WithoutAtSection_Throws()
  {
    // The builder enforces that AtSection(...) is called before Build()
    // — mirrors the JSON builder's AtPath requirement so missing
    // configuration surfaces at catalog wire-up, not at first Load.
    var config = new ConfigurationBuilder().Build();
    var anchor = Item.Of<WidgetConfig>("widget").FromConfiguration(config);

    Assert.Throws<InvalidOperationException>(() => anchor.Build());
  }

  [Test]
  public void AtSection_NullOrEmpty_Throws()
  {
    var config = new ConfigurationBuilder().Build();
    var builder = Item.Of<WidgetConfig>("widget").FromConfiguration(config);

    Assert.Throws<ArgumentException>(() => builder.AtSection(""));
    Assert.Throws<ArgumentException>(() => builder.AtSection("   "));
    Assert.Throws<ArgumentNullException>(() => builder.AtSection(null!));
  }

  [Test]
  public void FromConfiguration_NullConfig_Throws()
  {
    var anchor = Item.Of<WidgetConfig>("widget");
    Assert.Throws<ArgumentNullException>(() => anchor.FromConfiguration(null!));
  }
}
