using Flowthru.Cli;
using Flowthru.Flow;

namespace Flowthru.Cli.Tests;

[TestFixture]
public class ArgumentParserTests
{
  [Test]
  public void Empty_ReturnsDefaults()
  {
    var parsed = ArgumentParser.Parse(Array.Empty<string>());
    Assert.Multiple(() =>
    {
      Assert.That(parsed.FlowLabel, Is.Null);
      Assert.That(parsed.Options.DryRun, Is.EqualTo(DryRunOption.Off));
      Assert.That(parsed.Options.ValidationDepth, Is.EqualTo(ValidationDepth.Shallow));
      Assert.That(parsed.Options.StopOnFirstError, Is.True);
      Assert.That(parsed.ListFlows, Is.False);
      Assert.That(parsed.ShowHelp, Is.False);
    });
  }

  [Test]
  public void FlowFlag_AssignsLabel()
  {
    var parsed = ArgumentParser.Parse(new[] { "--flow", "data-science" });
    Assert.That(parsed.FlowLabel, Is.EqualTo("data-science"));
  }

  [Test]
  public void DryRunFlag_FlipsExecutionOption()
  {
    var parsed = ArgumentParser.Parse(new[] { "--dry-run" });
    Assert.That(parsed.Options.DryRun, Is.EqualTo(DryRunOption.On));
  }

  [Test]
  public void ValidationDepthFlag_AcceptsAllThreeLevels()
  {
    Assert.That(ArgumentParser.Parse(new[] { "--validation-depth", "none" }).Options.ValidationDepth,
      Is.EqualTo(ValidationDepth.None));
    Assert.That(ArgumentParser.Parse(new[] { "--validation-depth", "shallow" }).Options.ValidationDepth,
      Is.EqualTo(ValidationDepth.Shallow));
    Assert.That(ArgumentParser.Parse(new[] { "--validation-depth", "deep" }).Options.ValidationDepth,
      Is.EqualTo(ValidationDepth.Deep));
  }

  [Test]
  public void ContinueOnError_FlipsStopOnFirstError()
  {
    var parsed = ArgumentParser.Parse(new[] { "--continue-on-error" });
    Assert.That(parsed.Options.StopOnFirstError, Is.False);
  }

  [Test]
  public void List_TogglesListFlows()
  {
    var parsed = ArgumentParser.Parse(new[] { "--list" });
    Assert.That(parsed.ListFlows, Is.True);
  }

  [Test]
  public void HelpAndShortAlias_BothSetShowHelp()
  {
    Assert.That(ArgumentParser.Parse(new[] { "--help" }).ShowHelp, Is.True);
    Assert.That(ArgumentParser.Parse(new[] { "-h" }).ShowHelp, Is.True);
  }

  [Test]
  public void UnknownFlag_Throws()
  {
    Assert.Throws<ArgumentException>(() => ArgumentParser.Parse(new[] { "--bogus" }));
  }

  [Test]
  public void FlowWithoutValue_Throws()
  {
    Assert.Throws<ArgumentException>(() => ArgumentParser.Parse(new[] { "--flow" }));
  }

  [Test]
  public void UnknownValidationDepthLevel_Throws()
  {
    Assert.Throws<ArgumentException>(() =>
      ArgumentParser.Parse(new[] { "--validation-depth", "exhaustive" })
    );
  }

  // ── Slice flags ─────────────────────────────────────────────────────

  [Test]
  public void FromFlag_ProducesFromStrategy()
  {
    var args = ArgumentParser.Parse(new[] { "--from", "step.A" });
    Assert.That(args.FlowLabel, Is.Null,
      "--from should not populate FlowLabel; it builds a Slice.");
    Assert.That(args.Slice, Is.InstanceOf<Flowthru.Flow.FlowSliceStrategy.From>());
    var from = (Flowthru.Flow.FlowSliceStrategy.From)args.Slice!;
    Assert.That(from.LabelPatterns, Is.EquivalentTo(new[] { "step.A" }));
  }

  [Test]
  public void ToFlag_ProducesToStrategy()
  {
    var args = ArgumentParser.Parse(new[] { "--to", "data.final" });
    Assert.That(args.Slice, Is.InstanceOf<Flowthru.Flow.FlowSliceStrategy.To>());
    var to = (Flowthru.Flow.FlowSliceStrategy.To)args.Slice!;
    Assert.That(to.LabelPatterns, Is.EquivalentTo(new[] { "data.final" }));
  }

  [Test]
  public void OnlyFlag_ProducesOnlyStrategy()
  {
    var args = ArgumentParser.Parse(new[] { "--only", "compute" });
    Assert.That(args.Slice, Is.InstanceOf<Flowthru.Flow.FlowSliceStrategy.Only>());
    var only = (Flowthru.Flow.FlowSliceStrategy.Only)args.Slice!;
    Assert.That(only.LabelPatterns, Is.EquivalentTo(new[] { "compute" }));
  }

  [Test]
  public void SliceFlag_CommaSeparatedLabels_ParseAsMultipleLabels()
  {
    var args = ArgumentParser.Parse(new[] { "--only", "A,B,C" });
    var only = (Flowthru.Flow.FlowSliceStrategy.Only)args.Slice!;
    Assert.That(only.LabelPatterns, Is.EquivalentTo(new[] { "A", "B", "C" }),
      "Comma-separated label list must split into individual entries.");
  }

  [Test]
  public void SliceFlag_RepeatedFlag_UnionsLabels()
  {
    var args = ArgumentParser.Parse(new[] { "--from", "X", "--from", "Y" });
    var from = (Flowthru.Flow.FlowSliceStrategy.From)args.Slice!;
    Assert.That(from.LabelPatterns, Is.EquivalentTo(new[] { "X", "Y" }),
      "Repeated --from contributes to the same From() set (union within the primitive).");
  }

  [Test]
  public void SliceFlag_DifferentTypes_ComposeViaAnd()
  {
    var args = ArgumentParser.Parse(new[] { "--from", "step.A", "--only", "transform.*" });
    Assert.That(args.Slice, Is.InstanceOf<Flowthru.Flow.FlowSliceStrategy.And>(),
      "Combining --from with --only composes via And (intersection).");
    var and = (Flowthru.Flow.FlowSliceStrategy.And)args.Slice!;
    Assert.That(and.A, Is.InstanceOf<Flowthru.Flow.FlowSliceStrategy.From>());
    Assert.That(and.B, Is.InstanceOf<Flowthru.Flow.FlowSliceStrategy.Only>());
  }

  [Test]
  public void FlowAndSliceFlag_TogetherThrows()
  {
    Assert.Throws<ArgumentException>(() =>
      ArgumentParser.Parse(new[] { "--flow", "Reporting", "--from", "step.A" }),
      "--flow and slice flags are mutually exclusive — the parser must reject the combination."
    );
  }

  [Test]
  public void NoSliceFlags_SliceIsNull()
  {
    var args = ArgumentParser.Parse(new[] { "--flow", "Reporting" });
    Assert.That(args.Slice, Is.Null,
      "Without --from/--to/--only, Slice is null (the CLI takes the --flow path).");
  }

  [Test]
  public void FromFlag_WithoutValue_Throws()
  {
    Assert.Throws<ArgumentException>(() => ArgumentParser.Parse(new[] { "--from" }));
  }
}
