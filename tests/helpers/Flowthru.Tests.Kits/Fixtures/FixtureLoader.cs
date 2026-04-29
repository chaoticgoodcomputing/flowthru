using Flowthru.Core.Abstractions;
using Flowthru.Core.Data.Storage.Format;

namespace Flowthru.Tests.Kits.Fixtures;

/// <summary>
/// Loads JSON fixture data from <c>Flowthru.Tests.Kits/Fixtures/</c> via Core's
/// <see cref="JsonFormatSerializer{TRow}"/>.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Why JSON.</strong> JSON is the only first-party format in Core, so the kit can
/// deserialize fixtures without taking on an extension dependency. Every conforming format
/// extension that needs sample data calls into this loader; the resulting rows then feed
/// the round-trip test against the format-under-test.
/// </para>
/// <para>
/// <strong>Path resolution.</strong> Fixture paths are relative to <c>Fixtures/</c>. They
/// resolve against <see cref="AppContext.BaseDirectory"/> at test time — every consuming
/// project copies <c>Fixtures/**/*.json</c> to its output directory via the
/// <c>&lt;Content Include="Fixtures/**/*.json"&gt;</c> entry that the kit's csproj contributes
/// transitively.
/// </para>
/// </remarks>
public static class FixtureLoader
{
  /// <summary>
  /// Loads a JSON fixture as a list of rows of the given <typeparamref name="TRow"/> schema.
  /// </summary>
  /// <typeparam name="TRow">Row schema; must satisfy the constraints of
  /// <see cref="JsonFormatSerializer{TRow}"/>.</typeparam>
  /// <param name="relativePath">Path under <c>Fixtures/</c>, e.g.
  /// <c>"Flat/Simple/rows.json"</c>.</param>
  public static async Task<List<TRow>> LoadAsync<TRow>(string relativePath)
    where TRow : notnull, IStructuredSerializable
  {
    var fullPath = Resolve(relativePath);

    using var stream = File.OpenRead(fullPath);
    var serializer = new JsonFormatSerializer<TRow>();

    var rows = new List<TRow>();
    await foreach (var row in serializer.DeserializeRows(stream))
    {
      rows.Add(row);
    }
    return rows;
  }

  /// <summary>
  /// Synchronous convenience for callers that don't need async semantics
  /// (e.g. NUnit <c>[TestCaseSource]</c> setup paths).
  /// </summary>
  public static List<TRow> Load<TRow>(string relativePath)
    where TRow : notnull, IStructuredSerializable
  {
    return LoadAsync<TRow>(relativePath).GetAwaiter().GetResult();
  }

  /// <summary>
  /// Resolves a fixture-relative path to an absolute path under the test output's
  /// <c>Fixtures/</c> directory. Throws if the file is missing — this means either the
  /// fixture name is wrong or the consuming project failed to copy <c>Fixtures/**</c> to
  /// its output directory.
  /// </summary>
  public static string Resolve(string relativePath)
  {
    var fullPath = Path.Combine(AppContext.BaseDirectory, "Fixtures", relativePath);
    if (!File.Exists(fullPath))
    {
      throw new FileNotFoundException(
        $"Fixture not found at '{fullPath}'. "
          + "Verify the path is correct relative to Fixtures/ and that the consuming "
          + "test project's csproj transitively copies Fixtures/**/*.json to its output "
          + "(e.g. via <Content Include=\"Fixtures/**/*.json\"> with CopyToOutputDirectory=PreserveNewest).",
        fullPath
      );
    }
    return fullPath;
  }
}
