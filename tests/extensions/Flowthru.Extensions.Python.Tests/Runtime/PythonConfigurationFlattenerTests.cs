using Flowthru.Extensions.Python.Runtime;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace Flowthru.Extensions.Python.Tests.Runtime;

/// <summary>
/// Tests for <see cref="PythonConfigurationFlattener"/> — the bridge that
/// flattens a configured slice of <see cref="IConfiguration"/> into env-var
/// pairs for injection into the Python subprocess. Mirrors .NET's native
/// <c>:</c> → <c>__</c> rule used by ASP.NET Core, Azure App Service, and
/// every other .NET-spawns-subprocess deployment.
/// </summary>
[TestFixture]
[Category("Python")]
[Category("Runtime")]
public class PythonConfigurationFlattenerTests
{
  // ── Bridge disabled ─────────────────────────────────────────────────

  [Test]
  public void Flatten_EmptyConfigurationSection_ReturnsEmpty()
  {
    var flattener = BuildFlattener(
      configurationSection: string.Empty,
      configValues: new Dictionary<string, string?>
      {
        ["Diarization:PyannoteModel"] = "pyannote/speaker-diarization-3.1",
      }
    );

    Assert.That(flattener.Flatten(), Is.Empty);
  }

  [Test]
  public void Flatten_MissingSection_ReturnsEmpty()
  {
    // ConfigurationSection is set to a path that doesn't exist in
    // IConfiguration — the bridge silently produces an empty dict rather
    // than throwing. Missing config is the user's problem to detect via
    // their inspector logic, not the flattener's.
    var flattener = BuildFlattener(
      configurationSection: "DoesNotExist",
      configValues: new Dictionary<string, string?>
      {
        ["Diarization:PyannoteModel"] = "pyannote/speaker-diarization-3.1",
      }
    );

    Assert.That(flattener.Flatten(), Is.Empty);
  }

  // ── Single-level flattening ─────────────────────────────────────────

  [Test]
  public void Flatten_SingleLevelSection_ProducesKeyedEntries()
  {
    var flattener = BuildFlattener(
      configurationSection: "Diarization",
      configValues: new Dictionary<string, string?>
      {
        ["Diarization:PyannoteModel"] = "pyannote/speaker-diarization-3.1",
        ["Diarization:WhisperModel"] = "base.en",
        ["Other:Unrelated"] = "should-not-leak",
      }
    );

    var env = flattener.Flatten();
    Assert.Multiple(() =>
    {
      Assert.That(env["Diarization__PyannoteModel"], Is.EqualTo("pyannote/speaker-diarization-3.1"));
      Assert.That(env["Diarization__WhisperModel"], Is.EqualTo("base.en"));
      Assert.That(env, Has.Count.EqualTo(2), "Other:Unrelated should not be exported");
    });
  }

  // ── Nested flattening ───────────────────────────────────────────────

  [Test]
  public void Flatten_NestedSection_JoinsKeysWithDoubleUnderscore()
  {
    var flattener = BuildFlattener(
      configurationSection: "Diarization",
      configValues: new Dictionary<string, string?>
      {
        ["Diarization:Whisper:ModelName"] = "base.en",
        ["Diarization:Whisper:Cache:Directory"] = "/tmp/whisper",
      }
    );

    var env = flattener.Flatten();
    Assert.Multiple(() =>
    {
      Assert.That(env["Diarization__Whisper__ModelName"], Is.EqualTo("base.en"));
      Assert.That(
        env["Diarization__Whisper__Cache__Directory"],
        Is.EqualTo("/tmp/whisper")
      );
    });
  }

  // ── .NET array semantics ────────────────────────────────────────────

  [Test]
  public void Flatten_ArraySection_EmitsNumericKeys()
  {
    // IConfiguration represents arrays as sections whose child keys are
    // stringified non-negative integers. The flattener emits each entry as
    // Section__N=value; the Python flowthru.config re-nester detects the
    // numeric keys and materializes a list. No special-case handling is
    // required here.
    var flattener = BuildFlattener(
      configurationSection: "Diarization",
      configValues: new Dictionary<string, string?>
      {
        ["Diarization:AllowedModels:0"] = "small",
        ["Diarization:AllowedModels:1"] = "base",
        ["Diarization:AllowedModels:2"] = "medium",
      }
    );

    var env = flattener.Flatten();
    Assert.Multiple(() =>
    {
      Assert.That(env["Diarization__AllowedModels__0"], Is.EqualTo("small"));
      Assert.That(env["Diarization__AllowedModels__1"], Is.EqualTo("base"));
      Assert.That(env["Diarization__AllowedModels__2"], Is.EqualTo("medium"));
    });
  }

  // ── Mixed shape ─────────────────────────────────────────────────────

  [Test]
  public void Flatten_MixedScalarAndNestedAndArray_AllPresent()
  {
    var flattener = BuildFlattener(
      configurationSection: "Diarization",
      configValues: new Dictionary<string, string?>
      {
        ["Diarization:PyannoteModel"] = "pyannote/speaker-diarization-3.1",
        ["Diarization:Whisper:ModelName"] = "base.en",
        ["Diarization:AllowedModels:0"] = "small",
        ["Diarization:AllowedModels:1"] = "base",
      }
    );

    var env = flattener.Flatten();
    Assert.Multiple(() =>
    {
      Assert.That(env["Diarization__PyannoteModel"], Is.Not.Null);
      Assert.That(env["Diarization__Whisper__ModelName"], Is.Not.Null);
      Assert.That(env["Diarization__AllowedModels__0"], Is.EqualTo("small"));
      Assert.That(env["Diarization__AllowedModels__1"], Is.EqualTo("base"));
      Assert.That(env, Has.Count.EqualTo(4));
    });
  }

  // ── Deep section path ───────────────────────────────────────────────

  [Test]
  public void Flatten_DeepSectionPath_FlattensFromThatPoint()
  {
    // Section paths can themselves be nested (e.g. "Flowthru:Python:Services").
    // The flattener walks from the named section, not the root — keys
    // beyond the section path are joined with __, and the section's own
    // path becomes the env-var prefix.
    var flattener = BuildFlattener(
      configurationSection: "Flowthru:Python:Services",
      configValues: new Dictionary<string, string?>
      {
        ["Flowthru:Python:Services:Token"] = "secret-token",
        ["Flowthru:Python:Services:Endpoint:Url"] = "https://api.example.com",
      }
    );

    var env = flattener.Flatten();
    Assert.Multiple(() =>
    {
      Assert.That(env["Flowthru__Python__Services__Token"], Is.EqualTo("secret-token"));
      Assert.That(
        env["Flowthru__Python__Services__Endpoint__Url"],
        Is.EqualTo("https://api.example.com")
      );
    });
  }

  // ── Helper ──────────────────────────────────────────────────────────

  private static PythonConfigurationFlattener BuildFlattener(
    string configurationSection,
    Dictionary<string, string?> configValues
  )
  {
    var configuration = new ConfigurationBuilder()
      .AddInMemoryCollection(configValues)
      .Build();

    var options = new PythonRuntimeOptions { ConfigurationSection = configurationSection };

    return new PythonConfigurationFlattener(configuration, Options.Create(options));
  }
}
