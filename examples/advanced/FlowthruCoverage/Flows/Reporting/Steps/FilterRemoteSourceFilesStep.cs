using Flowthru.Step;
using FlowthruCoverage.Data._03_Primary.Schemas;
#if FUNIT_ENABLED
using Flowthru.Step.Testing;
#endif

namespace FlowthruCoverage.Flows.Reporting.Steps;

/// <summary>
/// Drops method summary rows whose <see cref="MethodHitSummaryRow.SourceFile"/> is a
/// remote URL (<c>https://...</c>) rather than a local source path.
/// </summary>
/// <remarks>
/// Coverlet's Cobertura output occasionally records SourceLink URLs pointing at a
/// pre-pushed commit hash for assemblies coming from NuGet caches rather than the
/// local <c>src/</c> build. Those entries add no analytical value to the report —
/// you can't grep for callers, can't open the file in IDE, and the underlying code
/// is presumed to resolve locally once the build catches up. We drop them at the
/// reporting boundary so the published <c>uncovered_method_*.csv</c> only contain
/// rows with locally-resolvable source.
/// </remarks>
[FlowthruStep]
public static class FilterRemoteSourceFilesStep
{
  public static Func<
    IEnumerable<MethodHitSummaryRow>,
    IEnumerable<MethodHitSummaryRow>
  > Create()
  {
    return rows => rows.Where(r => !IsRemote(r.SourceFile));
  }

  /// <summary>
  /// Returns <see langword="true"/> when <paramref name="sourceFile"/> is a remote
  /// SourceLink URL rather than a local path. Predicate is exposed for test reuse.
  /// </summary>
  public static bool IsRemote(string sourceFile) =>
    !string.IsNullOrEmpty(sourceFile)
    && sourceFile.StartsWith("https://", StringComparison.Ordinal);

#if FUNIT_ENABLED
  /// <summary>FUnit tests for <see cref="FilterRemoteSourceFilesStep"/>.</summary>
  public class Tests : FUnitContext
  {
    private static MethodHitSummaryRow Row(string sourceFile) =>
      new()
      {
        Id = "Some.Method()",
        Subgroup = "Core",
        SourceFile = sourceFile,
        LineCount = 1,
        TotalHits = 0,
        ProjectHits = 0,
      };

    [FUnitStepTest(typeof(FilterRemoteSourceFilesStep))]
    public void EmptyInput_YieldsEmptyOutput()
    {
      var result = Invoke(
        FilterRemoteSourceFilesStep.Create(),
        Enumerable.Empty<MethodHitSummaryRow>()
      );

      Assert.That(result, Is.Empty);
    }

    [FUnitStepTest(typeof(FilterRemoteSourceFilesStep))]
    public void RemoteUrlRows_AreDropped()
    {
      var rows = new[]
      {
        Row("/home/user/repo/src/Foo.cs"),
        Row("https://raw.githubusercontent.com/foo/bar/abc123/src/Bar.cs"),
        Row("/home/user/repo/src/Baz.cs"),
      };

      var result = Invoke(FilterRemoteSourceFilesStep.Create(), rows).ToList();

      Assert.That(result, Has.Count.EqualTo(2));
      Assert.That(result.All(r => !r.SourceFile.StartsWith("https://", StringComparison.Ordinal)));
    }

    [FUnitStepTest(typeof(FilterRemoteSourceFilesStep))]
    public void EmptySourceFile_IsTreatedAsLocal()
    {
      // Empty SourceFile is sometimes emitted by Coverlet for top-level program
      // statements with no enclosing file context. We keep these — dropping them
      // would mask a Coverlet quirk under the remote-URL filter.
      var rows = new[] { Row(""), Row("https://example.com/x.cs") };

      var result = Invoke(FilterRemoteSourceFilesStep.Create(), rows).ToList();

      Assert.That(result, Has.Count.EqualTo(1));
      Assert.That(result[0].SourceFile, Is.EqualTo(""));
    }

    [TestCase("https://raw.githubusercontent.com/x/y/abc/src/Foo.cs", true)]
    [TestCase("/home/u/r/src/Foo.cs", false)]
    [TestCase("", false)]
    [TestCase("http://example.com/x.cs", false)] // http (no s) is not remote per the predicate
    public void IsRemote_MatchesHttpsPrefix(string sourceFile, bool expected) =>
      Assert.That(FilterRemoteSourceFilesStep.IsRemote(sourceFile), Is.EqualTo(expected));
  }
#endif
}
