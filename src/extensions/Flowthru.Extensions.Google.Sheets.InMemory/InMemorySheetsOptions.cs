namespace Flowthru.Data.Storage.Sheets.InMemory;

/// <summary>
/// Configuration for an <see cref="InMemorySheetsGateway"/> — chiefly the
/// optional write-quota fault injection used to exercise retry/backoff against
/// an offline gateway.
/// </summary>
/// <remarks>
/// <para>
/// <strong>OFF by default.</strong> A default-constructed
/// <see cref="InMemorySheetsOptions"/> imposes no quota and uses
/// <see cref="System.TimeProvider.System"/> as a clock, so the gateway is fully
/// deterministic and never throws on volume — this is what the starter example
/// uses. Quota enforcement is opt-in by setting <see cref="WritesPerMinute"/>.
/// </para>
/// <para>
/// Fault injection is deterministic and clock-driven: supply a controllable
/// <see cref="System.TimeProvider"/> (e.g. a fake) to advance time without real
/// sleeps and reproduce a quota breach exactly.
/// </para>
/// </remarks>
public sealed class InMemorySheetsOptions
{
  /// <summary>
  /// The maximum number of write operations permitted within any rolling
  /// 60-second window, measured against <see cref="Clock"/>. A write that would
  /// exceed this throws a transient <see cref="SheetsRateLimitException"/>,
  /// emulating Google's <c>429</c> quota response.
  /// <see langword="null"/> (the default) disables quota enforcement entirely —
  /// no timestamps are tracked and writes never throw on volume.
  /// </summary>
  /// <remarks>
  /// A "write" is one <see cref="ISheetsGateway.ReplaceRows"/> or
  /// <see cref="ISheetsGateway.AddTable"/> call. Reads and resolves are free.
  /// </remarks>
  public int? WritesPerMinute { get; init; }

  /// <summary>
  /// The clock the quota window is measured against. Defaults to
  /// <see cref="System.TimeProvider.System"/>. Inject a controllable provider
  /// to drive the quota window deterministically in tests.
  /// </summary>
  public TimeProvider Clock { get; init; } = TimeProvider.System;
}
