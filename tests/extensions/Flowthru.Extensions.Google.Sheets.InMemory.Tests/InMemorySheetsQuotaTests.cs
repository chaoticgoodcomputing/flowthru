using Flowthru.Data.Storage.Sheets;
using Flowthru.Data.Storage.Sheets.InMemory;
using Microsoft.Extensions.Time.Testing;

namespace Flowthru.Extensions.Google.Sheets.InMemory.Tests;

/// <summary>
/// Quota-ON / fault-injection behavior of <see cref="InMemorySheetsGateway"/>.
/// The write quota is opt-in (via <see cref="InMemorySheetsOptions.WritesPerMinute"/>)
/// and driven by an injected <see cref="TimeProvider"/>, so a breach is
/// reproducible without real sleeps. A breach throws the transient
/// <see cref="SheetsRateLimitException"/> the production gateway also maps Google's
/// 429 onto.
/// </summary>
[TestFixture]
public sealed class InMemorySheetsQuotaTests
{
  private const string SpreadsheetId = "sheet-1";
  private const string TableName = "RawData";

  private static TableSchema Schema() =>
    new(new[] { new TableColumn("Name", ColumnType.Text) });

  private static TableData OneRow(TableSchema schema) =>
    new(schema, new[] { new[] { FieldValue.Text("x") } });

  [Test]
  public void QuotaOff_ByDefault_ManyWritesNeverThrow()
  {
    var gateway = new InMemorySheetsGateway();
    gateway.RegisterSpreadsheet(SpreadsheetId);
    var created = gateway.AddTable(SpreadsheetId, TableName, Schema(), default).Result;

    Assert.DoesNotThrowAsync(async () =>
    {
      for (var i = 0; i < 1000; i++)
      {
        await gateway.ReplaceRows(SpreadsheetId, created, OneRow(created.Schema), default);
      }
    });
  }

  [Test]
  public async Task WritesBeyondQuota_ThrowTransientException()
  {
    var clock = new FakeTimeProvider(DateTimeOffset.UnixEpoch);
    var gateway = new InMemorySheetsGateway(
      new InMemorySheetsOptions { WritesPerMinute = 3, Clock = clock });
    gateway.RegisterSpreadsheet(SpreadsheetId);

    // AddTable counts as a write (#1).
    var created = await gateway.AddTable(SpreadsheetId, TableName, Schema(), default);
    // Two more writes within the window (#2, #3) succeed.
    await gateway.ReplaceRows(SpreadsheetId, created, OneRow(created.Schema), default);
    await gateway.ReplaceRows(SpreadsheetId, created, OneRow(created.Schema), default);

    // The 4th write within the same minute breaches the quota.
    var ex = Assert.ThrowsAsync<SheetsRateLimitException>(
      async () => await gateway.ReplaceRows(SpreadsheetId, created, OneRow(created.Schema), default));
    Assert.That(ex!.RetryAfter, Is.Not.Null);
    Assert.That(ex.RetryAfter, Is.GreaterThan(TimeSpan.Zero));
  }

  [Test]
  public async Task RejectedWrite_LeavesTableUntouched()
  {
    var clock = new FakeTimeProvider(DateTimeOffset.UnixEpoch);
    var gateway = new InMemorySheetsGateway(
      new InMemorySheetsOptions { WritesPerMinute = 1, Clock = clock });
    gateway.RegisterSpreadsheet(SpreadsheetId);

    // AddTable is write #1; it exhausts the quota for the window.
    var created = await gateway.AddTable(SpreadsheetId, TableName, Schema(), default);

    Assert.ThrowsAsync<SheetsRateLimitException>(
      async () => await gateway.ReplaceRows(SpreadsheetId, created, OneRow(created.Schema), default));

    // The rejected write must not have torn the table's data.
    var read = await gateway.ReadRows(SpreadsheetId, created, default);
    Assert.That(read.RowCount, Is.EqualTo(0));
  }

  [Test]
  public async Task AdvancingClockPastWindow_LetsWritesResume()
  {
    var clock = new FakeTimeProvider(DateTimeOffset.UnixEpoch);
    var gateway = new InMemorySheetsGateway(
      new InMemorySheetsOptions { WritesPerMinute = 2, Clock = clock });
    gateway.RegisterSpreadsheet(SpreadsheetId);

    var created = await gateway.AddTable(SpreadsheetId, TableName, Schema(), default); // #1
    await gateway.ReplaceRows(SpreadsheetId, created, OneRow(created.Schema), default); // #2

    Assert.ThrowsAsync<SheetsRateLimitException>(
      async () => await gateway.ReplaceRows(SpreadsheetId, created, OneRow(created.Schema), default));

    // Advance past the rolling 60s window so the earlier writes age out.
    clock.Advance(TimeSpan.FromSeconds(61));

    Assert.DoesNotThrowAsync(
      async () => await gateway.ReplaceRows(SpreadsheetId, created, OneRow(created.Schema), default));
  }

  [Test]
  public async Task QuotaWindow_IsRolling_NotFixedBuckets()
  {
    var clock = new FakeTimeProvider(DateTimeOffset.UnixEpoch);
    var gateway = new InMemorySheetsGateway(
      new InMemorySheetsOptions { WritesPerMinute = 2, Clock = clock });
    gateway.RegisterSpreadsheet(SpreadsheetId);

    var created = await gateway.AddTable(SpreadsheetId, TableName, Schema(), default); // #1 @ t=0
    clock.Advance(TimeSpan.FromSeconds(40));
    await gateway.ReplaceRows(SpreadsheetId, created, OneRow(created.Schema), default); // #2 @ t=40

    // At t=70 the t=0 write has aged out (>60s) but t=40 is still live, so one
    // write is free; the one after that breaches again.
    clock.Advance(TimeSpan.FromSeconds(30)); // t=70
    Assert.DoesNotThrowAsync(
      async () => await gateway.ReplaceRows(SpreadsheetId, created, OneRow(created.Schema), default)); // #3 @ t=70
    Assert.ThrowsAsync<SheetsRateLimitException>(
      async () => await gateway.ReplaceRows(SpreadsheetId, created, OneRow(created.Schema), default));
  }

  [Test]
  public void SheetsRateLimitException_IsTransientShape()
  {
    // The retry layer (#83) branches on this type; keep it a plain Exception
    // subtype carrying an optional retry-after hint.
    var ex = new SheetsRateLimitException("quota", TimeSpan.FromSeconds(5));
    Assert.That(ex, Is.InstanceOf<Exception>());
    Assert.That(ex.RetryAfter, Is.EqualTo(TimeSpan.FromSeconds(5)));
  }
}
