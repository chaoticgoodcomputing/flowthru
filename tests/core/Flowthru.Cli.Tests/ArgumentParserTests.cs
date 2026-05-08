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
}
