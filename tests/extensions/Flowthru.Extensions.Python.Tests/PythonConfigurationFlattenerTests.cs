using Flowthru.Step.Python;
using Flowthru.Step.Python.Internal;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace Flowthru.Extensions.Python.Tests;

/// <summary>
/// Pins the IConfiguration → flat env-var dictionary projection used to
/// hand strongly-typed config to the Python child process. Composite
/// keys join with <c>__</c> per the .NET subprocess convention; arrays
/// are emitted as numeric-keyed children (e.g. <c>Foo__0</c>,
/// <c>Foo__1</c>) for the Python re-nester to materialise back into a
/// list.
/// </summary>
[TestFixture]
[Category("Python")]
public class PythonConfigurationFlattenerTests
{
  private static IPythonConfigurationFlattener Build(
    IDictionary<string, string?> values,
    string section
  )
  {
    var configuration = new ConfigurationBuilder().AddInMemoryCollection(values).Build();
    var options = Options.Create(new PythonRuntimeOptions { ConfigurationSection = section });
    return new PythonConfigurationFlattener(configuration, options);
  }

  [Test]
  public void Constructor_NullConfig_Throws()
  {
    var options = Options.Create(new PythonRuntimeOptions());
    Assert.That(
      () => new PythonConfigurationFlattener(null!, options),
      Throws.TypeOf<ArgumentNullException>()
    );
  }

  [Test]
  public void Constructor_NullOptions_Throws()
  {
    var configuration = new ConfigurationBuilder().Build();
    Assert.That(
      () => new PythonConfigurationFlattener(configuration, null!),
      Throws.TypeOf<ArgumentNullException>()
    );
  }

  [Test]
  public void Flatten_EmptySectionName_ReturnsEmpty()
  {
    var flat = Build(new Dictionary<string, string?>(), section: "").Flatten();
    Assert.That(flat, Is.Empty);
  }

  [Test]
  public void Flatten_WhitespaceSectionName_ReturnsEmpty()
  {
    var flat = Build(new Dictionary<string, string?>(), section: "   ").Flatten();
    Assert.That(flat, Is.Empty);
  }

  [Test]
  public void Flatten_NonExistentSection_ReturnsEmpty()
  {
    var values = new Dictionary<string, string?>
    {
      ["Other:Foo"] = "bar",
    };
    var flat = Build(values, section: "Missing").Flatten();
    Assert.That(flat, Is.Empty);
  }

  [Test]
  public void Flatten_FlatLeaves_EmitsPrefixedKeys()
  {
    var values = new Dictionary<string, string?>
    {
      ["Diarization:Endpoint"] = "https://api.example.com",
      ["Diarization:Token"] = "secret",
    };
    var flat = Build(values, section: "Diarization").Flatten();
    Assert.That(flat["Diarization__Endpoint"], Is.EqualTo("https://api.example.com"));
    Assert.That(flat["Diarization__Token"], Is.EqualTo("secret"));
    Assert.That(flat, Has.Count.EqualTo(2));
  }

  [Test]
  public void Flatten_NestedLeaves_JoinsWithDoubleUnderscore()
  {
    var values = new Dictionary<string, string?>
    {
      ["Diarization:Model:Name"] = "pyannote/speaker-diarization-3.1",
      ["Diarization:Model:Threshold"] = "0.5",
    };
    var flat = Build(values, section: "Diarization").Flatten();
    Assert.That(flat["Diarization__Model__Name"], Is.EqualTo("pyannote/speaker-diarization-3.1"));
    Assert.That(flat["Diarization__Model__Threshold"], Is.EqualTo("0.5"));
  }

  [Test]
  public void Flatten_ArrayValues_EmitNumericKeyedChildren()
  {
    // .NET IConfiguration treats arrays as sections with stringified-int keys.
    var values = new Dictionary<string, string?>
    {
      ["Diarization:AllowedSpeakers:0"] = "alice",
      ["Diarization:AllowedSpeakers:1"] = "bob",
    };
    var flat = Build(values, section: "Diarization").Flatten();
    Assert.That(flat["Diarization__AllowedSpeakers__0"], Is.EqualTo("alice"));
    Assert.That(flat["Diarization__AllowedSpeakers__1"], Is.EqualTo("bob"));
  }

  [Test]
  public void Flatten_OnlyEmitsKeysUnderConfiguredSection()
  {
    var values = new Dictionary<string, string?>
    {
      ["Diarization:Foo"] = "in-section",
      ["OtherSection:Bar"] = "out-of-section",
    };
    var flat = Build(values, section: "Diarization").Flatten();
    Assert.That(flat.Keys, Is.EquivalentTo(new[] { "Diarization__Foo" }));
  }
}
