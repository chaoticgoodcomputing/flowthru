using Flowthru.Step.Python;

namespace Flowthru.Extensions.Python.Tests;

/// <summary>
/// Coverage for the process-wide <see cref="PythonStepCacheRegistry"/>.
/// The registry is populated by source-generator-emitted module
/// initializers (one <c>Register</c> call per <c>@step(cacheable=True)</c>
/// in the consuming project) and queried at <c>AddPythonStep</c>
/// construction time. These tests exercise the surface directly,
/// independent of the generator.
/// </summary>
[TestFixture]
[Category("PythonCache")]
public class PythonStepCacheRegistryTests
{
  [SetUp]
  public void Clear() => PythonStepCacheRegistry.ClearForTests();

  [TearDown]
  public void Reset() => PythonStepCacheRegistry.ClearForTests();

  [Test]
  public void Lookup_BeforeRegister_ReturnsNull() =>
    Assert.That(PythonStepCacheRegistry.Lookup("module", "function"), Is.Null);

  [Test]
  public void Register_RoundTripsThroughLookup()
  {
    PythonStepCacheRegistry.Register(
      module: "Flows.Foo.Steps.bar",
      function: "do_bar",
      pyFilePath: "/abs/path/bar.py",
      "/abs/path/uv.lock"
    );

    var entry = PythonStepCacheRegistry.Lookup("Flows.Foo.Steps.bar", "do_bar");

    Assert.That(entry, Is.Not.Null);
    Assert.That(entry!.PyFilePath, Is.EqualTo("/abs/path/bar.py"));
    Assert.That(entry.LockfileCandidates, Is.EqualTo(new[] { "/abs/path/uv.lock" }));
  }

  [Test]
  public void Register_AcceptsMultipleLockfileCandidates()
  {
    // The generator emits the full walk-up list; the runtime picks the
    // first existing one via ResolveLockfile().
    PythonStepCacheRegistry.Register(
      module: "Flows.A.Steps.a",
      function: "a_fn",
      pyFilePath: "/repo/Flows/A/Steps/a.py",
      "/repo/Flows/A/Steps/uv.lock",
      "/repo/Flows/A/Steps/pyproject.toml",
      "/repo/Flows/A/uv.lock",
      "/repo/uv.lock"
    );

    var entry = PythonStepCacheRegistry.Lookup("Flows.A.Steps.a", "a_fn")!;
    Assert.That(entry.LockfileCandidates, Has.Count.EqualTo(4));
  }

  [Test]
  public void Register_ReplaysSamKey_IsIdempotent()
  {
    // Module initializers can run more than once in test harness
    // scenarios (e.g., assembly reload). Re-registering the same key
    // replaces the entry rather than failing.
    PythonStepCacheRegistry.Register("m", "f", "/first.py");
    PythonStepCacheRegistry.Register("m", "f", "/second.py", "/lock.lock");

    var entry = PythonStepCacheRegistry.Lookup("m", "f")!;
    Assert.That(entry.PyFilePath, Is.EqualTo("/second.py"));
    Assert.That(entry.LockfileCandidates, Is.EqualTo(new[] { "/lock.lock" }));
  }

  [Test]
  public void Register_RejectsEmptyModule() =>
    Assert.Throws<ArgumentException>(() =>
      PythonStepCacheRegistry.Register("", "f", "/p.py"));

  [Test]
  public void Register_RejectsEmptyFunction() =>
    Assert.Throws<ArgumentException>(() =>
      PythonStepCacheRegistry.Register("m", "", "/p.py"));

  [Test]
  public void Register_RejectsEmptyPyFilePath() =>
    Assert.Throws<ArgumentException>(() =>
      PythonStepCacheRegistry.Register("m", "f", ""));

  [Test]
  public void Lookup_WithEmptyArgs_ReturnsNull()
  {
    PythonStepCacheRegistry.Register("m", "f", "/p.py");
    Assert.That(PythonStepCacheRegistry.Lookup("", "f"), Is.Null);
    Assert.That(PythonStepCacheRegistry.Lookup("m", ""), Is.Null);
  }

  // ── Entry.ResolveLockfile ───────────────────────────────────────────

  [Test]
  public void ResolveLockfile_NoCandidates_ReturnsNull()
  {
    var entry = new PythonStepCacheRegistry.Entry("/p.py", Array.Empty<string>());
    Assert.That(entry.ResolveLockfile(), Is.Null);
  }

  [Test]
  public void ResolveLockfile_NoCandidateExists_ReturnsNull()
  {
    var entry = new PythonStepCacheRegistry.Entry(
      "/p.py",
      new[] { "/does/not/exist/uv.lock", "/also/missing/pyproject.toml" }
    );
    Assert.That(entry.ResolveLockfile(), Is.Null);
  }

  [Test]
  public void ResolveLockfile_FirstExistingWins()
  {
    var tempA = Path.Combine(Path.GetTempPath(), $"flowthru-lock-{Guid.NewGuid():N}.toml");
    var tempB = Path.Combine(Path.GetTempPath(), $"flowthru-lock-{Guid.NewGuid():N}.toml");
    File.WriteAllText(tempA, "# first");
    File.WriteAllText(tempB, "# second");
    try
    {
      var entry = new PythonStepCacheRegistry.Entry(
        "/p.py",
        new[] { "/missing.lock", tempA, tempB }
      );
      Assert.That(entry.ResolveLockfile(), Is.EqualTo(tempA));
    }
    finally
    {
      File.Delete(tempA);
      File.Delete(tempB);
    }
  }
}
