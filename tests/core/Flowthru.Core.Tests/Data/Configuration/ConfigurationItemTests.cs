using Flowthru.Data.Catalog;
using Flowthru.Data.Catalog.Configuration;
using Flowthru.Prelude;
using Microsoft.Extensions.Configuration;

namespace Flowthru.Core.Tests.Data.Configuration;

/// <summary>
/// Unit tests for <see cref="ConfigurationItem{T}"/> — the read-only
/// catalog item backed by an <see cref="IConfigurationSection"/>. Per
/// the Phase 5 RFC, a configuration item:
/// <list type="bullet">
///   <item>Round-trips a bound type through <see cref="ConfigurationItem{T}.Load"/>.</item>
///   <item>Fails deterministically on <see cref="ConfigurationItem{T}.Save"/> — config items are read-only.</item>
///   <item>Exposes a fingerprint that is stable across reads and sensitive to
///         value changes (consumed by Phase 6's cache plan).</item>
/// </list>
/// </summary>
[TestFixture]
public class ConfigurationItemTests
{
  /// <summary>
  /// Strongly-typed config payload. <see cref="ConfigurationBinder.Get{T}(IConfiguration)"/>
  /// requires a parameterless constructor and settable properties.
  /// </summary>
  public sealed class FeatureFlagsConfig
  {
    public bool UseV2 { get; set; }
    public int RetryCount { get; set; }
    public string? Region { get; set; }
  }

  private static IConfiguration BuildConfig(Dictionary<string, string?> values) =>
    new ConfigurationBuilder().AddInMemoryCollection(values).Build();

  // ── Load() round-trip ─────────────────────────────────────────────────

  [Test]
  public async Task Load_BindsSectionToTypedValue()
  {
    var config = BuildConfig(new Dictionary<string, string?>
    {
      ["FeatureFlags:UseV2"] = "true",
      ["FeatureFlags:RetryCount"] = "5",
      ["FeatureFlags:Region"] = "us-east-1",
    });

    var item = new ConfigurationItem<FeatureFlagsConfig>("flags", config.GetSection("FeatureFlags"));

    var loaded = await item.Load().Run();
    Assert.That(loaded, Is.InstanceOf<EffResult<FeatureFlagsConfig>.Success>(),
      $"Load failed: {(loaded as EffResult<FeatureFlagsConfig>.Failure)?.Error.Message}");

    var value = ((EffResult<FeatureFlagsConfig>.Success)loaded).Value;
    Assert.That(value.UseV2, Is.True);
    Assert.That(value.RetryCount, Is.EqualTo(5));
    Assert.That(value.Region, Is.EqualTo("us-east-1"));
  }

  [Test]
  public async Task Load_MissingSection_ReturnsDefaultInstance()
  {
    // A missing section binds to a fresh T (new T()); this matches
    // IConfiguration.Get<T>() returning null and the item filling in
    // the default. Authors who want to surface "missing config" as a
    // failure should compose with Exists() in pre-flight.
    var config = new ConfigurationBuilder().Build();
    var item = new ConfigurationItem<FeatureFlagsConfig>("flags", config.GetSection("Missing"));

    var loaded = await item.Load().Run();
    Assert.That(loaded, Is.InstanceOf<EffResult<FeatureFlagsConfig>.Success>());

    var value = ((EffResult<FeatureFlagsConfig>.Success)loaded).Value;
    Assert.That(value.UseV2, Is.False);
    Assert.That(value.RetryCount, Is.EqualTo(0));
    Assert.That(value.Region, Is.Null);
  }

  // ── Save() read-only enforcement ──────────────────────────────────────

  [Test]
  public async Task Save_AlwaysFails_WithReadOnlyDiagnostic()
  {
    var config = BuildConfig(new Dictionary<string, string?>
    {
      ["FeatureFlags:UseV2"] = "true",
    });
    var item = new ConfigurationItem<FeatureFlagsConfig>("flags", config.GetSection("FeatureFlags"));

    var result = await item.Save(new FeatureFlagsConfig { UseV2 = false }).Run();

    Assert.That(result, Is.InstanceOf<EffResult<FlowUnit>.Failure>(),
      "Save on a ConfigurationItem must always fail — configuration is read-only.");

    var error = ((EffResult<FlowUnit>.Failure)result).Error;
    Assert.That(error.Message, Does.Contain("read-only").IgnoreCase,
      "Diagnostic must call out the read-only nature of configuration items.");
  }

  // ── Exists() reflects the underlying section ──────────────────────────

  [Test]
  public async Task Exists_ReturnsTrue_ForPopulatedSection()
  {
    var config = BuildConfig(new Dictionary<string, string?>
    {
      ["FeatureFlags:UseV2"] = "true",
    });
    var item = new ConfigurationItem<FeatureFlagsConfig>("flags", config.GetSection("FeatureFlags"));

    var exists = await item.Exists().Run();
    Assert.That(exists, Is.InstanceOf<EffResult<bool>.Success>());
    Assert.That(((EffResult<bool>.Success)exists).Value, Is.True);
  }

  [Test]
  public async Task Exists_ReturnsFalse_ForMissingSection()
  {
    var config = new ConfigurationBuilder().Build();
    var item = new ConfigurationItem<FeatureFlagsConfig>("flags", config.GetSection("Missing"));

    var exists = await item.Exists().Run();
    Assert.That(exists, Is.InstanceOf<EffResult<bool>.Success>());
    Assert.That(((EffResult<bool>.Success)exists).Value, Is.False);
  }

