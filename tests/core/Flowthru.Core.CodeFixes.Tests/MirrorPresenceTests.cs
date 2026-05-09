namespace Flowthru.Core.CodeFixes.Tests;

/// <summary>
/// Skeleton smoke test for <c>Flowthru.Core.CodeFixes</c>. The source
/// project has no shipped CodeFixProviders yet — when one lands, replace
/// this with `CSharpCodeFixVerifier`-style fixture tests for it. The
/// project exists today so the workspace's <c>_test:project-mirror</c>
/// invariant ("every <c>src/{domain}/{Project}</c> has a matching
/// <c>tests/{domain}/{Project}.Tests</c>") holds.
/// </summary>
public class MirrorPresenceTests
{
  [Test]
  public void Mirror_ProjectExists()
  {
    Assert.Pass(
      "Flowthru.Core.CodeFixes test project mirrors the empty source project; "
      + "replace with real CodeFixProvider tests when fixes ship."
    );
  }
}
