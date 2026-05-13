using Flowthru.Core.SourceGenerators.Catalog;
using Flowthru.Data.Catalog;

namespace Flowthru.Core.SourceGenerators.Tests;

/// <summary>
/// Behavioural tests for <see cref="CatalogPropertyGenerator"/>. The
/// generator emits partial property bodies for catalog properties
/// annotated with <c>[JsonItem("path")]</c>, wiring
/// <c>ItemFactory.Enumerable.Json&lt;TRow&gt;</c> for
/// <c>IItem&lt;IEnumerable&lt;T&gt;&gt;</c> properties or
/// <c>ItemFactory.Singleton.Json&lt;T&gt;</c> for <c>IItem&lt;T&gt;</c>
/// singletons.
/// </summary>
[TestFixture]
public class CatalogPropertyGeneratorTests
{
  // ── Gating: partial keyword required ──────────────────────────────────

  [Test]
  public void NonPartialProperty_EmitsNothing()
  {
    // ExtractCatalogProperty requires the property to be marked `partial`
    // (the generator emits a partial body, so the user's slot must be
    // partial). A non-partial property is silently filtered out.
    var source = """
      using System.Collections.Generic;
      using Flowthru.Data.Catalog;
      namespace Sample;

      public partial class Catalog : CatalogAbstract
      {
        [JsonItem("path/to/file.json")]
        public IItem<IEnumerable<Row>> Rows { get; } = null!;
      }

      public record Row(int X);
      """;

    var result = AnalyzerTestHarness.RunGenerator(
      new CatalogPropertyGenerator(),
      source,
      typeof(JsonItemAttribute).Assembly
    );

    Assert.That(result.GeneratedSources, Is.Empty,
      "Non-partial property must not trigger generated source.");
  }

  // ── Enumerable shape ──────────────────────────────────────────────────

  [Test]
  public void EnumerableIItemProperty_EmitsEnumerableJsonFactory()
  {
    var source = """
      using System.Collections.Generic;
      using Flowthru.Data.Catalog;
      namespace Sample;

      public record Row(int X);

      public partial class Catalog : CatalogAbstract
      {
        [JsonItem("data/rows.json")]
        public partial IItem<IEnumerable<Row>> Rows { get; }
      }
      """;

    var emitted = EmitFirstSource(source);

    Assert.That(emitted,
      Does.Contain("global::Flowthru.Data.Catalog.ItemFactory.Enumerable.Json<global::Sample.Row>"),
      "An IItem<IEnumerable<T>> property must wire to Enumerable.Json<T>.");
    Assert.That(emitted, Does.Contain("\"Rows\""),
      "The label argument should default to the property name.");
    Assert.That(emitted, Does.Contain("\"data/rows.json\""),
      "The path argument should propagate from the JsonItem attribute.");
  }

  // ── Singleton shape ───────────────────────────────────────────────────

  [Test]
  public void SingletonIItemProperty_EmitsSingletonJsonFactory()
  {
    var source = """
      using Flowthru.Data.Catalog;
      namespace Sample;

      public record Config(int Threshold);

      public partial class Catalog : CatalogAbstract
      {
        [JsonItem("config.json")]
        public partial IItem<Config> Config { get; }
      }
      """;

    var emitted = EmitFirstSource(source);

    Assert.That(emitted,
      Does.Contain("global::Flowthru.Data.Catalog.ItemFactory.Singleton.Json<global::Sample.Config>"),
      "An IItem<T> property must wire to Singleton.Json<T>.");
    Assert.That(emitted, Does.Not.Contain("Enumerable.Json"),
      "A singleton property must not pick up the Enumerable.Json factory.");
  }

  // ── Path escaping ─────────────────────────────────────────────────────

