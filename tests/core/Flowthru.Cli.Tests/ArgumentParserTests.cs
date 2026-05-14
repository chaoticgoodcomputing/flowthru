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
  public void NoCache_FlipsBypassCacheReads()
  {
    var parsed = ArgumentParser.Parse(new[] { "--no-cache" });
    Assert.That(parsed.Options.BypassCacheReads, Is.True,
      "--no-cache must set ExecutionOptions.BypassCacheReads so the FlowthruService "
      + "skips the cache plan build but still writes new composites post-run.");
  }

  [Test]
  public void NoCache_DefaultIsFalse()
  {
    var parsed = ArgumentParser.Parse(Array.Empty<string>());
    Assert.That(parsed.Options.BypassCacheReads, Is.False,
      "Without --no-cache the run should consult the cache as normal.");
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

  // ── --exclude flag (Phase 2 — slice-algebra Not) ────────────────────────

  [Test]
  public void ExcludeFlag_Alone_BuildsNotAroundOnly()
  {
    // --exclude alone composes with the implicit "rest" = All, so the
    // resulting strategy tree is And(All, Not(Only(labels))).
    var args = ArgumentParser.Parse(new[] { "--exclude", "step.A" });
    Assert.That(args.Slice, Is.InstanceOf<Flowthru.Flow.FlowSliceStrategy.And>(),
      "--exclude composes against the implicit base via And(rest, Not(...)).");
    var and = (Flowthru.Flow.FlowSliceStrategy.And)args.Slice!;
    Assert.That(and.A, Is.InstanceOf<Flowthru.Flow.FlowSliceStrategy.All>(),
      "With no other slice flags, the LHS of And is FlowSliceStrategy.All.");
    Assert.That(and.B, Is.InstanceOf<Flowthru.Flow.FlowSliceStrategy.Not>());
    var not = (Flowthru.Flow.FlowSliceStrategy.Not)and.B;
    Assert.That(not.Inner, Is.InstanceOf<Flowthru.Flow.FlowSliceStrategy.Only>(),
      "A bare label inside --exclude becomes an Only matcher inside the Not.");
    var only = (Flowthru.Flow.FlowSliceStrategy.Only)not.Inner;
    Assert.That(only.LabelPatterns, Is.EquivalentTo(new[] { "step.A" }));
  }

  [Test]
  public void ExcludeFlag_FlowsPrefix_DispatchesToFlowsMatcher()
  {
    // `flows:X` inside --exclude resolves to FlowSliceStrategy.Flows
    // rather than the label-glob Only matcher.
    var args = ArgumentParser.Parse(new[] { "--exclude", "flows:Ingest" });
    var and = (Flowthru.Flow.FlowSliceStrategy.And)args.Slice!;
    var not = (Flowthru.Flow.FlowSliceStrategy.Not)and.B;
    Assert.That(not.Inner, Is.InstanceOf<Flowthru.Flow.FlowSliceStrategy.Flows>(),
      "`flows:Ingest` should strip the prefix and dispatch to FlowSliceStrategy.Flows.");
    var flows = (Flowthru.Flow.FlowSliceStrategy.Flows)not.Inner;
    Assert.That(flows.FlowLabels, Is.EquivalentTo(new[] { "Ingest" }),
      "The `flows:` prefix must be stripped before populating Flows.FlowLabels.");
  }

  [Test]
  public void ExcludeFlag_WithTo_ComposesAndNotInsideBase()
  {
    // `--to X --exclude flows:Y` should parse to And(To(X), Not(Flows(Y))).
    var args = ArgumentParser.Parse(new[]
    {
      "--to", "CardEmbeddings",
      "--exclude", "flows:Ingest",
    });
    Assert.That(args.Slice, Is.InstanceOf<Flowthru.Flow.FlowSliceStrategy.And>());
    var and = (Flowthru.Flow.FlowSliceStrategy.And)args.Slice!;
    Assert.That(and.A, Is.InstanceOf<Flowthru.Flow.FlowSliceStrategy.To>(),
      "--to populates the LHS rather than the implicit All base.");
    var to = (Flowthru.Flow.FlowSliceStrategy.To)and.A;
    Assert.That(to.LabelPatterns, Is.EquivalentTo(new[] { "CardEmbeddings" }));
    Assert.That(and.B, Is.InstanceOf<Flowthru.Flow.FlowSliceStrategy.Not>());
    var not = (Flowthru.Flow.FlowSliceStrategy.Not)and.B;
    Assert.That(not.Inner, Is.InstanceOf<Flowthru.Flow.FlowSliceStrategy.Flows>());
    var flows = (Flowthru.Flow.FlowSliceStrategy.Flows)not.Inner;
    Assert.That(flows.FlowLabels, Is.EquivalentTo(new[] { "Ingest" }));
  }

  [Test]
  public void ExcludeFlag_MultipleInvocations_UnionInsideSingleNot()
  {
    // Multiple --exclude flags compose via Or *inside* a single Not.
    // `--exclude flows:A --exclude flows:B` ≡ Not(Or(Flows(A), Flows(B))).
    var args = ArgumentParser.Parse(new[]
    {
      "--exclude", "flows:Ingest",
      "--exclude", "flows:Reporting",
    });
    var and = (Flowthru.Flow.FlowSliceStrategy.And)args.Slice!;
    var not = (Flowthru.Flow.FlowSliceStrategy.Not)and.B;
    Assert.That(not.Inner, Is.InstanceOf<Flowthru.Flow.FlowSliceStrategy.Or>(),
      "Two --exclude flags should compose via Or inside the single Not.");
    var or = (Flowthru.Flow.FlowSliceStrategy.Or)not.Inner;
    Assert.That(or.A, Is.InstanceOf<Flowthru.Flow.FlowSliceStrategy.Flows>());
    Assert.That(or.B, Is.InstanceOf<Flowthru.Flow.FlowSliceStrategy.Flows>());
    var labels = new List<string>();
    labels.AddRange(((Flowthru.Flow.FlowSliceStrategy.Flows)or.A).FlowLabels);
    labels.AddRange(((Flowthru.Flow.FlowSliceStrategy.Flows)or.B).FlowLabels);
    Assert.That(labels, Is.EquivalentTo(new[] { "Ingest", "Reporting" }));
  }

  [Test]
  public void ExcludeFlag_CommaSeparatedPatterns_UnionInsideSingleNot()
  {
    // A single --exclude with comma-separated patterns mixing prefix and
    // bare labels: each entry becomes its own sub-strategy unioned via Or.
    var args = ArgumentParser.Parse(new[]
    {
      "--exclude", "clean-customers,flows:Reporting,validate-*",
    });
    var and = (Flowthru.Flow.FlowSliceStrategy.And)args.Slice!;
    var not = (Flowthru.Flow.FlowSliceStrategy.Not)and.B;
    Assert.That(not.Inner, Is.InstanceOf<Flowthru.Flow.FlowSliceStrategy.Or>(),
      "Three comma-separated patterns should fold into a chain of Or inside the Not.");
  }

  [Test]
  public void ExcludeFlag_WithoutValue_Throws()
  {
    Assert.Throws<ArgumentException>(() => ArgumentParser.Parse(new[] { "--exclude" }));
  }

  [Test]
  public void ExcludeFlag_WithFlowFlag_Throws()
  {
    // --flow is mutually exclusive with the slice flags, including
    // --exclude — the host swaps between the registered-label and
    // slice-strategy code paths.
    Assert.Throws<ArgumentException>(() =>
      ArgumentParser.Parse(new[] { "--flow", "Reporting", "--exclude", "step.A" })
    );
  }

  [Test]
  public void HelpText_DocumentsExcludeAndFlowsPrefix()
  {
    // Snapshot-style assertion: the public help-text constant should
    // mention --exclude and the flows: prefix so end-users discover them
    // via `flowthru --help`.
    Assert.That(ArgumentParser.HelpText, Does.Contain("--exclude"),
      "Help text should document the new --exclude flag.");
    Assert.That(ArgumentParser.HelpText, Does.Contain("flows:"),
      "Help text should document the flows: matcher prefix.");
  }

  [Test]
  public void HelpText_DocumentsNoCache()
  {
    Assert.That(ArgumentParser.HelpText, Does.Contain("--no-cache"),
      "Help text should surface --no-cache so users discover the bypass.");
  }
}
