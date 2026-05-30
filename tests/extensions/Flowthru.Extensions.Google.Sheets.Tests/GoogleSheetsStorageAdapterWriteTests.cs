using Flowthru.Data.Catalog;
using Flowthru.Data.Schema;
using Flowthru.Data.Storage;
using Flowthru.Data.Storage.Sheets;
using Flowthru.Data.Storage.Sheets.InMemory;
using Flowthru.Prelude;
using Flowthru.Validation.Runtime;

namespace Flowthru.Extensions.Google.Sheets.Tests;

/// <summary>
/// Write-path tests for <see cref="GoogleSheetsStorageAdapter{TRow}"/>, driven
/// against the offline <see cref="InMemorySheetsGateway"/>. Exercises the
/// encode + create-if-absent + atomic-replace default, the <c>saveFunc</c>
/// override, and the read-only guard — round-tripping through the read path to
/// prove the encoder is the decoder's inverse.
/// </summary>
[TestFixture]
public sealed class GoogleSheetsStorageAdapterWriteTests
{
  private const string SpreadsheetId = "ss-write";
  private const string TableName = "People";

  public enum Tier
  {
    [SerializedEnum("free")] Free,
    [SerializedEnum("pro")] Pro,
    [SerializedEnum("enterprise")] Enterprise,
  }

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

