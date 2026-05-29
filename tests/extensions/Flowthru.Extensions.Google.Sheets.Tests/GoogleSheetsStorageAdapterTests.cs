using Flowthru.Data.Schema;
using Flowthru.Data.Storage;
using Flowthru.Data.Storage.Sheets;
using Flowthru.Data.Storage.Sheets.Internal;
using Flowthru.Data.Storage.Sheets.InMemory;
using Flowthru.Prelude;

namespace Flowthru.Extensions.Google.Sheets.Tests;

/// <summary>
/// Read-path tests for <see cref="GoogleSheetsStorageAdapter{TRow}"/>, driven
/// entirely against the offline <see cref="InMemorySheetsGateway"/> — no live
/// Google API. Exercises schema-driven decoding (serial Number → DateTime),
/// nullable/empty handling, column-order independence, and the minimal
/// required-field inspection.
/// </summary>
[TestFixture]
public sealed class GoogleSheetsStorageAdapterTests
{
  private const string SpreadsheetId = "ss-1";
  private const string TableName = "People";

  public enum Tier
  {
    [SerializedEnum("free")] Free,
    [SerializedEnum("pro")] Pro,
    [SerializedEnum("enterprise")] Enterprise,
  }

  // Serialized value the enum decoder reads, distinct from the C# name to prove
  // the [SerializedEnum] mapping (not Enum.Parse) drives decoding.
  private static string Serialized(Tier tier) => tier switch
  {
    Tier.Free => "free",
    Tier.Pro => "pro",
    Tier.Enterprise => "enterprise",
    _ => throw new ArgumentOutOfRangeException(nameof(tier)),
  };

  public sealed class Person : IFlatSchema
  {
    public string Name { get; set; } = string.Empty;
    public int Age { get; set; }
    public double Score { get; set; }
    public bool Active { get; set; }
    public DateTime JoinedOn { get; set; }
    public Tier Plan { get; set; }
    public string? Nickname { get; set; }
  }

  private static TableSchema PersonSchema() => new(new TableColumn[]
  {
    new("Name", ColumnType.Text),
    new("Age", ColumnType.Number),
    new("Score", ColumnType.Number),
    new("Active", ColumnType.Bool),
    new("JoinedOn", ColumnType.DateTime),
    new("Plan", ColumnType.Text),
    new("Nickname", ColumnType.Text),
  });

  private static IReadOnlyList<FieldValue> PersonRow(
    string name, int age, double score, bool active, DateTime joined, Tier plan, string? nickname)
    => new[]
    {
      FieldValue.Text(name),
      FieldValue.Number(age),
      FieldValue.Number(score),
      FieldValue.Bool(active),
      // Serial date as the gateway returns it on read — a Number, not Temporal.
      FieldValue.Number(SheetsTranslator.ToSerial(joined)),
      FieldValue.Text(Serialized(plan)),
      nickname is null ? FieldValue.Empty : FieldValue.Text(nickname),
    };

  private static async Task<A> Expect<A>(FlowIO<A> io)
  {
    var result = await io.Run();
    if (result is EffResult<A>.Failure failure)
    {
      Assert.Fail($"Expected success, got failure: {failure.Error.Message}");
    }
    return ((EffResult<A>.Success)result).Value;
  }

  [Test]
  public async Task Load_RoundTrips_AllPrimitiveKinds()
  {
    var joined = new DateTime(2023, 4, 15, 9, 30, 0);
    var gateway = new InMemorySheetsGateway();
    gateway.Seed(SpreadsheetId, TableName, PersonSchema(), new[]
    {
      PersonRow("Ada", 36, 99.5, true, joined, Tier.Pro, "Countess"),
    });

    var adapter = new GoogleSheetsStorageAdapter<Person>(SpreadsheetId, TableName, gateway);
    var rows = (await Expect(adapter.Load())).ToList();

    Assert.That(rows, Has.Count.EqualTo(1));
    var p = rows[0];
    Assert.Multiple(() =>
    {
      Assert.That(p.Name, Is.EqualTo("Ada"));
      Assert.That(p.Age, Is.EqualTo(36));
      Assert.That(p.Score, Is.EqualTo(99.5));
      Assert.That(p.Active, Is.True);
      // Serial-date encoding (days-as-double) loses sub-millisecond precision;
      // that is the honest behaviour of the round-trip, not an adapter bug.
      Assert.That(p.JoinedOn, Is.EqualTo(joined).Within(TimeSpan.FromMilliseconds(1)));
      Assert.That(p.Plan, Is.EqualTo(Tier.Pro));
      Assert.That(p.Nickname, Is.EqualTo("Countess"));
    });
  }

