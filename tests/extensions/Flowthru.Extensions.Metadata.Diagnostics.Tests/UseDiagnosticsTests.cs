using Flowthru.Core.Meta;
using Flowthru.Core.Meta.Providers;
using Flowthru.Meta.Diagnostics;
using Flowthru.Meta.Diagnostics.Providers;

namespace Flowthru.Meta.Diagnostics.Tests;

[TestFixture]
[Category("Diagnostics")]
[Category("Registration")]
public class UseDiagnosticsTests
{
  private static IReadOnlyList<IMetadataProvider> RegisterAndCollect(
    Action<DiagnosticsOptions>? configure = null
  )
  {
    var meta = new FlowthruMetadataBuilder();
    meta.UseDiagnostics(configure);
    // Reflection — Providers is internal and lives in Flowthru.Core. The Diagnostics
    // package isn't InternalsVisibleTo Core, so reach via reflection in the test only.
    var prop = typeof(FlowthruMetadataBuilder).GetProperty(
      "Providers",
      System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic
    );
    return (IReadOnlyList<IMetadataProvider>)prop!.GetValue(meta)!;
  }

  [Test]
  public void UseDiagnostics_Defaults_RegistersTimingsAndSummary()
  {
    var providers = RegisterAndCollect();

    var types = providers.Select(p => p.GetType()).ToList();
    Assert.That(types, Does.Contain(typeof(StepTimingProvider)));
    Assert.That(types, Does.Contain(typeof(RunSummaryProvider)));
    Assert.That(types, Has.None.EqualTo(typeof(RowCountProvider)),
      "RowCounts is opt-in (touches live storage)");
    Assert.That(types, Has.None.EqualTo(typeof(OutputExistenceProvider)),
      "OutputExistence is opt-in");
  }

  [Test]
  public void UseDiagnostics_OptIn_RegistersAllRequestedProviders()
  {
    var providers = RegisterAndCollect(opts =>
    {
      opts.RowCounts.Enabled = true;
      opts.OutputExistence.Enabled = true;
    });

    var types = providers.Select(p => p.GetType()).ToList();
    Assert.That(types, Does.Contain(typeof(StepTimingProvider)));
    Assert.That(types, Does.Contain(typeof(RunSummaryProvider)));
    Assert.That(types, Does.Contain(typeof(RowCountProvider)));
    Assert.That(types, Does.Contain(typeof(OutputExistenceProvider)));
  }

  [Test]
  public void UseDiagnostics_AllDisabled_RegistersNothing()
  {
    var providers = RegisterAndCollect(opts =>
    {
      opts.StepTimings.Enabled = false;
      opts.RunSummary.Enabled = false;
      // RowCounts and OutputExistence default to false already.
    });

    Assert.That(providers, Is.Empty);
  }

  [Test]
  public void AddStepTimings_RegistersOnlyTimingProvider()
  {
    var meta = new FlowthruMetadataBuilder();
    meta.AddStepTimings(opts => opts.SlowThreshold = TimeSpan.FromSeconds(5));

    var prop = typeof(FlowthruMetadataBuilder).GetProperty(
      "Providers",
      System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic
    );
    var providers = (IReadOnlyList<IMetadataProvider>)prop!.GetValue(meta)!;

    Assert.That(providers, Has.Count.EqualTo(1));
    Assert.That(providers[0], Is.TypeOf<StepTimingProvider>());
  }

  [Test]
  public void RowCountOptions_DefaultsAreCostConservative()
  {
    var opts = new RowCountOptions();
    Assert.That(opts.ForceCountAll, Is.False, "Default must not subsidize materialization");
    Assert.That(opts.IncludeOutputs, Is.True);
    Assert.That(opts.IncludeInputs, Is.False);
  }
}
