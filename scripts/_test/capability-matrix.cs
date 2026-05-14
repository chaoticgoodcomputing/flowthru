#:project ../../src/core/Flowthru.Core/Flowthru.Core.csproj
#:project ../../src/extensions/Flowthru.Extensions.Csv/Flowthru.Extensions.Csv.csproj
#:project ../../src/extensions/Flowthru.Extensions.Excel/Flowthru.Extensions.Excel.csproj
#:project ../../src/extensions/Flowthru.Extensions.Parquet/Flowthru.Extensions.Parquet.csproj

// ─────────────────────────────────────────────────────────────────────────────
// Capability matrix generator (file-based C# program — .NET 10+).
//
// Constructs each first-party format with a representative schema, reads its
// runtime StorageTraits + planner-opt-out status + segment-implementation
// surface, and emits a Markdown matrix to the path passed as argv[0]
// (default: docs/reference/extensions/capability-matrix.md).
//
// Row-shape capability claims (IScalar wrappers, nested support) moved from
// a runtime FormatRowFeatures bag to type-level markers post-FP-rewrite —
// the script now reads them via ISupportsIScalar / ISupportsNested interface
// implementation rather than RowFeatures property access.
//
// Two consumers:
//   - tests:_test:capability-matrix-freshness — generates fresh output, then
//     `git diff --quiet` against the committed file. Drift fails the meta-test.
//   - docs:_build-capability-matrix — generates as part of the docs barrel.
//
// Both invoke this script via `dotnet run scripts/_test/capability-matrix.cs`.
// ─────────────────────────────────────────────────────────────────────────────

using System.Reflection;
using System.Text;
using Flowthru.Data.Schema;
using Flowthru.Data.Schema.Mapping;
using Flowthru.Data.Storage;
using Flowthru.Data.Storage.Csv;
using Flowthru.Data.Storage.Excel;
using Flowthru.Data.Storage.Parquet;

var outputPath = args.Length > 0
  ? args[0]
  : ResolveDefaultOutputPath();

var entries = new[]
{
  InspectFormat<CsvFormatSerializer<MatrixProbeRow>>(
    "CSV",
    "Flowthru.Extensions.Csv",
    () => new CsvFormatSerializer<MatrixProbeRow>()
  ),
  InspectFormat<ExcelFormatSerializer<MatrixProbeRow>>(
    "Excel",
    "Flowthru.Extensions.Excel",
    () => new ExcelFormatSerializer<MatrixProbeRow>(sheetName: "Sheet1")
  ),
  InspectFormat<ParquetFormatSerializer<MatrixProbeRow>>(
    "Parquet",
    "Flowthru.Extensions.Parquet",
    () => new ParquetFormatSerializer<MatrixProbeRow>()
  ),
  InspectFormat<JsonFormatSerializer<MatrixProbeRow>>(
    "JSON",
    "Flowthru.Core (built-in)",
    () => new JsonFormatSerializer<MatrixProbeRow>()
  ),
};

var markdown = RenderMarkdown(entries);

Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
File.WriteAllText(outputPath, markdown);

Console.WriteLine($"Capability matrix written to {outputPath}");
return 0;

static FormatEntry InspectFormat<TSerializer>(
  string displayName,
  string assemblyShortName,
  Func<IFormatBase<MatrixProbeRow>> factory
)
{
  var concreteType = typeof(TSerializer);
  var openType = concreteType.IsGenericType
    ? concreteType.GetGenericTypeDefinition()
    : concreteType;

  var optOutAttr = openType.GetCustomAttribute<OptOutOfPropertyPlannerAttribute>();
  var consumesPlanner = optOutAttr is null;

  // Detect structural flat-only constraint via the open generic's parameter constraints.
  // A format declared `where TRow : ..., IFlatSchema, ...` is structurally incapable of
  // accepting nested schemas — its Nested cell in the matrix is "not applicable" rather
  // than a tracked false claim.
  var constrainedToFlat = false;
  var typeParams = openType.GetGenericArguments();
  if (typeParams.Length > 0)
  {
    var constraints = typeParams[0].GetGenericParameterConstraints();
    constrainedToFlat = constraints.Any(c => c == typeof(IFlatSchema));
  }

  // Phase D capability segments: the format's structural read/write surface is encoded
  // by which interface segments it implements. Reader is the floor (every first-party
  // format reads); writer is optional (Excel via ExcelDataReader is reader-only); the
  // stream-reader sub-interface is a structural claim of bounded-memory decoding
  // (e.g., CSV and Parquet, but not JSON's whole-array buffer).
  var implementsReader = typeof(IFormatRowReader<MatrixProbeRow>).IsAssignableFrom(concreteType);
  var implementsWriter = typeof(IFormatRowWriter<MatrixProbeRow>).IsAssignableFrom(concreteType);
  var implementsStreamReader = typeof(IFormatStreamReader<MatrixProbeRow>).IsAssignableFrom(concreteType);

  // Row-shape capability claims (post-FP-rewrite): the format declares support by
  // implementing the corresponding marker interface, not by setting a bool on a
  // runtime features bag. Absence of ISupportsNested combined with a flat-only
  // constraint is the canonical "structurally not applicable" cell.
  var supportsIScalar = typeof(ISupportsIScalar).IsAssignableFrom(concreteType);
  var supportsNested = typeof(ISupportsNested).IsAssignableFrom(concreteType);

  var instance = factory();
  return new FormatEntry(
    displayName,
    assemblyShortName,
    consumesPlanner,
    optOutAttr?.Reason,
    supportsIScalar,
    supportsNested,
    instance.Traits,
    constrainedToFlat,
    implementsReader,
    implementsWriter,
    implementsStreamReader
  );
}

