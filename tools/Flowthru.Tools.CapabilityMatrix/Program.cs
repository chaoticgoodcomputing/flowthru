using System.Reflection;
using System.Text;
using Flowthru.Core.Abstractions;
using Flowthru.Core.Data.Capabilities;
using Flowthru.Core.Data.Serialization;
using Flowthru.Core.Data.Storage;
using Flowthru.Core.Data.Storage.Format;
using Flowthru.Tests.Kits.Schemas;

namespace Flowthru.Tools.CapabilityMatrix;

// ─────────────────────────────────────────────────────────────────────────────
// Capability matrix generator
//
// Constructs each first-party IFormatSerializer<TRow> with a representative
// schema, reads its RowFeatures + planner-opt-out status, and emits a Markdown
// matrix at docs/reference/extensions/capability-matrix.md.
//
// The generator is run by the `_test:capability-matrix-freshness` meta-test:
// generate fresh output, `git diff --quiet` against the committed file, fail
// if drift detected.
// ─────────────────────────────────────────────────────────────────────────────

internal static class Program
{
  private record FormatEntry(
    string Name,
    string AssemblyShortName,
    bool ConsumesPlanner,
    string? OptOutReason,
    FormatRowFeatures Features,
    StorageTraits Traits,
    bool ConstrainedToFlat,
    bool ImplementsReader,
    bool ImplementsWriter
  );

  private static int Main(string[] args)
  {
    var outputPath = args.Length > 0
      ? args[0]
      : ResolveDefaultOutputPath();

    var entries = new[]
    {
      InspectFormat<CsvFormatSerializer<TraditionalSchema>>(
        "CSV",
        "Flowthru.Extensions.Csv",
        () => new CsvFormatSerializer<TraditionalSchema>()
      ),
      InspectFormat<ExcelFormatSerializer<TraditionalSchema>>(
        "Excel",
        "Flowthru.Extensions.Excel",
        () => new ExcelFormatSerializer<TraditionalSchema>(sheetName: "Sheet1")
      ),
      InspectFormat<ParquetFormatSerializer<TraditionalSchema>>(
        "Parquet",
        "Flowthru.Extensions.Parquet",
        () => new ParquetFormatSerializer<TraditionalSchema>()
      ),
      InspectFormat<JsonFormatSerializer<TraditionalSchema>>(
        "JSON",
        "Flowthru.Core (built-in)",
        () => new JsonFormatSerializer<TraditionalSchema>()
      ),
    };

    var markdown = RenderMarkdown(entries);

    Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
    File.WriteAllText(outputPath, markdown);

    Console.WriteLine($"Capability matrix written to {outputPath}");
    return 0;
  }

  private static FormatEntry InspectFormat<TSerializer>(
    string displayName,
    string assemblyShortName,
    Func<IFormatRowReader<TraditionalSchema>> factory
  )
  {
    var serializerType = typeof(TSerializer).IsGenericType
      ? typeof(TSerializer).GetGenericTypeDefinition()
      : typeof(TSerializer);

    var optOutAttr = serializerType.GetCustomAttribute<OptOutOfPropertyPlannerAttribute>();
    var consumesPlanner = optOutAttr is null;

    // Detect structural flat-only constraint via the open generic's parameter constraints.
    // A format declared `where TRow : ..., IFlatSchema, ...` is structurally incapable of
    // accepting nested schemas — its Nested cell in the matrix is "not applicable" rather
    // than a tracked false claim.
    var constrainedToFlat = false;
    var typeParams = serializerType.GetGenericArguments();
    if (typeParams.Length > 0)
    {
      var constraints = typeParams[0].GetGenericParameterConstraints();
      constrainedToFlat = constraints.Any(c => c == typeof(IFlatSchema));
    }

    // Phase D capability segments: the format's structural read/write surface is encoded
    // by which interface segments it implements. Reader is the floor (every first-party
    // format reads); writer is optional (Excel via ExcelDataReader is reader-only).
    var implementsReader = typeof(IFormatRowReader<TraditionalSchema>).IsAssignableFrom(typeof(TSerializer));
    var implementsWriter = typeof(IFormatRowWriter<TraditionalSchema>).IsAssignableFrom(typeof(TSerializer));

    var instance = factory();
    return new FormatEntry(
      displayName,
      assemblyShortName,
      consumesPlanner,
      optOutAttr?.Reason,
      instance.RowFeatures,
      instance.Traits,
      constrainedToFlat,
      implementsReader,
      implementsWriter
    );
  }

