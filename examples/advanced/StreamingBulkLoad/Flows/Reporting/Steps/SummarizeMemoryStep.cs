using Flowthru.Step;
using StreamingBulkLoad.Data._01_Raw.Schemas;
using StreamingBulkLoad.Data._03_Primary.Schemas;

namespace StreamingBulkLoad.Flows.Reporting.Steps;

/// <summary>
/// Reduce the two raw <see cref="MemorySample"/> rows to a single
/// <see cref="MemoryComparison"/> verdict: peaks in MB, streaming/eager ratios,
/// and durations. Pure — the arithmetic the report needs, computed once.
/// </summary>
[FlowthruStep]
public static class SummarizeMemoryStep
{
  private const double BytesPerMb = 1024.0 * 1024.0;

  public static Func<IEnumerable<MemorySample>, IEnumerable<MemoryComparison>> Create()
  {
    return samples =>
    {
      var byVariant = samples.ToDictionary(s => s.Variant, StringComparer.OrdinalIgnoreCase);
      var eager = byVariant["Eager"];
      var streaming = byVariant["Streaming"];

      var eagerManagedMb = eager.PeakManagedBytes / BytesPerMb;
      var streamingManagedMb = streaming.PeakManagedBytes / BytesPerMb;
      var eagerWorkingSetMb = eager.PeakWorkingSetBytes / BytesPerMb;
      var streamingWorkingSetMb = streaming.PeakWorkingSetBytes / BytesPerMb;

      return new[]
      {
        new MemoryComparison
        {
          RowCount = eager.RowCount,
          EagerPeakManagedMb = Round(eagerManagedMb),
          StreamingPeakManagedMb = Round(streamingManagedMb),
          EagerPeakWorkingSetMb = Round(eagerWorkingSetMb),
          StreamingPeakWorkingSetMb = Round(streamingWorkingSetMb),
          ManagedRatioPct = eagerManagedMb > 0 ? Round(100.0 * streamingManagedMb / eagerManagedMb) : 0,
          WorkingSetRatioPct = eagerWorkingSetMb > 0 ? Round(100.0 * streamingWorkingSetMb / eagerWorkingSetMb) : 0,
          EagerDurationMs = eager.DurationMs,
          StreamingDurationMs = streaming.DurationMs,
        },
      };
    };
  }

  private static double Round(double value) => Math.Round(value, 1);
}
