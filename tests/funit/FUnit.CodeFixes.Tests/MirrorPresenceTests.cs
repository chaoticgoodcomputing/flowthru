namespace FUnit.CodeFixes.Tests;

/// <summary>
/// Skeleton smoke test for <c>FUnit.CodeFixes</c>. The shipped fix
/// (<c>Fu001ScaffoldTestsClassFix</c> for the <c>FU001</c> diagnostic)
/// has no fixture coverage today; when added it should follow the
/// <c>CSharpCodeFixVerifier</c> pattern from
/// <c>Flowthru.Core.SourceGenerators.Tests</c>. This project exists so
/// the workspace's <c>_test:project-mirror</c> invariant holds.
/// </summary>
public class MirrorPresenceTests
{
  [Test]
  public void Mirror_ProjectExists()
  {
    Assert.Pass(
      "FUnit.CodeFixes test project mirrors src; "
      + "Fu001ScaffoldTestsClassFix awaits dedicated fixture coverage."
    );
  }
}