  [Test]
  public void PathWithDoubleQuotes_IsEscapedInGeneratedString()
  {
    // The generator runs the path through SymbolDisplayFormatExt.EscapeStringLiteral
    // before splicing it into the generated source. A double-quote in
    // the path must be backslash-escaped so the emitted C# remains
    // valid.
    var source = """"
      using System.Collections.Generic;
      using Flowthru.Data.Catalog;
      namespace Sample;

      public record Row(int X);

      public partial class Catalog : CatalogAbstract
      {
        [JsonItem("path/with\"quote.json")]
        public partial IItem<IEnumerable<Row>> Rows { get; }
      }
      """";

    var emitted = EmitFirstSource(source);
    Assert.That(emitted, Does.Contain(@"path/with\""quote.json"),
      "Double quotes in the path must be backslash-escaped in the emitted string literal.");
  }

  // ── Multiple properties on one catalog ────────────────────────────────

  [Test]
  public void TwoCatalogPropertiesOnSameClass_EmitInSinglePartialFile()
  {
    // The generator groups emissions by containing class so the user
    // sees one `partial class` per file, not one per property.
    var source = """
      using System.Collections.Generic;
      using Flowthru.Data.Catalog;
      namespace Sample;

      public record Row(int X);
      public record Config(int Threshold);

      public partial class Catalog : CatalogAbstract
      {
        [JsonItem("rows.json")]
        public partial IItem<IEnumerable<Row>> Rows { get; }

        [JsonItem("config.json")]
        public partial IItem<Config> Config { get; }
      }
      """;

    var result = AnalyzerTestHarness.RunGenerator(
      new CatalogPropertyGenerator(),
      source,
      typeof(JsonItemAttribute).Assembly
    );

    var files = result.GeneratedSources
      .Where(g => g.HintName.Contains("Catalog.JsonItems.g.cs"))
      .ToList();
    Assert.That(files, Has.Count.EqualTo(1),
      "Both properties on one catalog class should collapse into a single emitted file. Generated: "
      + string.Join(", ", result.GeneratedSources.Select(g => g.HintName)));

    var emitted = files[0].Source;
    Assert.That(emitted, Does.Contain("Rows"));
    Assert.That(emitted, Does.Contain("Config"));
    Assert.That(emitted, Does.Contain("Enumerable.Json"));
    Assert.That(emitted, Does.Contain("Singleton.Json"));
  }

  // ── Hint-name / header ────────────────────────────────────────────────

  [Test]
  public void EmittedSource_HasAutoGeneratedHeaderAndExpectedHintName()
  {
    var source = """
      using System.Collections.Generic;
      using Flowthru.Data.Catalog;
      namespace Sample.Catalogs;

      public record Row(int X);

      public partial class MyCatalog : CatalogAbstract
      {
        [JsonItem("rows.json")]
        public partial IItem<IEnumerable<Row>> Rows { get; }
      }
      """;

    var result = AnalyzerTestHarness.RunGenerator(
      new CatalogPropertyGenerator(),
      source,
      typeof(JsonItemAttribute).Assembly
    );

    var match = result.GeneratedSources
      .FirstOrDefault(g => g.HintName == "Sample.Catalogs.MyCatalog.JsonItems.g.cs");
    Assert.That(match.Source, Is.Not.Null,
      "Hint name should be `{Namespace}.{TypeName}.JsonItems.g.cs`. Generated: "
      + string.Join(", ", result.GeneratedSources.Select(g => g.HintName)));
    Assert.That(match.Source, Does.Contain("// <auto-generated/>"));
    Assert.That(match.Source, Does.Contain("#nullable enable"));
    Assert.That(match.Source, Does.Contain("namespace Sample.Catalogs;"));
    Assert.That(match.Source, Does.Contain("partial class MyCatalog"));
  }

  // ── helper ────────────────────────────────────────────────────────────

  private static string EmitFirstSource(string source)
  {
    var result = AnalyzerTestHarness.RunGenerator(
      new CatalogPropertyGenerator(),
      source,
      typeof(JsonItemAttribute).Assembly
    );
    Assert.That(result.GeneratedSources, Is.Not.Empty,
      "Expected at least one generated file. Diagnostics: "
      + string.Join("; ", result.Diagnostics.Select(d => d.GetMessage())));
    return result.GeneratedSources[0].Source;
  }
}
