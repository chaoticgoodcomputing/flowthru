using Flowthru.Data.Storage.Sheets;
using Flowthru.Data.Storage.Sheets.InMemory;
using Microsoft.Extensions.Time.Testing;

namespace Flowthru.Extensions.Google.Sheets.Tests;

/// <summary>
/// Backoff behavior of <see cref="RetryingSheetsGateway"/> driven against the
/// offline <see cref="InMemorySheetsGateway"/> with its quota fault injection ON.
/// Timing is exercised through a <see cref="FakeTimeProvider"/> shared by both the
/// gateway's quota window and the decorator's backoff delay, so a transient
/// <c>429</c> is reproduced and ridden out without a single real sleep.
/// </summary>
[TestFixture]
public sealed class RetryingSheetsGatewayTests
{
  private const string SpreadsheetId = "sheet-1";
  private const string TableName = "RawData";

  private static TableSchema Schema() =>
    new(new[] { new TableColumn("Name", ColumnType.Text) });

  private static TableData OneRow(TableSchema schema) =>
    new(schema, new[] { new[] { FieldValue.Text("x") } });

  // Drive a clock-blocked operation to completion: advance the fake clock in
  // small steps until the task settles, so Task.Delay(.., TimeProvider, ..) fires
  // without a real wait. Returns the completed task (faulted or successful).
  private static async Task<Task> PumpToCompletion(
    Task operation, FakeTimeProvider clock, TimeSpan step)
  {
    var guard = 0;
    while (!operation.IsCompleted)
    {
      clock.Advance(step);
      // Yield so the delay continuation runs before the next advance.
      await Task.Yield();
      if (++guard > 10_000)
      {
        throw new InvalidOperationException("Operation did not complete; backoff may be stuck.");
      }
    }

    return operation;
  }

  [Test]
  public async Task TransientThenSuccess_RetriesAfterBackoff()
  {
    var clock = new FakeTimeProvider(DateTimeOffset.UnixEpoch);
    // Quota of 1/min: AddTable (the fixture write) saturates the window, so the
    // first ReplaceRows hits a 429; once the window rolls past 60s the retry
    // succeeds.
    var inner = new InMemorySheetsGateway(
      new InMemorySheetsOptions { WritesPerMinute = 1, Clock = clock });
    inner.RegisterSpreadsheet(SpreadsheetId);
    var created = await inner.AddTable(SpreadsheetId, TableName, Schema(), default);

    var gateway = new RetryingSheetsGateway(
      inner,
      new SheetsRetryOptions
      {
        MaxAttempts = 5,
        BaseDelay = TimeSpan.FromSeconds(30),
        MaxDelay = TimeSpan.FromSeconds(120),
      },
      clock);

    var write = gateway.ReplaceRows(SpreadsheetId, created, OneRow(created.Schema), default);
    var settled = await PumpToCompletion(write, clock, TimeSpan.FromSeconds(5));

    Assert.That(settled.IsCompletedSuccessfully, Is.True,
      "the retried write should succeed once the quota window rolls past");
    var read = await inner.ReadRows(SpreadsheetId, created, default);
    Assert.That(read.RowCount, Is.EqualTo(1));
  }

  [Test]
  public async Task PersistentTransient_ExhaustsRetries_SurfacesClearFailure()
  {
    var clock = new FakeTimeProvider(DateTimeOffset.UnixEpoch);
    // A quota that never recovers within the backoff horizon: 1 write/min, but the
    // backoff caps well under 60s so the window never rolls — every attempt 429s.
    var inner = new InMemorySheetsGateway(
      new InMemorySheetsOptions { WritesPerMinute = 1, Clock = clock });
    inner.RegisterSpreadsheet(SpreadsheetId);
    var created = await inner.AddTable(SpreadsheetId, TableName, Schema(), default);

    const int maxAttempts = 3;
    var gateway = new RetryingSheetsGateway(
      inner,
      new SheetsRetryOptions
      {
        MaxAttempts = maxAttempts,
        BaseDelay = TimeSpan.FromSeconds(1),
        MaxDelay = TimeSpan.FromSeconds(2),
      },
      clock);

    var write = gateway.ReplaceRows(SpreadsheetId, created, OneRow(created.Schema), default);
    var settled = await PumpToCompletion(write, clock, TimeSpan.FromMilliseconds(500));

    Assert.That(settled.IsFaulted, Is.True);
    var ex = settled.Exception!.InnerException;
    Assert.That(ex, Is.InstanceOf<SheetsRetryExhaustedException>(),
      "exhausted retries surface a clear, non-transient failure");
    var exhausted = (SheetsRetryExhaustedException)ex!;
    Assert.That(exhausted.Attempts, Is.EqualTo(maxAttempts));
    Assert.That(exhausted.Message, Does.Contain("FTGS1607"));
    Assert.That(exhausted.InnerException, Is.InstanceOf<SheetsRateLimitException>(),
      "the last transient cause is chained");
  }

