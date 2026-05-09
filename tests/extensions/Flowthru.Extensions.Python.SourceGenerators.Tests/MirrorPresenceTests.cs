namespace Flowthru.Extensions.Python.SourceGenerators.Tests;

/// <summary>
/// Skeleton smoke test for <c>Flowthru.Extensions.Python.SourceGenerators</c>.
/// The shipped generators (<c>PythonStepFactoryGenerator</c>,
/// <c>PythonStepGenerator</c>) are exercised end-to-end by the consumer
/// integration test (<c>Flowthru.Tests.Examples</c> running
/// <c>SpaceflightsPythonEFCore</c>, <c>KedroIrisPython</c>, etc.) and
/// behaviourally by the <c>Flowthru.Extensions.Python.Tests</c> suite.
/// This project exists so the workspace's <c>_test:project-mirror</c>
/// invariant holds; replace with focused generator-output snapshot tests
/// (using <c>CSharpGeneratorDriver</c>) when finer-grained coverage is
/// needed.
/// </summary>
public class MirrorPresenceTests
{
  [Test]
  public void Mirror_ProjectExists()
  {
    Assert.Pass(
      "Flowthru.Extensions.Python.SourceGenerators test project mirrors src; "
      + "generator behaviour is currently covered end-to-end via "
      + "Flowthru.Tests.Examples and Flowthru.Extensions.Python.Tests."
    );
  }
}
