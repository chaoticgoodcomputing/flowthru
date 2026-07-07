using System.Globalization;
using System.Text;
using Flowthru.Step;
using StreamingBulkLoad.Data._03_Primary.Schemas;

namespace StreamingBulkLoad.Flows.Reporting.Steps;

/// <summary>
/// Fill the Markdown template with the computed verdict. Structure and prose
/// live in the template (a Raw Catalog Item); this step only substitutes
/// <c>{{token}}</c> placeholders, so re-shaping the report is a template edit,
/// not a code change.
/// </summary>
[FlowthruStep]
public static class RenderMemoryReportStep
{
  public static Func<(IEnumerable<MemoryComparison>, string), byte[]> Create()
  {
    return inputs =>
    {
      var (comparisons, template) = inputs;
      var c = comparisons.First();
      var ci = CultureInfo.InvariantCulture;

      string Mb(double v) => v.ToString("0.0", ci);
      string Pct(double v) => v.ToString("0.0", ci);

      var verdict =
        $"Streaming held peak managed memory to **{Pct(c.ManagedRatioPct)}%** of eager "
        + $"({Mb(c.StreamingPeakManagedMb)} MB vs {Mb(c.EagerPeakManagedMb)} MB) while loading the same "
        + $"{c.RowCount.ToString("N0", ci)} rows into SQLite.";

      var subs = new Dictionary<string, string>(StringComparer.Ordinal)
      {
        ["row_count"] = c.RowCount.ToString("N0", ci),
        ["eager_peak_managed_mb"] = Mb(c.EagerPeakManagedMb),
        ["streaming_peak_managed_mb"] = Mb(c.StreamingPeakManagedMb),
        ["managed_ratio_pct"] = Pct(c.ManagedRatioPct),
        ["eager_peak_ws_mb"] = Mb(c.EagerPeakWorkingSetMb),
        ["streaming_peak_ws_mb"] = Mb(c.StreamingPeakWorkingSetMb),
        ["ws_ratio_pct"] = Pct(c.WorkingSetRatioPct),
        ["eager_ms"] = c.EagerDurationMs.ToString("N0", ci),
        ["streaming_ms"] = c.StreamingDurationMs.ToString("N0", ci),
        ["verdict"] = verdict,
        ["generated_utc"] = DateTime.UtcNow.ToString("u", ci),
      };

      var rendered = template;
      foreach (var (token, value) in subs)
      {
        rendered = rendered.Replace("{{" + token + "}}", value);
      }

      return Encoding.UTF8.GetBytes(rendered);
    };
  }
}