  private static Person SamplePerson(string name = "Ada") => new()
  {
    Name = name,
    Age = 36,
    Score = 99.5,
    Active = true,
    JoinedOn = new DateTime(2023, 4, 15, 9, 30, 0),
    Plan = Tier.Pro,
    Nickname = "Countess",
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
  public async Task Save_Then_Load_RoundTrips_AllPrimitiveKinds()
  {
    var gateway = new InMemorySheetsGateway();
    gateway.Seed(SpreadsheetId, TableName, PersonSchema());
    var adapter = new GoogleSheetsStorageAdapter<Person>(SpreadsheetId, TableName, gateway);

    var input = SamplePerson();
    await Expect(adapter.Save(new[] { input }));

    var output = (await Expect(adapter.Load())).Single();
    Assert.Multiple(() =>
    {
      Assert.That(output.Name, Is.EqualTo(input.Name));
      Assert.That(output.Age, Is.EqualTo(input.Age));
      Assert.That(output.Score, Is.EqualTo(input.Score));
      Assert.That(output.Active, Is.EqualTo(input.Active));
      Assert.That(output.JoinedOn, Is.EqualTo(input.JoinedOn).Within(TimeSpan.FromMilliseconds(1)));
      Assert.That(output.Plan, Is.EqualTo(input.Plan));
      Assert.That(output.Nickname, Is.EqualTo(input.Nickname));
    });
  }

  [Test]
  public async Task Save_CreatesTable_FromRowSchema_WhenAbsent()
  {
    var gateway = new InMemorySheetsGateway();
    // The spreadsheet exists but holds no table yet — create-if-absent (Save)
    // creates the table, but Flowthru never creates the spreadsheet.
    gateway.RegisterSpreadsheet(SpreadsheetId);
    var adapter = new GoogleSheetsStorageAdapter<Person>(SpreadsheetId, TableName, gateway);

    Assert.That(await Expect(adapter.Exists()), Is.False, "precondition: table absent");

    await Expect(adapter.Save(new[] { SamplePerson("Grace") }));

    var resolved = await gateway.ResolveTable(SpreadsheetId, TableName, CancellationToken.None);
    Assert.That(resolved, Is.Not.Null);

    // Schema derived from Person's properties, in declaration order, with the
    // CLR-type → ColumnType mapping (DateTime → DateTime, bool → Bool, etc.).
    var byName = resolved!.Schema.Columns.ToDictionary(c => c.Name, c => c.Type);
    Assert.Multiple(() =>
    {
      Assert.That(resolved.Schema.Columns.Select(c => c.Name),
        Is.EqualTo(new[] { "Name", "Age", "Score", "Active", "JoinedOn", "Plan", "Nickname" }));
      Assert.That(byName["Name"], Is.EqualTo(ColumnType.Text));
      Assert.That(byName["Age"], Is.EqualTo(ColumnType.Number));
      Assert.That(byName["Score"], Is.EqualTo(ColumnType.Number));
      Assert.That(byName["Active"], Is.EqualTo(ColumnType.Bool));
      Assert.That(byName["JoinedOn"], Is.EqualTo(ColumnType.DateTime));
      Assert.That(byName["Plan"], Is.EqualTo(ColumnType.Text));
      Assert.That(byName["Nickname"], Is.EqualTo(ColumnType.Text));
    });

    // And the created table is immediately readable.
    var rows = (await Expect(adapter.Load())).ToList();
    Assert.That(rows, Has.Count.EqualTo(1));
    Assert.That(rows[0].Name, Is.EqualTo("Grace"));
  }

  [Test]
  public async Task Save_ReplaceSemantics_LeavesNoStaleTrailingRows()
  {
    var gateway = new InMemorySheetsGateway();
    gateway.Seed(SpreadsheetId, TableName, PersonSchema());
    var adapter = new GoogleSheetsStorageAdapter<Person>(SpreadsheetId, TableName, gateway);

    await Expect(adapter.Save(new[]
    {
      SamplePerson("A"), SamplePerson("B"), SamplePerson("C"),
    }));
    Assert.That((await Expect(adapter.Load())).Count(), Is.EqualTo(3));

    // A second Save with fewer rows must replace, not merge — no stale C/B.
    await Expect(adapter.Save(new[] { SamplePerson("Z") }));

    var rows = (await Expect(adapter.Load())).ToList();
    Assert.That(rows, Has.Count.EqualTo(1));
    Assert.That(rows[0].Name, Is.EqualTo("Z"));
  }

  [Test]
  public async Task Save_DateTime_RoundTrips_Through_Serial_WithinOneMillisecond()
  {
    var gateway = new InMemorySheetsGateway();
    gateway.Seed(SpreadsheetId, TableName, PersonSchema());
    var adapter = new GoogleSheetsStorageAdapter<Person>(SpreadsheetId, TableName, gateway);

    var joined = new DateTime(2021, 7, 4, 13, 14, 15);
    var person = SamplePerson();
    person.JoinedOn = joined;
    await Expect(adapter.Save(new[] { person }));

    var output = (await Expect(adapter.Load())).Single();
    // Serial-date encoding (days-as-double) loses sub-ms precision — the
    // documented, honest behaviour of the round-trip.
    Assert.That(output.JoinedOn, Is.EqualTo(joined).Within(TimeSpan.FromMilliseconds(1)));
  }

  [Test]
  public async Task Save_InvokesSaveFunc_InsteadOfDefault()
  {
    var gateway = new InMemorySheetsGateway();
    gateway.Seed(SpreadsheetId, TableName, PersonSchema());

    var invoked = false;
    var adapter = new GoogleSheetsStorageAdapter<Person>(
      SpreadsheetId, TableName, gateway,
      saveFunc: (_, _, _, _, _) =>
      {
        invoked = true;
        // Deliberately write nothing — proves the default replace did not run.
        return Task.CompletedTask;
      });

    await Expect(adapter.Save(new[] { SamplePerson() }));

    Assert.That(invoked, Is.True);
    Assert.That((await Expect(adapter.Load())).Count(), Is.EqualTo(0),
      "saveFunc replaced the default, which would otherwise have written a row");
  }

  [Test]
  public async Task SaveFunc_CanComposeWith_DefaultSave()
  {
    var gateway = new InMemorySheetsGateway();
    gateway.Seed(SpreadsheetId, TableName, PersonSchema());

    var preStepRan = false;
    var adapter = new GoogleSheetsStorageAdapter<Person>(
      SpreadsheetId, TableName, gateway,
      saveFunc: async (gw, ssId, tbl, rows, ct) =>
      {
        preStepRan = true;
        // A custom pre-step then the public default replace.
        await GoogleSheetsStorageAdapter<Person>.DefaultSave(gw, ssId, tbl, rows, ct);
      });

    await Expect(adapter.Save(new[] { SamplePerson("Composed") }));

    Assert.That(preStepRan, Is.True);
    var rows = (await Expect(adapter.Load())).ToList();
    Assert.That(rows, Has.Count.EqualTo(1));
    Assert.That(rows[0].Name, Is.EqualTo("Composed"));
  }

  [Test]
  public async Task Save_FailsClearly_WhenConstrainedReadOnly()
  {
    var gateway = new InMemorySheetsGateway();
    gateway.Seed(SpreadsheetId, TableName, PersonSchema());
    var adapter = new GoogleSheetsStorageAdapter<Person>(SpreadsheetId, TableName, gateway);

    // The user-facing read-only path: constrain CanWrite to false. The
    // constrained wrapper rejects Save before it reaches the gateway.
    var readOnly = new Item<IEnumerable<Person>>("People", adapter)
      .Constrain(t => t with { CanWrite = false });

    var result = await readOnly.Save(new[] { SamplePerson() }).Run();

    Assert.That(result, Is.InstanceOf<EffResult<FlowUnit>.Failure>());
    var failure = (EffResult<FlowUnit>.Failure)result;
    Assert.That(failure.Error, Is.InstanceOf<RuntimeError.ConstraintViolated>());

    // And the store is untouched.
    var loaded = await Expect(adapter.Load());
    Assert.That(loaded.Count(), Is.EqualTo(0));
  }
}
