using Flowthru.Data.Storage.Sheets.Local;
using Flowthru.Extensions.Google.Sheets.Tests.Support;

namespace Flowthru.Extensions.Google.Sheets.Tests.Backends;

/// <summary>
/// Offline backend for <see cref="Contract.SheetsGatewayLaws{TBackend}"/>:
/// each <see cref="CreateResource"/> yields a <see cref="JsonFileSheetsGateway"/>
/// over a fresh temp JSON file with its own unique spreadsheet id and
/// table-name prefix. No external dependency, no credentials, no network — runs
/// on every PR via the default test flow.
/// </summary>
/// <remarks>
/// Disjoint state is structural: a GUID-keyed temp path per call means no two
/// resources ever share a store, and the per-call spreadsheet id / table-name
/// prefix keep names distinct even within the (separate) files. The created
/// temp files are tracked and deleted in <see cref="Cleanup"/>; failed deletes
/// are ignored (best effort).
/// </remarks>
public sealed class OfflineSheetsBackend : ISheetsGatewayBackend
{
  private readonly List<string> _paths = new();
  private readonly object _gate = new();
  private int _counter;

  public SheetsGatewayContext CreateResource()
  {
    var n = Interlocked.Increment(ref _counter);
    var path = Path.Combine(
      Path.GetTempPath(), $"flowthru-sheets-laws-{Guid.NewGuid():N}.json");
    lock (_gate)
    {
      _paths.Add(path);
    }

    var gateway = new JsonFileSheetsGateway(path);
    var spreadsheetId = $"laws-spreadsheet-{n}-{Guid.NewGuid():N}";
    // A registered (reachable) spreadsheet is the offline analogue of a
    // pre-existing Drive sheet — the laws never create a spreadsheet, only
    // tables on it, matching the live contract.
    gateway.RegisterSpreadsheet(spreadsheetId);

    return new SheetsGatewayContext(
      Gateway: gateway,
      SpreadsheetId: spreadsheetId,
      TableNamePrefix: $"T{n}_{Guid.NewGuid():N}_");
  }

  public Task Cleanup()
  {
    lock (_gate)
    {
      foreach (var path in _paths)
      {
        if (File.Exists(path))
        {
          try { File.Delete(path); }
          catch { /* best effort */ }
        }
      }
      _paths.Clear();
    }
    return Task.CompletedTask;
  }
}