  [Test]
  public void PermanentFailure_IsNotRetried_PropagatesImmediately()
  {
    var clock = new FakeTimeProvider(DateTimeOffset.UnixEpoch);
    // No spreadsheet registered → ResolveTable throws a permanent access failure.
    var inner = new InMemorySheetsGateway(
      new InMemorySheetsOptions { WritesPerMinute = 1, Clock = clock });

    var gateway = new RetryingSheetsGateway(inner, timeProvider: clock);

    // Synchronous-by-construction: a permanent failure must not enter the backoff
    // loop, so the task faults without any clock advance.
    var resolve = gateway.ResolveTable(SpreadsheetId, TableName, default);

    Assert.That(resolve.IsFaulted, Is.True,
      "a permanent failure faults immediately, never awaiting a backoff delay");
    Assert.That(
      resolve.Exception!.InnerException,
      Is.InstanceOf<SheetsSpreadsheetAccessException>(),
      "the permanent exception propagates unchanged, not wrapped as exhausted");
  }

  [Test]
  public async Task RetryAfterHint_IsHonored_OverExponentialBackoff()
  {
    var clock = new FakeTimeProvider(DateTimeOffset.UnixEpoch);
    // The in-memory gateway's 429 carries a RetryAfter hint computed from its
    // quota window. The decorator should wait on that hint rather than its own
    // exponential base, so a backoff base larger than the window still resolves
    // once the hint's window passes.
    var inner = new InMemorySheetsGateway(
      new InMemorySheetsOptions { WritesPerMinute = 1, Clock = clock });
    inner.RegisterSpreadsheet(SpreadsheetId);
    var created = await inner.AddTable(SpreadsheetId, TableName, Schema(), default);

    // Confirm the hint is actually present on the raw transient failure.
    var raw = Assert.ThrowsAsync<SheetsRateLimitException>(
      async () => await inner.ReplaceRows(SpreadsheetId, created, OneRow(created.Schema), default));
    Assert.That(raw!.RetryAfter, Is.Not.Null.And.GreaterThan(TimeSpan.Zero));

    var gateway = new RetryingSheetsGateway(
      inner,
      new SheetsRetryOptions
      {
        MaxAttempts = 5,
        // Base far larger than the ~60s window; if the hint were ignored the
        // clamp would still let it through, so cap the MaxDelay high to ensure
        // the honored hint (not the cap) is what advances us past the window.
        BaseDelay = TimeSpan.FromSeconds(1),
        MaxDelay = TimeSpan.FromMinutes(10),
      },
      clock);

    var write = gateway.ReplaceRows(SpreadsheetId, created, OneRow(created.Schema), default);
    var settled = await PumpToCompletion(write, clock, TimeSpan.FromSeconds(5));

    Assert.That(settled.IsCompletedSuccessfully, Is.True,
      "honoring the RetryAfter hint lets the retry land just past the quota window");
  }

  [Test]
  public async Task ExponentialBackoff_WithoutHint_RidesOutThePerMinuteWindow()
  {
    var clock = new FakeTimeProvider(DateTimeOffset.UnixEpoch);
    // A quota of 1/min where the transient carries NO RetryAfter hint exercises
    // the decorator's own exponential branch (the DelayFor exponential path,
    // distinct from the honored-hint path). A 100/min quota strips the hint?
    // No — we suppress it by re-wrapping. Instead drive the no-hint path by
    // having the inner throw a hint-less rate-limit via a stub.
    var inner = new HintlessTransientGateway(failuresBeforeSuccess: 3);

    var gateway = new RetryingSheetsGateway(
      inner,
      new SheetsRetryOptions
      {
        MaxAttempts = 5,
        // Exponential: 1s, 2s, 4s before the 4th attempt succeeds. The clock must
        // advance through each doubled delay for the write to land.
        BaseDelay = TimeSpan.FromSeconds(1),
        MaxDelay = TimeSpan.FromSeconds(60),
      },
      clock);

    var write = gateway.ReplaceRows(SpreadsheetId, Resolved(), OneRow(Schema()), default);
    var settled = await PumpToCompletion(write, clock, TimeSpan.FromMilliseconds(250));

    Assert.That(settled.IsCompletedSuccessfully, Is.True,
      "the hint-less transient is ridden out by the decorator's own exponential backoff");
    Assert.That(inner.Attempts, Is.EqualTo(4),
      "three transient failures, then success on the fourth attempt");
  }

