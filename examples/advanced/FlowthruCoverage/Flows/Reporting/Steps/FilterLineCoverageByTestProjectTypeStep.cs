using Flowthru.Core.Steps;
using FlowthruCoverage.Data._01_Raw.Schemas;
using FlowthruCoverage.Data._02_Intermediate.Schemas;
#if FUNIT_ENABLED
using Flowthru.FUnit;
#endif

namespace FlowthruCoverage.Flows.Reporting.Steps;

/// <summary>
/// Filters line coverage rows down to those produced by test projects of a specified
/// manifest <see cref="ProjectManifestEntry.ProjectType"/>. Used to slice coverage by
/// originator — e.g., "what does just the example surface area cover?" vs the default
/// "everything that ran."
/// </summary>
[FlowthruStep]
public static class FilterLineCoverageByTestProjectTypeStep
{
  public static Func<
    (IEnumerable<LineCoverageRow>, IEnumerable<ProjectManifestEntry>),
    IEnumerable<LineCoverageRow>
  > Create(string projectType)
  {
    if (string.IsNullOrWhiteSpace(projectType))
      throw new ArgumentException("Project type required.", nameof(projectType));

    return inputs =>
    {
      var (rows, manifest) = inputs;

      var allowedProjects = manifest
        .Where(e => string.Equals(e.ProjectType, projectType, StringComparison.Ordinal))
        .Select(e => e.AssemblyName)
        .ToHashSet(StringComparer.Ordinal);

      return rows.Where(r => allowedProjects.Contains(r.TestProject));
    };
  }

#if FUNIT_ENABLED
  /// <summary>FUnit tests for <see cref="FilterLineCoverageByTestProjectTypeStep"/>.</summary>
  public class Tests : FUnitContext
  {
    private static ProjectManifestEntry Manifest(string assemblyName, string projectType) =>
      new()
      {
        AssemblyName = assemblyName,
        ProjectType = projectType,
        Subgroup = "",
      };

    private static LineCoverageRow Row(string testProject) =>
      new()
      {
        TestProject = testProject,
        SrcPackage = "Pkg",
        SourceFile = "/repo/src/Pkg/A.cs",
        ClassName = "Pkg.A",
        MethodName = "M",
        MethodSignature = "()",
        LineNumber = 1,
        Hits = 1,
      };

    /// <summary>
    /// Only rows whose <c>TestProject</c> matches a manifest entry of the requested type
    /// pass through. Confirms the load-bearing case for slicing example-only coverage.
    /// </summary>
    [StepTest(typeof(FilterLineCoverageByTestProjectTypeStep))]
    public void Filter_KeepsOnlyMatchingProjectType()
    {
      var rows = new[] { Row("LibTests"), Row("ExampleA"), Row("ExampleB"), Row("Other") };
      var manifest = new[]
      {
        Manifest("LibTests", "LibraryTest"),
        Manifest("ExampleA", "Example"),
        Manifest("ExampleB", "Example"),
        Manifest("Other", "IntegrationTest"),
      };

      var result = Invoke(FilterLineCoverageByTestProjectTypeStep.Create("Example"), (rows, manifest)).ToList();

      Assert.That(result.Select(r => r.TestProject), Is.EquivalentTo(new[] { "ExampleA", "ExampleB" }));
    }

    /// <summary>
    /// Rows whose <c>TestProject</c> isn't in the manifest at all are dropped — the filter
    /// is gated on a positive manifest match, not negation. Important for runs where the
    /// staged coverage XML directory contains files produced by projects that don't exist
    /// in the manifest yet.
    /// </summary>
    [StepTest(typeof(FilterLineCoverageByTestProjectTypeStep))]
    public void Filter_DropsRowsForUnknownTestProjects()
    {
      var rows = new[] { Row("Known"), Row("Unknown") };
      var manifest = new[] { Manifest("Known", "Example") };

      var result = Invoke(FilterLineCoverageByTestProjectTypeStep.Create("Example"), (rows, manifest)).ToList();

      Assert.That(result.Select(r => r.TestProject), Is.EqualTo(new[] { "Known" }));
    }

    [StepTest(typeof(FilterLineCoverageByTestProjectTypeStep))]
    public void Filter_EmptyInput_YieldsEmptyOutput()
    {
      var result = Invoke(
        FilterLineCoverageByTestProjectTypeStep.Create("Example"),
        (Enumerable.Empty<LineCoverageRow>(), Enumerable.Empty<ProjectManifestEntry>())
      );

      Assert.That(result, Is.Empty);
    }
  }
#endif
}
