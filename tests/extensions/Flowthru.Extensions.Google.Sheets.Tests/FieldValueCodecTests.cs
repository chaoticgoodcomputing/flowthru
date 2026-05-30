using Flowthru.Data.Schema;
using Flowthru.Data.Storage.Sheets;
using Flowthru.Data.Storage.Sheets.Internal;
using Flowthru.Data.Storage.Sheets.InMemory;
using Flowthru.Prelude;

namespace Flowthru.Extensions.Google.Sheets.Tests;

/// <summary>
/// Codec coverage for the schema-driven encode/decode the adapter rides on,
/// exercised end-to-end through the adapter's <see cref="GoogleSheetsStorageAdapter{TRow}.Load"/>
/// against the offline gateway. Two themes: the #94 date-fidelity contract (a
/// date column seeded as Temporal vs as a serial Number decodes identically),
/// and the BCL-scalar / IScalar / enum edges of
/// <see cref="FieldValueEncoder"/> / <see cref="FieldValueDecoder"/> that the
/// happy-path Person schema does not reach.
/// </summary>
[TestFixture]
public sealed class FieldValueCodecTests
{
  private const string SpreadsheetId = "ss-codec";
  private const string TableName = "T";

  private static async Task<A> Expect<A>(FlowIO<A> io)
  {
    var result = await io.Run();
    if (result is EffResult<A>.Failure failure)
    {
      Assert.Fail($"Expected success, got failure: {failure.Error.Message}");
    }
    return ((EffResult<A>.Success)result).Value;
  }

  private static async Task<EffResult<A>> RunFor<A>(FlowIO<A> io) => await io.Run();

  // ── #94: date fidelity through the adapter's Load ─────────────────────────

  public sealed class DatedRow : IFlatSchema
  {
    public DateOnly SoldOn { get; set; }
  }

  [Test]
  public async Task Load_DateColumn_DecodesIdentically_WhetherSeededTemporalOrSerial()
  {
    var soldOn = new DateOnly(2026, 5, 1);
    var asDateTime = soldOn.ToDateTime(TimeOnly.MinValue);
    var serial = SheetsTranslator.ToSerial(asDateTime);
    var schema = new TableSchema(new[] { new TableColumn("SoldOn", ColumnType.Date) });

    var temporalGateway = new InMemorySheetsGateway();
    temporalGateway.Seed(SpreadsheetId, TableName, schema, new[]
    {
      new[] { FieldValue.Temporal(asDateTime, TemporalKind.Date) },
    });

    var numberGateway = new InMemorySheetsGateway();
    numberGateway.Seed(SpreadsheetId, TableName, schema, new[]
    {
      new[] { FieldValue.Number(serial) },
    });

    var fromTemporal = (await Expect(
      new GoogleSheetsStorageAdapter<DatedRow>(SpreadsheetId, TableName, temporalGateway).Load()))
      .Single();
    var fromNumber = (await Expect(
      new GoogleSheetsStorageAdapter<DatedRow>(SpreadsheetId, TableName, numberGateway).Load()))
      .Single();

    Assert.Multiple(() =>
    {
      Assert.That(fromTemporal.SoldOn, Is.EqualTo(soldOn));
      Assert.That(fromNumber.SoldOn, Is.EqualTo(soldOn),
        "the #94 acceptance: both seedings round-trip to the same DateOnly");
    });
  }

  // ── BCL scalar struct round-trip (Guid / TimeSpan) ────────────────────────

  public sealed class ScalarStructRow : IFlatSchema
  {
    public Guid Id { get; set; }
    public TimeSpan Elapsed { get; set; }
  }

  [Test]
  public async Task Save_Load_RoundTrips_BclScalarStructs_ViaText()
  {
    var id = Guid.NewGuid();
    var elapsed = TimeSpan.FromMinutes(90);
    var gateway = new InMemorySheetsGateway();
    gateway.RegisterSpreadsheet(SpreadsheetId);

    var adapter = new GoogleSheetsStorageAdapter<ScalarStructRow>(SpreadsheetId, TableName, gateway);
    await Expect(adapter.Save(new[] { new ScalarStructRow { Id = id, Elapsed = elapsed } }));

    var read = (await Expect(adapter.Load())).Single();
    Assert.Multiple(() =>
    {
      Assert.That(read.Id, Is.EqualTo(id), "a Guid encodes to its canonical text and parses back");
      Assert.That(read.Elapsed, Is.EqualTo(elapsed), "a TimeSpan round-trips through its text form");
    });
  }