  [Test]
  public async Task Load_CoercesSerialNumber_ToDateTime_DrivenBySchema()
  {
    var joined = new DateTime(2020, 1, 1, 0, 0, 0);
    var gateway = new InMemorySheetsGateway();
    gateway.Seed(SpreadsheetId, TableName, PersonSchema(), new[]
    {
      PersonRow("Grace", 40, 1.0, false, joined, Tier.Free, null),
    });

    var adapter = new GoogleSheetsStorageAdapter<Person>(SpreadsheetId, TableName, gateway);
    var rows = (await Expect(adapter.Load())).ToList();

    // The field is a Number; it becomes DateTime only because the property is.
    Assert.That(rows[0].JoinedOn, Is.EqualTo(joined));
  }

  [Test]
  public async Task Load_LeavesNullable_AsNull_WhenFieldEmpty()
  {
    var gateway = new InMemorySheetsGateway();
    gateway.Seed(SpreadsheetId, TableName, PersonSchema(), new[]
    {
      PersonRow("Edsger", 50, 0, true, new DateTime(2021, 6, 1), Tier.Enterprise, null),
    });

    var adapter = new GoogleSheetsStorageAdapter<Person>(SpreadsheetId, TableName, gateway);
    var rows = (await Expect(adapter.Load())).ToList();

    Assert.That(rows[0].Nickname, Is.Null);
  }

  [Test]
  public async Task Load_MatchesColumns_ByName_IndependentOfOrder()
  {
    // Columns deliberately shuffled relative to property declaration order.
    var schema = new TableSchema(new TableColumn[]
    {
      new("Plan", ColumnType.Text),
      new("JoinedOn", ColumnType.DateTime),
      new("Name", ColumnType.Text),
      new("Active", ColumnType.Bool),
      new("Score", ColumnType.Number),
      new("Nickname", ColumnType.Text),
      new("Age", ColumnType.Number),
    });
    var joined = new DateTime(2022, 2, 2);
    var rowFields = new[]
    {
      FieldValue.Text(Serialized(Tier.Pro)),
      FieldValue.Number(SheetsTranslator.ToSerial(joined)),
      FieldValue.Text("Linus"),
      FieldValue.Bool(true),
      FieldValue.Number(7.0),
      FieldValue.Text("Penguin"),
      FieldValue.Number(33),
    };

    var gateway = new InMemorySheetsGateway();
    gateway.Seed(SpreadsheetId, TableName, schema, new[] { (IReadOnlyList<FieldValue>)rowFields });

    var adapter = new GoogleSheetsStorageAdapter<Person>(SpreadsheetId, TableName, gateway);
    var p = (await Expect(adapter.Load())).Single();

    Assert.Multiple(() =>
    {
      Assert.That(p.Name, Is.EqualTo("Linus"));
      Assert.That(p.Age, Is.EqualTo(33));
      Assert.That(p.Score, Is.EqualTo(7.0));
      Assert.That(p.Active, Is.True);
      Assert.That(p.JoinedOn, Is.EqualTo(joined));
      Assert.That(p.Plan, Is.EqualTo(Tier.Pro));
      Assert.That(p.Nickname, Is.EqualTo("Penguin"));
    });
  }

  [Test]
  public async Task Load_ToleratesExtraColumns_IgnoringUnmapped()
  {
    var schema = new TableSchema(new TableColumn[]
    {
      new("Name", ColumnType.Text),
      new("Age", ColumnType.Number),
      new("Score", ColumnType.Number),
      new("Active", ColumnType.Bool),
      new("JoinedOn", ColumnType.DateTime),
      new("Plan", ColumnType.Text),
      new("Nickname", ColumnType.Text),
      new("InternalNote", ColumnType.Text), // not on Person
    });
    var joined = new DateTime(2024, 1, 1);
    var fields = new List<FieldValue>(PersonRow("Margaret", 45, 5, true, joined, Tier.Pro, "Mags"))
    {
      FieldValue.Text("ignore me"),
    };

    var gateway = new InMemorySheetsGateway();
    gateway.Seed(SpreadsheetId, TableName, schema, new[] { (IReadOnlyList<FieldValue>)fields });

    var adapter = new GoogleSheetsStorageAdapter<Person>(SpreadsheetId, TableName, gateway);
    var p = (await Expect(adapter.Load())).Single();

    Assert.That(p.Name, Is.EqualTo("Margaret"));
    Assert.That(p.Nickname, Is.EqualTo("Mags"));
  }