static string RenderMarkdown(IReadOnlyList<FormatEntry> entries)
{
  var sb = new StringBuilder();

  sb.AppendLine("# Flowthru Format Extension Capability Matrix");
  sb.AppendLine();
  sb.AppendLine(
    "This document is **auto-generated** from each format extension's"
      + " marker-interface declarations (`ISupportsIScalar`, `ISupportsNested`)"
      + " and which capability segments it implements (`IFormatRowReader<TRow>`,"
      + " `IFormatRowWriter<TRow>`, `IFormatStreamReader<TRow>`)."
      + " Do not edit by hand —"
      + " the `_test:capability-matrix-freshness` meta-test fails on drift."
  );
  sb.AppendLine();
  sb.AppendLine(
    "Regenerate locally via `nx run tests:_test:capability-matrix-freshness`,"
      + " `nx run docs:build`, or directly via"
      + " `dotnet run scripts/_test/capability-matrix.cs`."
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
    "- **`✓`** — format implements the marker interface; the matching kit"
      + " conformance fixture round-trips successfully."
  );
  sb.AppendLine(
    "- **`✗`** — format does not implement the marker; could be implemented but"
      + " isn't. Tracked as a follow-up; kit fixtures requiring the feature skip"
      + " vacuously."
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
    var nestedCell = e.ConstrainedToFlat ? "—" : Cell(e.SupportsNested);
    sb.AppendLine(
      $"| **{e.Name}** ({e.AssemblyShortName}) | {shape} | {Cell(e.SupportsIScalar)} | {nestedCell} |"
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
      + " `Flowthru.Data.Storage.StorageTraits` for the full surface."
  );
  sb.AppendLine();
  sb.AppendLine(
    "**Read / Write / Stream columns** carry two signals. Phase D"
      + " (capability-segmented interfaces) split the format surface into"
      + " `IFormatRowReader<TRow>`, `IFormatRowWriter<TRow>`, and"
      + " `IFormatStreamReader<TRow>` (a sub-interface of the row reader,"
      + " marking bounded-memory decoding). A format that does not implement a"
      + " segment is *structurally* incapable of that operation — the absence"
      + " is enforced by the type system, not a runtime trait flag. A format"
      + " that implements the segment but reports `Traits.CanWrite = false`"
      + " (etc.) is *runtime*-disabled."
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
        + $" {SegmentCell(e.ImplementsStreamReader, e.Traits.CanStream)} |"
        + $" {Cell(e.Traits.CanAppend)} | {Cell(e.Traits.IsTransactional)} |"
    );
  }
  sb.AppendLine();

  return sb.ToString();
}

static string Cell(bool flag) => flag ? "✓" : "✗";

// Phase D segment cell: combines structural (interface implementation) with runtime
// (Traits flag). Structural absence wins — a format that doesn't implement the
// segment cannot have it enabled at runtime.
static string SegmentCell(bool implementsSegment, bool runtimePermits) =>
  !implementsSegment ? "—" : (runtimePermits ? "✓" : "✗");

static string EscapeMarkdown(string input) =>
  input.Replace("|", "\\|").Replace("\n", " ").Replace("\r", "");

static string ResolveDefaultOutputPath()
{
  // Walk up from the script's CWD to the repo root, identified by the docs/ directory.
  var dir = Directory.GetCurrentDirectory();
  while (!string.IsNullOrEmpty(dir) && !Directory.Exists(Path.Combine(dir, "docs")))
  {
    var parent = Directory.GetParent(dir)?.FullName;
    if (parent == null || parent == dir)
    {
      throw new InvalidOperationException(
        "Could not locate repository root from "
          + Directory.GetCurrentDirectory()
          + ". Pass the output path as the first argument."
      );
    }
    dir = parent;
  }

  return Path.Combine(dir, "docs", "reference", "extensions", "capability-matrix.md");
}

internal record FormatEntry(
  string Name,
  string AssemblyShortName,
  bool ConsumesPlanner,
  string? OptOutReason,
  bool SupportsIScalar,
  bool SupportsNested,
  StorageTraits Traits,
  bool ConstrainedToFlat,
  bool ImplementsReader,
  bool ImplementsWriter,
  bool ImplementsStreamReader
);

/// <summary>
/// Inline probe schema for the capability-matrix generator. Flat fields
/// only so it satisfies the <c>IFlatSchema</c>-constrained formats
/// (CSV, Excel, Parquet) and <c>IStructuredSerializable</c> for JSON.
/// </summary>
/// <remarks>
/// File-based C# programs don't pull in the <c>[FlowthruSchema]</c>
/// source generator that would normally emit the four marker
/// interfaces, so we declare them inline. The script only constructs
/// the format types and reflects on them — no actual serialization
/// runs — so the manual marker declarations are sufficient and never
/// diverge from a real schema's emitted surface.
/// </remarks>
public record MatrixProbeRow :
  IFlatSchema,
  ITextSerializable,
  IBinarySerializable,
  IStructuredSerializable
{
  public required int Id { get; init; }
  public required string Name { get; init; }
}