  [Test]
  public async Task PerMinuteWindow_NotJustFirstWrite_GovernsRetryEligibility()
  {
    var clock = new FakeTimeProvider(DateTimeOffset.UnixEpoch);
    // 2 writes/min. AddTable (#1) + one ReplaceRows (#2) saturate the window; the
    // next write 429s, and the retry can only land once a slot ages out >60s
    // later — proving the rolling per-minute window, not a one-shot, governs the
    // backoff's eventual success.
    var inner = new InMemorySheetsGateway(
      new InMemorySheetsOptions { WritesPerMinute = 2, Clock = clock });
    inner.RegisterSpreadsheet(SpreadsheetId);
    var created = await inner.AddTable(SpreadsheetId, TableName, Schema(), default); // #1 @ t=0
    await inner.ReplaceRows(SpreadsheetId, created, OneRow(created.Schema), default); // #2 @ t=0

    var gateway = new RetryingSheetsGateway(
      inner,
      new SheetsRetryOptions
      {
        MaxAttempts = 10,
        BaseDelay = TimeSpan.FromSeconds(20),
        MaxDelay = TimeSpan.FromSeconds(120),
      },
      clock);

    var write = gateway.ReplaceRows(SpreadsheetId, created, OneRow(created.Schema), default);
    var settled = await PumpToCompletion(write, clock, TimeSpan.FromSeconds(5));

    Assert.That(settled.IsCompletedSuccessfully, Is.True,
      "the retry lands only once the rolling window frees a write slot");
  }

  [Test]
  public void Constructor_RejectsZeroMaxAttempts()
  {
    var inner = new InMemorySheetsGateway();
    Assert.Throws<ArgumentOutOfRangeException>(
      () => new RetryingSheetsGateway(inner, new SheetsRetryOptions { MaxAttempts = 0 }));
  }

  [Test]
  public void RetryExhausted_CarriesAttemptCountAndCode()
  {
    var ex = new SheetsRetryExhaustedException(4, new SheetsRateLimitException());
    Assert.Multiple(() =>
    {
      Assert.That(ex.Attempts, Is.EqualTo(4));
      Assert.That(ex.Message, Does.Contain("FTGS1607"));
      Assert.That(ex.InnerException, Is.InstanceOf<SheetsRateLimitException>());
    });
  }

  [Test]
  public void RetryOptions_Defaults_AreQuotaTuned()
  {
    var options = new SheetsRetryOptions();
    Assert.Multiple(() =>
    {
      Assert.That(options.MaxAttempts, Is.EqualTo(5));
      Assert.That(options.BaseDelay, Is.EqualTo(TimeSpan.FromSeconds(1)));
      Assert.That(options.MaxDelay, Is.EqualTo(TimeSpan.FromSeconds(60)));
    });
  }

  private static ResolvedTable Resolved() =>
    new(TableName, Schema(), new TableRange(0, 0, 1, 0, 1));

  // A gateway whose ReplaceRows throws a hint-LESS transient a fixed number of
  // times, then succeeds — isolates the decorator's own exponential-backoff
  // branch (no RetryAfter to honor), which the in-memory gateway's hinted 429
  // does not reach.
  private sealed class HintlessTransientGateway : ISheetsGateway
  {
    private readonly int _failuresBeforeSuccess;

    public HintlessTransientGateway(int failuresBeforeSuccess)
    {
      _failuresBeforeSuccess = failuresBeforeSuccess;
    }

    public int Attempts { get; private set; }

    public Task ReplaceRows(string spreadsheetId, ResolvedTable table, TableData rows, CancellationToken ct)
    {
      Attempts++;
      if (Attempts <= _failuresBeforeSuccess)
      {
        // No RetryAfter hint → the decorator falls back to exponential backoff.
        throw new SheetsRateLimitException("transient, no hint");
      }
      return Task.CompletedTask;
    }

    public Task<ResolvedTable?> ResolveTable(string spreadsheetId, string tableName, CancellationToken ct) =>
      Task.FromResult<ResolvedTable?>(Resolved());

    public Task<TableData> ReadRows(string spreadsheetId, ResolvedTable table, CancellationToken ct) =>
      Task.FromResult(TableData.Empty(table.Schema));

    public Task<ResolvedTable> AddTable(
      string spreadsheetId, string tableName, TableSchema schema, CancellationToken ct) =>
      Task.FromResult(Resolved());
  }
}