  [Test]
  public async Task Load_FailsClearly_WhenTableMissing()
  {
    var gateway = new InMemorySheetsGateway();
    var adapter = new GoogleSheetsStorageAdapter<Person>(SpreadsheetId, "Nope", gateway);

    var result = await adapter.Load().Run();

    Assert.That(result, Is.InstanceOf<EffResult<IEnumerable<Person>>.Failure>());
    var failure = (EffResult<IEnumerable<Person>>.Failure)result;
    Assert.That(failure.Error.Message, Does.Contain("Nope"));
    Assert.That(failure.Error.Message, Does.Contain(SpreadsheetId));
  }

  [Test]
  public async Task Exists_TrueWhenTablePresent_FalseOtherwise()
  {
    var gateway = new InMemorySheetsGateway();
    gateway.Seed(SpreadsheetId, TableName, PersonSchema());

    var present = new GoogleSheetsStorageAdapter<Person>(SpreadsheetId, TableName, gateway);
    var absent = new GoogleSheetsStorageAdapter<Person>(SpreadsheetId, "Ghost", gateway);

    Assert.That(await Expect(present.Exists()), Is.True);
    Assert.That(await Expect(absent.Exists()), Is.False);
  }

  [Test]
  public async Task InspectShallow_Succeeds_WhenAllRequiredColumnsPresent()
  {
    var gateway = new InMemorySheetsGateway();
    gateway.Seed(SpreadsheetId, TableName, PersonSchema());
    var adapter = new GoogleSheetsStorageAdapter<Person>(SpreadsheetId, TableName, gateway);

    var result = await Expect(adapter.InspectShallow(0));

    Assert.That(result.IsValid, Is.True);
  }

  [Test]
  public async Task InspectShallow_FailsSchemaMismatch_WhenRequiredColumnMissing()
  {
    // Drop the required "Age" column; "Nickname" is nullable so its absence is fine.
    var schema = new TableSchema(new TableColumn[]
    {
      new("Name", ColumnType.Text),
      new("Score", ColumnType.Number),
      new("Active", ColumnType.Bool),
      new("JoinedOn", ColumnType.DateTime),
      new("Plan", ColumnType.Text),
    });
    var gateway = new InMemorySheetsGateway();
    gateway.Seed(SpreadsheetId, TableName, schema);
    var adapter = new GoogleSheetsStorageAdapter<Person>(SpreadsheetId, TableName, gateway);

    var result = await Expect(adapter.InspectShallow(0));

    Assert.That(result.HasErrors, Is.True);
    Assert.That(result.Errors[0].ErrorType, Is.EqualTo(ValidationErrorType.SchemaMismatch));
    Assert.That(result.Errors[0].Details, Does.Contain("Age"));
  }

  [Test]
  public async Task InspectShallow_FailsNotFound_WhenTableMissing()
  {
    var gateway = new InMemorySheetsGateway();
    var adapter = new GoogleSheetsStorageAdapter<Person>(SpreadsheetId, "Nope", gateway);

    var result = await Expect(adapter.InspectShallow(0));

    Assert.That(result.Errors[0].ErrorType, Is.EqualTo(ValidationErrorType.NotFound));
  }

  [Test]
  public async Task InspectDeep_Succeeds_WhenRowsDecode()
  {
    var gateway = new InMemorySheetsGateway();
    gateway.Seed(SpreadsheetId, TableName, PersonSchema(), new[]
    {
      PersonRow("Tim", 30, 1, true, new DateTime(2025, 5, 5), Tier.Free, null),
    });
    var adapter = new GoogleSheetsStorageAdapter<Person>(SpreadsheetId, TableName, gateway);

    var result = await Expect(adapter.InspectDeep());

    Assert.That(result.IsValid, Is.True);
  }
}