  private static string RenderMarkdown(IReadOnlyList<FormatEntry> entries)
  {
    var sb = new StringBuilder();

    sb.AppendLine("# Flowthru Format Extension Capability Matrix");
    sb.AppendLine();
    sb.AppendLine(
      "This document is **auto-generated** from each format extension's"
        + " `IFormatBase<TRow>.RowFeatures` declaration and which capability"
        + " segments it implements (`IFormatRowReader<TRow>`,"
        + " `IFormatRowWriter<TRow>`)."
        + " Do not edit by hand —"
        + " the `_test:capability-matrix-freshness` meta-test fails on drift."
    );
    sb.AppendLine();
    sb.AppendLine(
      "Regenerate locally via `nx run tests:_test:capability-matrix-freshness` or"
        + " directly via the `Flowthru.Tools.CapabilityMatrix` tool."
    );
    sb.AppendLine();
    sb.AppendLine("## Universal baseline");
    sb.AppendLine();
    sb.AppendLine(
      "All four formats round-trip the universal row-shape baseline:"
    );
    sb.AppendLine();
    sb.AppendLine("- CLR primitives (`int`, `string`, `bool`, `double`, `decimal`, …)");
    sb.AppendLine("- BCL scalar structs (`Guid`, `DateTime`, `TimeSpan`, `DateTimeOffset`, …)");
    sb.AppendLine("- `Nullable<T>` value types and nullable reference types");
    sb.AppendLine("- `[SerializedLabel(\"…\")]` field-name mapping");
    sb.AppendLine("- `[SerializedEnum(\"…\")]` enum value mapping");
    sb.AppendLine("- `required` members and positional-record activation");
    sb.AppendLine();
    sb.AppendLine(
      "These features are intrinsic to the planner's classification cascade and don't"
        + " vary across formats. The matrix below tracks capabilities **on top of** that"
        + " baseline — features where format-by-format support genuinely differs."
    );
    sb.AppendLine();
    sb.AppendLine("## Row-shape Features");
    sb.AppendLine();
    sb.AppendLine(
      "Each format declares which row-shape features it round-trips on top of the"
        + " universal baseline. Cell semantics:"
    );
    sb.AppendLine();
    sb.AppendLine(
      "- **`✓`** — format claims support; the matching kit conformance fixture"
        + " round-trips successfully."
    );
    sb.AppendLine(
      "- **`✗`** — format claims false; could be implemented but isn't."
        + " Tracked as a follow-up; kit fixtures requiring the feature skip vacuously."
    );
    sb.AppendLine(
      "- **`—`** — structurally not applicable; the format's generic constraint"
        + " (`where TRow : IFlatSchema`) prevents the schema shape from compiling."
        + " The matching fixture cannot be wired against this format."
    );
    sb.AppendLine();
    sb.AppendLine("| Format | Schema shape | IScalar wrappers | Nested rows |");
    sb.AppendLine("|---|---|:---:|:---:|");
    foreach (var e in entries)
    {
      var shape = e.ConstrainedToFlat ? "Flat-only" : "Flat or nested";
      var nestedCell = e.ConstrainedToFlat ? "—" : Cell(e.Features.SupportsNested);
      sb.AppendLine(
        $"| **{e.Name}** ({e.AssemblyShortName}) | {shape} | {Cell(e.Features.SupportsIScalar)} | {nestedCell} |"
      );
    }
    sb.AppendLine();
    sb.AppendLine(
      "Primitive-level format mechanics (`byte[]` blobs handled as base64/binary,"
        + " timezone semantics on `DateTimeOffset`, etc.) are intrinsic to each format's"
        + " underlying serialization library and aren't tracked here."
    );
    sb.AppendLine();

    sb.AppendLine("## Property Mapping");
    sb.AppendLine();
    sb.AppendLine(
      "Format extensions are expected to consume Core's"
        + " `PropertyMappingPlanner` for per-property classification (see"
        + " `docs/scratch/data-extension-contract.md` Phase B)."
        + " Formats with a structural reason can opt out via"
        + " `[OptOutOfPropertyPlanner(...)]` — those formats handle row-shape"
        + " classification on their own and may diverge from the planner-driven"
        + " baseline."
    );
    sb.AppendLine();
    sb.AppendLine("| Format | Planner consumption | Opt-out reason |");
    sb.AppendLine("|---|---|---|");
    foreach (var e in entries)
    {
      var status = e.ConsumesPlanner ? "✓ consumes planner" : "✗ manual mapping";
      var reason = e.OptOutReason is null ? "—" : EscapeMarkdown(e.OptOutReason);
      sb.AppendLine($"| **{e.Name}** | {status} | {reason} |");
    }
    sb.AppendLine();

    sb.AppendLine("## Storage Traits");
    sb.AppendLine();
    sb.AppendLine(
      "Medium-level capabilities of each format. See"
        + " `Flowthru.Core.Data.Capabilities.StorageTraits` for the full surface."
    );
    sb.AppendLine();
    sb.AppendLine(
      "**Read / Write columns** carry two signals. Phase D"
        + " (capability-segmented interfaces) split the format surface into"
        + " `IFormatRowReader<TRow>` and `IFormatRowWriter<TRow>`. A format that"
        + " does not implement a segment is *structurally* incapable of that"
        + " operation — the absence is enforced by the type system, not a runtime"
        + " trait flag. A format that implements the segment but reports"
        + " `Traits.CanWrite = false` (etc.) is *runtime*-disabled."
    );
    sb.AppendLine();
    sb.AppendLine(
      "- **`✓`** — segment implemented and runtime trait permits."
    );
    sb.AppendLine(
      "- **`—`** — segment not implemented (structural / compile-time signal)."
        + " Calling code paths against the missing segment fail at compile time."
    );
    sb.AppendLine(
      "- **`✗`** — segment implemented but runtime trait reports unavailable"
        + " (e.g., medium pointed at a read-only file system)."
    );
    sb.AppendLine();
    sb.AppendLine("| Format | Read | Write | Stream | Append | Transactional |");
    sb.AppendLine("|---|:---:|:---:|:---:|:---:|:---:|");
    foreach (var e in entries)
    {
      sb.AppendLine(
        $"| **{e.Name}** | {SegmentCell(e.ImplementsReader, e.Traits.CanRead)} |"
          + $" {SegmentCell(e.ImplementsWriter, e.Traits.CanWrite)} |"
          + $" {Cell(e.Traits.CanStream)} | {Cell(e.Traits.CanAppend)} |"
          + $" {Cell(e.Traits.IsTransactional)} |"
      );
    }
    sb.AppendLine();

    return sb.ToString();
  }

  private static string Cell(bool flag) => flag ? "✓" : "✗";

  // Phase D segment cell: combines structural (interface implementation) with runtime
  // (Traits flag). Structural absence wins — a format that doesn't implement the
  // segment cannot have it enabled at runtime.
  private static string SegmentCell(bool implementsSegment, bool runtimePermits) =>
    !implementsSegment ? "—" : (runtimePermits ? "✓" : "✗");

  private static string EscapeMarkdown(string input) =>
    input.Replace("|", "\\|").Replace("\n", " ").Replace("\r", "");

  private static string ResolveDefaultOutputPath()
  {
    // Walk up from the executable to the repo root to find the docs/ directory.
    var dir = AppContext.BaseDirectory;
    while (!string.IsNullOrEmpty(dir) && !Directory.Exists(Path.Combine(dir, "docs")))
    {
      var parent = Directory.GetParent(dir)?.FullName;
      if (parent == null || parent == dir)
      {
        throw new InvalidOperationException(
          "Could not locate repository root from "
            + AppContext.BaseDirectory
            + ". Pass the output path as the first argument."
        );
      }
      dir = parent;
    }

    return Path.Combine(dir, "docs", "reference", "extensions", "capability-matrix.md");
  }
}