  // ── TryGetFingerprint() stability + sensitivity ───────────────────────

  [Test]
  public void TryGetFingerprint_IsNonNull()
  {
    // ConfigurationItem opts into fingerprinting — Phase 6's cache plan
    // consumes this to invalidate downstream steps when config changes.
    var config = BuildConfig(new Dictionary<string, string?>
    {
      ["FeatureFlags:UseV2"] = "true",
    });
    var item = new ConfigurationItem<FeatureFlagsConfig>("flags", config.GetSection("FeatureFlags"));

    var fingerprint = item.TryGetFingerprint();
    Assert.That(fingerprint, Is.Not.Null,
      "ConfigurationItem must surface a fingerprint to participate in cache planning.");
  }

  [Test]
  public async Task TryGetFingerprint_IsStableAcrossReads()
  {
    // Two consecutive fingerprint computations on the same unchanging
    // section must return the same value — otherwise cache plans would
    // false-miss on every run.
    var config = BuildConfig(new Dictionary<string, string?>
    {
      ["FeatureFlags:UseV2"] = "true",
      ["FeatureFlags:RetryCount"] = "3",
    });
    var item = new ConfigurationItem<FeatureFlagsConfig>("flags", config.GetSection("FeatureFlags"));

    var firstResult = await item.TryGetFingerprint()!.Run();
    var secondResult = await item.TryGetFingerprint()!.Run();
    var first = ((EffResult<string>.Success)firstResult).Value;
    var second = ((EffResult<string>.Success)secondResult).Value;

    Assert.That(second, Is.EqualTo(first),
      "Repeat fingerprint reads on an unchanged section must be stable.");
  }

  [Test]
  public async Task TryGetFingerprint_IsSensitiveToValueChange()
  {
    // The whole point of the fingerprint: a config-value change must
    // produce a different fingerprint so Phase 6 can invalidate cache.
    var itemBefore = new ConfigurationItem<FeatureFlagsConfig>(
      "flags",
      BuildConfig(new Dictionary<string, string?>
      {
        ["FeatureFlags:UseV2"] = "true",
      }).GetSection("FeatureFlags"));

    var itemAfter = new ConfigurationItem<FeatureFlagsConfig>(
      "flags",
      BuildConfig(new Dictionary<string, string?>
      {
        ["FeatureFlags:UseV2"] = "false",
      }).GetSection("FeatureFlags"));

    var beforeResult = await itemBefore.TryGetFingerprint()!.Run();
    var afterResult = await itemAfter.TryGetFingerprint()!.Run();
    var before = ((EffResult<string>.Success)beforeResult).Value;
    var after = ((EffResult<string>.Success)afterResult).Value;

    Assert.That(after, Is.Not.EqualTo(before),
      "Fingerprint must change when a config value changes — otherwise downstream cache won't invalidate.");
  }

  [Test]
  public async Task TryGetFingerprint_IsSensitiveToNewKey()
  {
    var itemBefore = new ConfigurationItem<FeatureFlagsConfig>(
      "flags",
      BuildConfig(new Dictionary<string, string?>
      {
        ["FeatureFlags:UseV2"] = "true",
      }).GetSection("FeatureFlags"));

    var itemAfter = new ConfigurationItem<FeatureFlagsConfig>(
      "flags",
      BuildConfig(new Dictionary<string, string?>
      {
        ["FeatureFlags:UseV2"] = "true",
        ["FeatureFlags:RetryCount"] = "7",
      }).GetSection("FeatureFlags"));

    var beforeResult = await itemBefore.TryGetFingerprint()!.Run();
    var afterResult = await itemAfter.TryGetFingerprint()!.Run();
    var before = ((EffResult<string>.Success)beforeResult).Value;
    var after = ((EffResult<string>.Success)afterResult).Value;

    Assert.That(after, Is.Not.EqualTo(before),
      "Adding a new key under the section must change the fingerprint.");
  }

  // ── IReadOnlyItem marker ─────────────────────────────────────────────

  [Test]
  public void ConfigurationItem_ImplementsIReadOnlyItem()
  {
    // The marker is what the output-rejection analyzer keys on at the
    // output position of a step. Confirm that ConfigurationItem<T> carries it.
    var config = new ConfigurationBuilder().Build();
    var item = new ConfigurationItem<FeatureFlagsConfig>("flags", config.GetSection("any"));

    Assert.That(item, Is.AssignableTo<IReadOnlyItem<FeatureFlagsConfig>>(),
      "ConfigurationItem<T> must implement IReadOnlyItem<T> so the output-rejection analyzer can recognize it.");
  }

  [Test]
  public void Label_ReflectsConstructorArgument()
  {
    var config = new ConfigurationBuilder().Build();
    var item = new ConfigurationItem<FeatureFlagsConfig>("my-flags", config.GetSection("FeatureFlags"));
    Assert.That(item.Label, Is.EqualTo("my-flags"));
  }

  [Test]
  public void DataType_IsBoundType()
  {
    var config = new ConfigurationBuilder().Build();
    IItem item = new ConfigurationItem<FeatureFlagsConfig>("flags", config.GetSection("FeatureFlags"));
    Assert.That(item.DataType, Is.EqualTo(typeof(FeatureFlagsConfig)));
  }
}