  // ── IScalar (NewType) wrapper unwraps to its backing primitive ────────────

  public readonly record struct CustomerId(string Value) : IScalar;
  public readonly record struct Quantity(int Value) : IScalar;

  public sealed class ScalarRow : IFlatSchema
  {
    public CustomerId Customer { get; set; }
    public Quantity Count { get; set; }
  }

  [Test]
  public async Task Save_Load_RoundTrips_IScalarWrappers_ToBackingPrimitive()
  {
    var gateway = new InMemorySheetsGateway();
    gateway.RegisterSpreadsheet(SpreadsheetId);

    var adapter = new GoogleSheetsStorageAdapter<ScalarRow>(SpreadsheetId, TableName, gateway);
    await Expect(adapter.Save(new[]
    {
      new ScalarRow { Customer = new CustomerId("acme"), Count = new Quantity(7) },
    }));

    // The number-backed NewType must be created as a Number column, not text.
    var resolved = await gateway.ResolveTable(SpreadsheetId, TableName, default);
    var countColumn = resolved!.Schema.Columns.Single(c => c.Name == "Count");
    Assert.That(countColumn.Type, Is.EqualTo(ColumnType.Number));

    var read = (await Expect(adapter.Load())).Single();
    Assert.Multiple(() =>
    {
      Assert.That(read.Customer, Is.EqualTo(new CustomerId("acme")));
      Assert.That(read.Count, Is.EqualTo(new Quantity(7)));
    });
  }

  // ── enum decode failure surfaces a clear schema error ─────────────────────

  public enum Tier
  {
    [SerializedEnum("free")] Free,
    [SerializedEnum("pro")] Pro,
  }

  public sealed class EnumRow : IFlatSchema
  {
    public Tier Plan { get; set; }
  }

  [Test]
  public async Task Load_FailsToDecode_WhenEnumValueHasNoSerializedMapping()
  {
    // A text value that is not one of the [SerializedEnum] labels has no enum
    // member, so the decode throws and the adapter surfaces a Load failure.
    var schema = new TableSchema(new[] { new TableColumn("Plan", ColumnType.Text) });
    var gateway = new InMemorySheetsGateway();
    gateway.Seed(SpreadsheetId, TableName, schema, new[]
    {
      new[] { FieldValue.Text("platinum") },
    });

    var adapter = new GoogleSheetsStorageAdapter<EnumRow>(SpreadsheetId, TableName, gateway);
    var result = await RunFor(adapter.Load());

    Assert.That(result, Is.InstanceOf<EffResult<IEnumerable<EnumRow>>.Failure>(),
      "an unmapped enum label cannot decode, so Load fails rather than guessing");
  }

  [Test]
  public async Task Load_DecodesEnum_ViaSerializedLabel_NotCsName()
  {
    var schema = new TableSchema(new[] { new TableColumn("Plan", ColumnType.Text) });
    var gateway = new InMemorySheetsGateway();
    gateway.Seed(SpreadsheetId, TableName, schema, new[]
    {
      new[] { FieldValue.Text("pro") },
    });

    var adapter = new GoogleSheetsStorageAdapter<EnumRow>(SpreadsheetId, TableName, gateway);
    var read = (await Expect(adapter.Load())).Single();
    Assert.That(read.Plan, Is.EqualTo(Tier.Pro));
  }

  // ── Save encodes every temporal shape to a serial that decodes back ───────

  public sealed class AllTemporalRow : IFlatSchema
  {
    public DateTime At { get; set; }
    public DateOnly On { get; set; }
    public TimeOnly Clock { get; set; }
  }

