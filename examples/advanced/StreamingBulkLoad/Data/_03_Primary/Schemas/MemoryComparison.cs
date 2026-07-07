using Flowthru.Data.Schema;

namespace StreamingBulkLoad.Data._03_Primary.Schemas;

/// <summary>
/// The eager-vs-streaming verdict computed from the two <c>MemorySample</c>
/// rows — peaks in megabytes, the streaming/eager ratios, and durations.
/// Produced by <c>SummarizeMemoryStep</c> and rendered into
/// <c>memory_report.md</c> by <c>RenderMemoryReportStep</c>.
/// </summary>
[FlowthruSchema]
public partial record MemoryComparison
{
  /// <summary>Valid rows loaded by both variants (they must match).</summary>
  public required int RowCount { get; init; }

  /// <summary>Eager peak managed heap, MB.</summary>
  public required double EagerPeakManagedMb { get; init; }

  /// <summary>Streaming peak managed heap, MB.</summary>
  public required double StreamingPeakManagedMb { get; init; }

  /// <summary>Eager peak working set, MB.</summary>
  public required double EagerPeakWorkingSetMb { get; init; }

  /// <summary>Streaming peak working set, MB.</summary>
  public required double StreamingPeakWorkingSetMb { get; init; }

  /// <summary>Streaming managed peak as a percentage of eager (lower is better).</summary>
  public required double ManagedRatioPct { get; init; }

  /// <summary>Streaming working-set peak as a percentage of eager.</summary>
  public required double WorkingSetRatioPct { get; init; }

  /// <summary>Eager run duration, ms.</summary>
  public required long EagerDurationMs { get; init; }

  /// <summary>Streaming run duration, ms.</summary>
  public required long StreamingDurationMs { get; init; }
}
