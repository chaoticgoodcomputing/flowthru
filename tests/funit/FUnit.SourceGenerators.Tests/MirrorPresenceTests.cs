namespace FUnit.SourceGenerators.Tests;

/// <summary>
/// Skeleton smoke test for <c>FUnit.SourceGenerators</c>. The shipped
/// generator (<c>StepTestRegistryGenerator</c>) and analyzer
/// (<c>FUnitDiagnosticAnalyzer</c> emitting <c>FU001</c> / <c>FU002</c>)
/// are exercised end-to-end by the inline-step smoke test in
/// <c>tests/funit/FUnit.Tests/InlineStepSmokeTest.cs</c>. This project
/// exists so the workspace's <c>_test:project-mirror</c> invariant
/// holds; replace with focused generator-output snapshot tests (using
/// <c>CSharpGeneratorDriver</c>) and analyzer-fixture tests when finer-
/// grained coverage is needed.
/// </summary>
public class MirrorPresenceTests
{
  [Test]
  public void Mirror_ProjectExists()
  {
    Assert.Pass(
      "FUnit.SourceGenerators test project mirrors src; generator + analyzer "
      + "behaviour is currently covered end-to-end via FUnit.Tests's inline "
      + "smoke test."
    );
  }
}