  [Test]
  public async Task Save_Load_RoundTrips_DateTime_DateOnly_AndTimeOnly()
  {
    var row = new AllTemporalRow
    {
      At = new DateTime(2025, 3, 4, 5, 6, 7),
      On = new DateOnly(2025, 3, 4),
      Clock = new TimeOnly(13, 14, 15),
    };
    var gateway = new InMemorySheetsGateway();
    gateway.RegisterSpreadsheet(SpreadsheetId);
    var adapter = new GoogleSheetsStorageAdapter<AllTemporalRow>(SpreadsheetId, TableName, gateway);

    // Each temporal shape gets its own dedicated column type on create.
    await Expect(adapter.Save(new[] { row }));
    var resolved = await gateway.ResolveTable(SpreadsheetId, TableName, default);
    Assert.Multiple(() =>
    {
      Assert.That(resolved!.Schema.Columns.Single(c => c.Name == "At").Type,
        Is.EqualTo(ColumnType.DateTime));
      Assert.That(resolved.Schema.Columns.Single(c => c.Name == "On").Type,
        Is.EqualTo(ColumnType.Date));
      Assert.That(resolved.Schema.Columns.Single(c => c.Name == "Clock").Type,
        Is.EqualTo(ColumnType.Time));
    });

    var read = (await Expect(adapter.Load())).Single();
    Assert.Multiple(() =>
    {
      Assert.That(read.At, Is.EqualTo(row.At).Within(TimeSpan.FromMilliseconds(1)));
      Assert.That(read.On, Is.EqualTo(row.On));
      // Serial encoding (days-as-double) loses sub-millisecond precision, and
      // TimeOnly has no NUnit tolerance overload — compare at second resolution.
      Assert.That(
        read.Clock.ToTimeSpan().TotalSeconds,
        Is.EqualTo(row.Clock.ToTimeSpan().TotalSeconds).Within(0.5));
    });
  }

  // ── Empty field leaves a non-nullable property at its CLR default ──────────

  public sealed class OptionalRow : IFlatSchema
  {
    public string Name { get; set; } = string.Empty;
    public int? Maybe { get; set; }
  }

  [Test]
  public async Task Load_EmptyField_LeavesNullableNull_AndKeepsDefault()
  {
    var schema = new TableSchema(new[]
    {
      new TableColumn("Name", ColumnType.Text),
      new TableColumn("Maybe", ColumnType.Number),
    });
    var gateway = new InMemorySheetsGateway();
    gateway.Seed(SpreadsheetId, TableName, schema, new[]
    {
      new[] { FieldValue.Text("x"), FieldValue.Empty },
    });

    var read = (await Expect(
      new GoogleSheetsStorageAdapter<OptionalRow>(SpreadsheetId, TableName, gateway).Load()))
      .Single();
    Assert.Multiple(() =>
    {
      Assert.That(read.Name, Is.EqualTo("x"));
      Assert.That(read.Maybe, Is.Null, "an empty cell leaves the nullable property null");
    });
  }

  // ── Encode-side enum with no serialized mapping fails on Save ─────────────

  // An enum where one member carries NO [SerializedEnum] label has no forward
  // mapping, so encoding that value on Save throws — the write-side mirror of the
  // decode failure.
  public enum PartlyLabeled
  {
    [SerializedEnum("ok")] Ok,
    Unlabeled,
  }

  public sealed class PartlyLabeledRow : IFlatSchema
  {
    public PartlyLabeled State { get; set; }
  }

  [Test]
  public async Task Save_FailsToEncode_WhenEnumValueHasNoSerializedMapping()
  {
    var gateway = new InMemorySheetsGateway();
    gateway.RegisterSpreadsheet(SpreadsheetId);
    var adapter = new GoogleSheetsStorageAdapter<PartlyLabeledRow>(SpreadsheetId, TableName, gateway);

    var result = await RunFor(
      adapter.Save(new[] { new PartlyLabeledRow { State = PartlyLabeled.Unlabeled } }));

    Assert.That(result, Is.InstanceOf<EffResult<FlowUnit>.Failure>(),
      "an enum value with no serialized form cannot be written");
  }
}
