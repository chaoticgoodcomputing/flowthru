using Flowthru.Data.Catalog;
using Flowthru.Flow;
using Flowthru.Prelude;
using Flowthru.Step;
using Flowthru.Step.Python;
using Flowthru.Step.Python.Internal;
using Flowthru.Validation.PreFlight;
using Flowthru.Validation.Runtime;

namespace Flowthru.Extensions.Python.Tests;

/// <summary>
/// Pins the <c>CodeVersion</c> identity that
/// <see cref="PythonStep{TIn, TOut}"/> exposes on the
/// <see cref="IStepNode"/> surface. The Python extension's CodeVersion
/// is derived from three sources that together describe a step's
/// reproducible environment:
/// <list type="number">
///   <item>The <c>.py</c> source text containing the step function;</item>
///   <item>The Python interpreter version string;</item>
///   <item>The dependency manifest (e.g., <c>requirements.txt</c>).</item>
/// </list>
/// Any of these changing invalidates the identity; cosmetic edits to
/// the surrounding directory layout do not.
/// </summary>
[TestFixture]
[Category("Python")]
public class PythonStepCodeVersionTests
{
  private string _tempRoot = null!;

  [SetUp]
  public void SetUp()
  {
    _tempRoot = Path.Combine(Path.GetTempPath(), "flowthru-pyver-" + Path.GetRandomFileName());
    Directory.CreateDirectory(_tempRoot);
  }

  [TearDown]
  public void TearDown()
  {
    if (Directory.Exists(_tempRoot))
    {
      try { Directory.Delete(_tempRoot, recursive: true); } catch { /* best-effort */ }
    }
  }

  // ── Derivation: stability and sensitivity ─────────────────────────────

  [Test]
  public void Derive_SamePySourceAndRequirements_ProducesStableVersion()
  {
    var pyPathA = Path.Combine(_tempRoot, "step-a.py");
    File.WriteAllText(pyPathA, "def step(x):\n    return x + 1\n");
    var reqsA = Path.Combine(_tempRoot, "reqs-a.txt");
    File.WriteAllText(reqsA, "numpy==1.26.0\n");

    var versionA = PythonCodeVersion.Derive(pyPathA, "Python 3.12.0", reqsA);
    var versionB = PythonCodeVersion.Derive(pyPathA, "Python 3.12.0", reqsA);

    Assert.That(versionB, Is.EqualTo(versionA),
      "Identical inputs must yield identical CodeVersions.");
    Assert.That(versionA, Is.Not.Null);
    Assert.That(versionA!.Length, Is.GreaterThan(0));
  }

  [Test]
  public void Derive_PySourceChange_ChangesVersion()
  {
    var pyPath = Path.Combine(_tempRoot, "step.py");
    File.WriteAllText(pyPath, "def step(x):\n    return x + 1\n");
    var reqs = Path.Combine(_tempRoot, "reqs.txt");
    File.WriteAllText(reqs, "numpy==1.26.0\n");

    var before = PythonCodeVersion.Derive(pyPath, "Python 3.12.0", reqs);

    File.WriteAllText(pyPath, "def step(x):\n    return x + 2\n");
    var after = PythonCodeVersion.Derive(pyPath, "Python 3.12.0", reqs);

    Assert.That(after, Is.Not.EqualTo(before),
      "Changing the .py source text must change the CodeVersion.");
  }

  [Test]
  public void Derive_RequirementsChange_ChangesVersion()
  {
    var pyPath = Path.Combine(_tempRoot, "step.py");
    File.WriteAllText(pyPath, "def step(x):\n    return x + 1\n");
    var reqs = Path.Combine(_tempRoot, "reqs.txt");
    File.WriteAllText(reqs, "numpy==1.26.0\n");

    var before = PythonCodeVersion.Derive(pyPath, "Python 3.12.0", reqs);

    File.WriteAllText(reqs, "numpy==1.26.1\n");
    var after = PythonCodeVersion.Derive(pyPath, "Python 3.12.0", reqs);

    Assert.That(after, Is.Not.EqualTo(before),
      "Changing the requirements file must change the CodeVersion.");
  }

  [Test]
  public void Derive_InterpreterVersionChange_ChangesVersion()
  {
    var pyPath = Path.Combine(_tempRoot, "step.py");
    File.WriteAllText(pyPath, "def step(x):\n    return x + 1\n");
    var reqs = Path.Combine(_tempRoot, "reqs.txt");
    File.WriteAllText(reqs, "numpy==1.26.0\n");

    var v312 = PythonCodeVersion.Derive(pyPath, "Python 3.12.0", reqs);
    var v311 = PythonCodeVersion.Derive(pyPath, "Python 3.11.0", reqs);

    Assert.That(v311, Is.Not.EqualTo(v312),
      "Changing the interpreter version must change the CodeVersion.");
  }

  [Test]
  public void Derive_NullPyPath_ReturnsNull()
  {
    // Fail-safe: an unknown source file shouldn't crash; just return null
    // so downstream cache-plan logic treats the step as cache-miss.
    var version = PythonCodeVersion.Derive(null, "Python 3.12.0", null);
    Assert.That(version, Is.Null);
  }

  [Test]
  public void Derive_MissingPyPath_ReturnsNull()
  {
    var missing = Path.Combine(_tempRoot, "does-not-exist.py");
    var version = PythonCodeVersion.Derive(missing, "Python 3.12.0", null);
    Assert.That(version, Is.Null,
      "Missing .py file must not crash; return null so downstream treats as cache-miss.");
  }

  [Test]
  public void Derive_MissingRequirements_FallsBackToSourceAndInterpreter()
  {
    // Requirements file is optional; absence shouldn't return null —
    // we still have a meaningful identity from the .py source + interpreter.
    var pyPath = Path.Combine(_tempRoot, "step.py");
    File.WriteAllText(pyPath, "def step(x):\n    return x\n");

    var version = PythonCodeVersion.Derive(pyPath, "Python 3.12.0", null);

    Assert.That(version, Is.Not.Null);
    Assert.That(version!.Length, Is.GreaterThan(0));
  }

  // ── PythonStep<TIn, TOut> integration ─────────────────────────────────

  [Test]
  public void PythonStep_ConstructedWithCodeVersion_ExposesValue()
  {
    var input = ItemFactory.Singleton.Memory<int>("py-cv-in");
    var output = ItemFactory.Singleton.Memory<int>("py-cv-out");

    var step = new PythonStep<int, int>(
      label: "py-step",
      moduleName: "demo",
      functionName: "step",
      transform: x => FlowIO.Pure(x),
      inputs: new IItem[] { input },
      outputs: new IItem[] { output },
      loadInputs: () => input.Load(),
      saveOutputs: r => output.Save(r),
      codeVersion: "py-v1"
    );

    Assert.That(((IStepNode)step).CodeVersion, Is.EqualTo("py-v1"));
  }

  [Test]
  public void PythonStep_ConstructedWithoutCodeVersion_ExposesNull()
  {
    var input = ItemFactory.Singleton.Memory<int>("py-cv-null-in");
    var output = ItemFactory.Singleton.Memory<int>("py-cv-null-out");

    var step = new PythonStep<int, int>(
      label: "py-step",
      moduleName: "demo",
      functionName: "step",
      transform: x => FlowIO.Pure(x),
      inputs: new IItem[] { input },
      outputs: new IItem[] { output },
      loadInputs: () => input.Load(),
      saveOutputs: r => output.Save(r)
    );

    Assert.That(((IStepNode)step).CodeVersion, Is.Null);
  }
}
