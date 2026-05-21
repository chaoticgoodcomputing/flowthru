using System.Text;
using System.Text.Json;
using Flowthru.Data.Storage;
using Flowthru.Data.Storage.Csv;
using Flowthru.Tests.Kits.Storage;

namespace Flowthru.Extensions.Csv.Tests;

/// <summary>
/// Runs the <see cref="IInspectShallowLaws{TContainer}"/> kit
/// against a <see cref="ComposedStorageAdapter{TContainer, TRow}"/>
/// composed of <see cref="CsvFormatSerializer{TRow}"/> + a filesystem
/// medium + an <see cref="EnumerableContainerAdapter{TRow}"/>. The
/// projection parses the canonical JSON object and writes a
/// single-row CSV with the same field-name set, preserving absence
/// and order.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Known gap: CSV's InspectShallow does not yet implement
/// partial-match.</strong> CsvHelper's default header-validation
/// requires every schema column (including optionals) to be present
/// in the data, and bails at the first missing header rather than
/// emitting a complete diff. Both behaviors violate the
/// <see cref="IStorageAdapter{T}.InspectShallow"/> contract:
/// </para>
/// <list type="bullet">
///   <item>Optional fields ought to be tolerable when absent.</item>
///   <item>Missing required fields should surface as
///     <c>SchemaMismatch</c> with the full set of missing fields named
///     in one diff, not stop at the first one.</item>
/// </list>
/// <para>
/// <see cref="EnforcesPartialMatchOnInspectShallow"/> is set to
/// <c>false</c> until the CSV adapter is brought into conformance —
/// likely by walking the CSV header row up front, comparing to
/// <see cref="Flowthru.Data.Schema.Mapping.PropertyMappingPlan{TRow}.RequiredFieldNames"/>,
/// and emitting the diff before deferring to CsvHelper's row-iteration.
/// </para>
/// </remarks>
public sealed class CsvInspectShallowLaws
  : IInspectShallowLaws<IEnumerable<InspectShallowKitRow>>
{
  /// <inheritdoc/>
  protected override bool EnforcesPartialMatchOnInspectShallow => false;

  /// <inheritdoc/>
  protected override Task<byte[]> ProjectJsonPayloadAsync(string jsonPayload)
  {
    // Parse the JSON object and write a header line + one values line
    // using only the keys that appear in the JSON. This preserves the
    // canonical fixture's field-set verbatim — including absences —
    // without going through the typed Save path (which would reject
    // payloads with missing required fields).
    using var doc = JsonDocument.Parse(jsonPayload);
    var root = doc.RootElement;
    var headers = new List<string>();
    var values = new List<string>();
    foreach (var property in root.EnumerateObject())
    {
      headers.Add(property.Name);
      values.Add(StringifyJsonValue(property.Value));
    }

    var sb = new StringBuilder();
    sb.AppendLine(string.Join(',', headers.Select(EscapeCsv)));
    sb.AppendLine(string.Join(',', values.Select(EscapeCsv)));
    return Task.FromResult(Encoding.UTF8.GetBytes(sb.ToString()));
  }

  /// <inheritdoc/>
  protected override IStorageAdapter<IEnumerable<InspectShallowKitRow>> CreateAdapter(string filePath) =>
    new ComposedStorageAdapter<IEnumerable<InspectShallowKitRow>, InspectShallowKitRow>(
      new FileStorageMedium(filePath),
      new CsvFormatSerializer<InspectShallowKitRow>(),
      new EnumerableContainerAdapter<InspectShallowKitRow>()
    );

  /// <inheritdoc/>
  protected override string FileExtension => ".csv";

  // ── JSON-to-CSV projection helpers ─────────────────────────────────────

  private static string StringifyJsonValue(JsonElement element) =>
    element.ValueKind switch
    {
      JsonValueKind.String => element.GetString() ?? string.Empty,
      JsonValueKind.Number => element.GetRawText(),
      JsonValueKind.True or JsonValueKind.False => element.GetRawText(),
      JsonValueKind.Null => string.Empty,
      _ => element.GetRawText(),
    };

  private static string EscapeCsv(string value) =>
    value.Contains(',') || value.Contains('"') || value.Contains('\n')
      ? $"\"{value.Replace("\"", "\"\"")}\""
      : value;
}
