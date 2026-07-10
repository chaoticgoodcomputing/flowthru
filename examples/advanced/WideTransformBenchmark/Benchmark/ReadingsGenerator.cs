using WideTransformBenchmark.Data._01_Raw.Schemas;

namespace WideTransformBenchmark.Benchmark;

/// <summary>
/// Seeded, deterministic fabricator for the benchmark's input datasets.
/// Same size in, byte-for-byte same rows out — re-running the example never
/// silently changes the workload under the measurements.
/// </summary>
/// <remarks>
/// The composite keyspace (device × channel × second-of-window) is sized at
/// roughly twice the row count, so drawing N keys uniformly leaves ~20% of
/// rows as duplicates for the dedup to remove — enough to make the
/// wide transform real, not so many that the output size stops resembling the
/// input size. The lineage columns (payload, checksum, source file, ...)
/// carry ~100 bytes per row of data the optimize pass exists to prune.
/// </remarks>
public static class ReadingsGenerator
{
  private const int DeviceCount = 50;

  private static readonly string[] Channels = ["temp", "humidity", "pressure", "vibration"];
  private static readonly string[] Units = ["C", "pct", "hPa", "mm_s"];

  public static IEnumerable<RawReadingRow> Generate(int rowCount)
  {
    // Seed folds in the row count so each size is its own deterministic
    // dataset rather than a prefix of the largest one.
    var random = new Random(20260709 ^ rowCount);
    var baseTime = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    // Keyspace ≈ 2 × rowCount → expected distinct keys ≈ 79% of rows drawn.
    var secondsWindow = Math.Max(1, (2 * rowCount) / (DeviceCount * Channels.Length));

    for (var i = 0; i < rowCount; i++)
    {
      var device = random.Next(DeviceCount);
      var channel = random.Next(Channels.Length);
      var observedAt = baseTime.AddSeconds(random.Next(secondsWindow));

      yield return new RawReadingRow
      {
        RowId = i,
        DeviceId = $"dev-{device:D4}",
        Channel = Channels[channel],
        ObservedAt = observedAt,
        Reading = Math.Round(random.NextDouble() * 100.0, 4),
        Unit = Units[channel],
        SourceFile = $"ingest/batch_{i / 10_000:D5}.jsonl",
        IngestedBy = $"collector-{i % 7:D2}",
        RawPayload = BuildPayload(random),
        BatchId = i / 10_000,
        Checksum = $"{random.Next():x8}",
      };
    }
  }

  /// <summary>A plausible ~90-character wire payload — the fat prunable column.</summary>
  private static string BuildPayload(Random random) =>
    $"{{\"seq\":{random.Next(1_000_000)},\"raw\":\"{random.Next():x8}{random.Next():x8}"
    + $"{random.Next():x8}{random.Next():x8}\",\"flags\":{random.Next(16)},"
    + $"\"rssi\":-{random.Next(30, 90)}}}";
}
