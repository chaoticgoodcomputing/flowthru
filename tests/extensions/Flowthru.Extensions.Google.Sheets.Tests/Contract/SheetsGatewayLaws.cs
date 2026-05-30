using Flowthru.Data.Storage.Sheets;
using Flowthru.Data.Storage.Sheets.Internal;
using Flowthru.Extensions.Google.Sheets.Tests.Backends;
using Flowthru.Extensions.Google.Sheets.Tests.Support;
using Flowthru.Tests.Kits.Prelude;

namespace Flowthru.Extensions.Google.Sheets.Tests.Contract;

/// <summary>
/// The <see cref="ISheetsGateway"/> contract as backend-agnostic laws, run
/// identically over every <see cref="ISheetsGatewayBackend"/> via
/// <c>[TestFixture(typeof(...))]</c> — the Sheets analogue of
/// <c>EFCoreResourceLaws</c>'s backend matrix. The offline tier
/// (<see cref="OfflineSheetsBackend"/>) always runs; the live tier
/// (<see cref="LiveGoogleSheetsBackend"/>) gates itself on
/// <see cref="TestCapabilities.GoogleSheetsCredentials"/> and reports
/// Inconclusive when no test sheet + credential is configured, so the default
/// flow stays green on CI.
/// </summary>
/// <remarks>
/// <para>
/// This kit reuses the backend-matrix <em>shape</em> and the capability gate
/// from <c>Flowthru.Tests.Kits.Prelude</c>, but defines its own backend
/// interface rather than inheriting <c>FlowResourceLaws</c>: the gateway is a
/// behavioral seam (resolve / add / replace / read), not an acquire/release
/// bracket, so the resource-lifecycle laws do not apply. See
/// <see cref="ISheetsGatewayBackend"/> for the full rationale.
/// </para>
/// <para>
/// Every law calls <see cref="Fresh"/> for a disjoint context, so tests never
/// observe each other's tables. The laws touch only <see cref="ISheetsGateway"/>
/// and the neutral tabular types — no Google SDK type, no backend specifics.
/// </para>
/// </remarks>
/// <typeparam name="TBackend">
/// Backend under test. Bound by NUnit via the <c>[TestFixture(typeof(...))]</c>
/// attributes on this class.
/// </typeparam>
[TestFixture(typeof(OfflineSheetsBackend))]
[TestFixture(typeof(LiveGoogleSheetsBackend))]
[Category("Sheets")]
[Category("Laws")]
public sealed class SheetsGatewayLaws<TBackend>
  where TBackend : ISheetsGatewayBackend, new()
{
  private TBackend _backend = default!;

  // ── Capability gate + shared setup (mirrors FlowResourceLaws) ─────────────

  [OneTimeSetUp]
  public async Task GateAndInitialiseBackend()
  {
    _backend = new TBackend();
    foreach (var capability in _backend.RequiredCapabilities)
    {
      Assume.That(
        capability.IsAvailable(),
        $"[{capability.Name}] {capability.MissingMessage}");
    }
    await _backend.InitializeAsync();
  }

  [OneTimeTearDown]
  public async Task ReleaseBackendResources()
  {
    if (_backend is not null)
    {
      await _backend.Cleanup();
    }
  }

  private SheetsGatewayContext Fresh() => _backend.CreateResource();

  // ── Laws ──────────────────────────────────────────────────────────────────

  /// <summary>
  /// A seeded table resolves to itself; an absent table resolves to
  /// <see langword="null"/> (the create-if-absent / pre-flight branch point).
  /// </summary>
  [Test]
  public async Task SeededTableResolves_AbsentTableIsNull()
  {
    var ctx = Fresh();
    var name = ctx.Table("Resolve");
    var schema = TextSchema("Name");

    await ctx.Gateway.AddTable(ctx.SpreadsheetId, name, schema, default);

    var resolved = await ctx.Gateway.ResolveTable(ctx.SpreadsheetId, name, default);
    Assert.That(resolved, Is.Not.Null, "A created table should resolve.");
    Assert.That(resolved!.Name, Is.EqualTo(name));

    var absent = await ctx.Gateway.ResolveTable(ctx.SpreadsheetId, ctx.Table("NoSuchTable"), default);
    Assert.That(absent, Is.Null, "An absent table should resolve to null, not throw.");
  }

  /// <summary>
  /// <see cref="ISheetsGateway.AddTable"/> creates a table carrying the given
  /// schema; creating a second table with the same name throws.
  /// </summary>
  [Test]
  public async Task AddTable_CreatesWithSchema_AndRejectsDuplicateName()
  {
    var ctx = Fresh();
    var name = ctx.Table("Dup");
    var schema = new TableSchema(new[]
    {
      new TableColumn("Name", ColumnType.Text),
      new TableColumn("Amount", ColumnType.Number),
    });

    var created = await ctx.Gateway.AddTable(ctx.SpreadsheetId, name, schema, default);
    Assert.That(created.Schema.Columns.Select(c => c.Name),
      Is.EqualTo(new[] { "Name", "Amount" }));

    Assert.That(
      async () => await ctx.Gateway.AddTable(ctx.SpreadsheetId, name, schema, default),
      Throws.Exception,
      "Creating a duplicate-named table should throw.");
  }

  /// <summary>
  /// <see cref="ISheetsGateway.ReplaceRows"/> is an atomic replace: a second
  /// write with fewer rows leaves no stale trailing rows, and the header /
  /// columns are preserved.
  /// </summary>
  [Test]
  public async Task ReplaceRows_IsAtomicReplace_NoStaleTrailingRows()
  {
    var ctx = Fresh();
    var name = ctx.Table("Replace");
    var schema = TextSchema("Name");
    var created = await ctx.Gateway.AddTable(ctx.SpreadsheetId, name, schema, default);

    await ctx.Gateway.ReplaceRows(ctx.SpreadsheetId, created,
      new TableData(schema, new[]
      {
        Row(FieldValue.Text("a")),
        Row(FieldValue.Text("b")),
        Row(FieldValue.Text("c")),
      }), default);

    // Re-resolve so the range reflects the now-larger table before shrinking it.
    var afterThree = await ctx.Gateway.ResolveTable(ctx.SpreadsheetId, name, default);
    await ctx.Gateway.ReplaceRows(ctx.SpreadsheetId, afterThree!,
      new TableData(afterThree!.Schema, new[] { Row(FieldValue.Text("only")) }), default);

    var resolved = await ctx.Gateway.ResolveTable(ctx.SpreadsheetId, name, default);
    var read = await ctx.Gateway.ReadRows(ctx.SpreadsheetId, resolved!, default);

    Assert.That(read.RowCount, Is.EqualTo(1), "Stale trailing rows should be gone.");
    Assert.That(read.Rows[0][0], Is.EqualTo(FieldValue.Text("only")));
    Assert.That(resolved!.Schema.Columns.Select(c => c.Name),
      Is.EqualTo(new[] { "Name" }), "Header / columns should be preserved by a replace.");
  }

  /// <summary>
  /// A typed row (Text / Number / Bool + a Date) round-trips through
  /// write-then-read, and the Date column reads back as a <em>serial Number</em>
  /// (the live representation; the offline store normalizes to match).
  /// </summary>
  [Test]
  public async Task ReadRows_RoundTripsTypedRow_DateReadsAsSerialNumber()
  {
    var ctx = Fresh();
    var name = ctx.Table("Typed");
    var schema = new TableSchema(new[]
    {
      new TableColumn("Name", ColumnType.Text),
      new TableColumn("Amount", ColumnType.Number),
      new TableColumn("Flag", ColumnType.Bool),
      new TableColumn("When", ColumnType.Date),
    });
    var created = await ctx.Gateway.AddTable(ctx.SpreadsheetId, name, schema, default);

    var date = new DateTime(2026, 1, 15, 0, 0, 0, DateTimeKind.Unspecified);
    await ctx.Gateway.ReplaceRows(ctx.SpreadsheetId, created,
      new TableData(schema, new[]
      {
        Row(
          FieldValue.Text("alpha"),
          FieldValue.Number(12.5),
          FieldValue.Bool(true),
          FieldValue.Temporal(date, TemporalKind.Date)),
      }), default);

    var resolved = await ctx.Gateway.ResolveTable(ctx.SpreadsheetId, name, default);
    var read = await ctx.Gateway.ReadRows(ctx.SpreadsheetId, resolved!, default);

    Assert.That(read.RowCount, Is.EqualTo(1));
    var row = read.Rows[0];
    Assert.That(row[0], Is.EqualTo(FieldValue.Text("alpha")));
    Assert.That(row[1], Is.EqualTo(FieldValue.Number(12.5)));
    Assert.That(row[2], Is.EqualTo(FieldValue.Bool(true)));

    // The contract: a Date column reads back as a serial Number, never a
    // Temporal. The serial is Flowthru's own epoch math (SheetsTranslator.ToSerial),
    // which the write path uses, so write→read is self-consistent across backends.
    Assert.That(row[3].Kind, Is.EqualTo(FieldKind.Number),
      "A Date column should read back as a serial Number, not a Temporal.");
    Assert.That(row[3].NumberValue, Is.EqualTo(SheetsTranslator.ToSerial(date)),
      "The serial should be the value the write path emitted.");
  }

  /// <summary>
  /// Column types survive a create→resolve round trip: a table created with
  /// <c>TEXT</c> / <c>DOUBLE</c> / <c>DATE_TIME</c> columns reports those neutral
  /// types back.
  /// </summary>
  [Test]
  public async Task ColumnTypes_RoundTrip_TextNumberDateTime()
  {
    var ctx = Fresh();
    var name = ctx.Table("Types");
    var schema = new TableSchema(new[]
    {
      new TableColumn("Name", ColumnType.Text),
      new TableColumn("Amount", ColumnType.Number),
      new TableColumn("When", ColumnType.DateTime),
    });

    await ctx.Gateway.AddTable(ctx.SpreadsheetId, name, schema, default);
    var resolved = await ctx.Gateway.ResolveTable(ctx.SpreadsheetId, name, default);

    Assert.That(resolved, Is.Not.Null);
    Assert.That(resolved!.Schema.Columns.Select(c => c.Type),
      Is.EqualTo(new[] { ColumnType.Text, ColumnType.Number, ColumnType.DateTime }));
  }

  // ── Helpers ─────────────────────────────────────────────────────────────────

  private static TableSchema TextSchema(string column) =>
    new(new[] { new TableColumn(column, ColumnType.Text) });

  private static IReadOnlyList<FieldValue> Row(params FieldValue[] fields) => fields;
}
