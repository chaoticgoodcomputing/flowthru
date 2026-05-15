using Flowthru.Data.Schema;

namespace Flowthru.Extensions.Python.SourceGenerators.Tests;

/// <summary>
/// Coverage for the <c>@step(cacheable=True)</c> auto-derivation
/// pipeline. The factory generator parses the <c>cacheable</c> kwarg
/// from the Python decorator and, for every step that opted in, emits
/// a <c>[ModuleInitializer]</c> companion class that registers the
/// step with <c>PythonStepCacheRegistry</c> at module load. The matrix
/// generator then consults that registry from <c>AddPythonStep</c> to
/// derive a CodeVersion automatically — no user-side wiring required.
/// </summary>
[TestFixture]
[Category("PythonCache")]
public class PythonStepCacheableGeneratorTests
{
  private const string SchemaSource = """
    using Flowthru.Data.Schema;

    namespace Sample;

    [FlowthruSchema]
    public partial record Row
    {
      public required int Id { get; init; }
    }
    """;

  // ── Default (no cacheable kwarg) — no registration emitted ─────────

  [Test]
  public void StepWithoutCacheableKwarg_EmitsNoRegistration()
  {
    var py = new GeneratorTestHarness.InMemoryAdditionalText(
      path: "Flows/Demo/step.py",
      text: "@step(inputs=[Row], outputs=[Row])\ndef do_thing(x):\n    return x\n"
    );

    var result = GeneratorTestHarness.Run(
      new PythonStepFactoryGenerator(),
      source: SchemaSource,
      additionalFiles: new[] { py }
    );

    Assert.That(result.GeneratedSources.TryGetValue("PythonSteps.g.cs", out var emitted), Is.True,
      "PythonSteps.g.cs should be emitted.");
    Assert.That(emitted, Does.Not.Contain("PythonStepCacheRegistry.Register"),
      "Steps without cacheable=True must not emit any cache registration.");
    Assert.That(emitted, Does.Not.Contain("PythonStepCacheRegistration"),
      "The companion module-initializer class should not be emitted for opt-out steps.");
  }

  // ── cacheable=False explicitly — same as default ────────────────────

  [Test]
  public void StepWithCacheableFalse_EmitsNoRegistration()
  {
    var py = new GeneratorTestHarness.InMemoryAdditionalText(
      path: "Flows/Demo/step.py",
      text: "@step(inputs=[Row], outputs=[Row], cacheable=False)\ndef do_thing(x):\n    return x\n"
    );

    var result = GeneratorTestHarness.Run(
      new PythonStepFactoryGenerator(),
      source: SchemaSource,
      additionalFiles: new[] { py }
    );

    Assert.That(result.GeneratedSources.TryGetValue("PythonSteps.g.cs", out var emitted), Is.True,
      "PythonSteps.g.cs should be emitted.");
    Assert.That(emitted, Does.Not.Contain("PythonStepCacheRegistry.Register"));
  }

  // ── cacheable=True — module initializer emitted ─────────────────────

  [Test]
  public void StepWithCacheableTrue_EmitsModuleInitializerWithRegistration()
  {
    var py = new GeneratorTestHarness.InMemoryAdditionalText(
      path: "/repo/Flows/Demo/step.py",
      text:
        "@step(inputs=[Row], outputs=[Row], cacheable=True)\n" +
        "def do_thing(x):\n    return x\n"
    );

    var result = GeneratorTestHarness.Run(
      new PythonStepFactoryGenerator(),
      source: SchemaSource,
      additionalFiles: new[] { py }
    );

    Assert.That(result.GeneratedSources.TryGetValue("PythonSteps.g.cs", out var emitted), Is.True,
      "PythonSteps.g.cs should be emitted.");
    Assert.That(emitted, Does.Contain("internal static class PythonStepCacheRegistration"),
      "A cacheable=True step must trigger emission of the module-initializer companion.");
    Assert.That(emitted,
      Does.Contain("[global::System.Runtime.CompilerServices.ModuleInitializer]"),
      "The companion must carry [ModuleInitializer] so it runs at assembly load.");
    Assert.That(emitted, Does.Contain("PythonStepCacheRegistry.Register"),
      "The companion must call Register() for the step.");
    Assert.That(emitted, Does.Contain("\"do_thing\""),
      "Register must receive the function name as a string literal.");
    Assert.That(emitted, Does.Contain("\"Flows.Demo.step\""),
      "Register must receive the dotted module path as a string literal.");
  }

  [Test]
  public void StepWithCacheableTrue_EmitsLockfileCandidatesWalkingUp()
  {
    var py = new GeneratorTestHarness.InMemoryAdditionalText(
      path: "/repo/Flows/Demo/Steps/step.py",
      text: "@step(inputs=[Row], outputs=[Row], cacheable=True)\ndef do_thing(x):\n    return x\n"
    );

    var result = GeneratorTestHarness.Run(
      new PythonStepFactoryGenerator(),
      source: SchemaSource,
      additionalFiles: new[] { py }
    );

    Assert.That(result.GeneratedSources.TryGetValue("PythonSteps.g.cs", out var emitted), Is.True,
      "PythonSteps.g.cs should be emitted.");

    // The candidate list should walk up at least to the repo root,
    // emitting each lockfile name at each directory level.
    Assert.That(emitted, Does.Contain("uv.lock"),
      "Lockfile candidates must include uv.lock at every directory level.");
    Assert.That(emitted, Does.Contain("pyproject.toml"),
      "Lockfile candidates must include pyproject.toml fallback.");
    Assert.That(emitted, Does.Contain("poetry.lock"),
      "Lockfile candidates must include poetry.lock.");
    Assert.That(emitted, Does.Contain("requirements.txt"),
      "Lockfile candidates must include requirements.txt.");
  }

  // ── Mixed cacheable / non-cacheable steps in same project ───────────

  [Test]
  public void MixedSteps_OnlyCacheableOnesRegister()
  {
    var cacheable = new GeneratorTestHarness.InMemoryAdditionalText(
      path: "/repo/Flows/A/cached.py",
      text: "@step(inputs=[Row], outputs=[Row], cacheable=True)\ndef cached_fn(x):\n    return x\n"
    );
    var notCacheable = new GeneratorTestHarness.InMemoryAdditionalText(
      path: "/repo/Flows/B/plain.py",
      text: "@step(inputs=[Row], outputs=[Row])\ndef plain_fn(x):\n    return x\n"
    );

    var result = GeneratorTestHarness.Run(
      new PythonStepFactoryGenerator(),
      source: SchemaSource,
      additionalFiles: new[] { cacheable, notCacheable }
    );

    Assert.That(result.GeneratedSources.TryGetValue("PythonSteps.g.cs", out var emitted), Is.True,
      "PythonSteps.g.cs should be emitted.");
    Assert.That(emitted, Does.Contain("\"cached_fn\""),
      "The cacheable step's function name must appear in the registration block.");
    Assert.That(emitted, Does.Not.Contain("\"plain_fn\""),
      "The non-cacheable step's function name must NOT appear in any Register call.");
  }
}
