using Flowthru.Data.Storage;

namespace Flowthru.Data.Catalog;

/// <summary>
/// Marker interface every per-file format builder implements so the
/// universal <see cref="DirectoryOfExtensions.Directory{T, TBuilder}"/>
/// lift can wrap it into a <see cref="DirectoryOf{T}"/> item without
/// the format extension shipping its own per-directory builder.
/// </summary>
/// <typeparam name="T">
/// The per-file payload type — e.g. <c>IEnumerable&lt;TRow&gt;</c> for
/// row-stream formats (CSV, Parquet), <c>byte[]</c> for binary blobs,
/// <c>string</c> for plain text.
/// </typeparam>
/// <remarks>
/// <para>
/// Per the §2.6 catalog-builder discipline, formats expose a single
/// per-file builder. The universal directory lift is then "format ∘
/// directory-of" — apply any IFileItemBuilder per file, scan a
/// directory for matching files, and surface the result as
/// <see cref="DirectoryOf{T}"/>. This keeps directory-of-X support
/// completeness-by-construction: a new format only needs to implement
/// this interface to gain directory support automatically.
/// </para>
/// <para>
/// Implementers should NOT call their own <c>AtPath</c>/path-setter
/// methods inside <see cref="CreateAdapterForFile(string)"/> — the
/// directory adapter supplies a fresh path per file, late, from its
/// directory scan. The builder's own path field is only consulted by
/// its <c>Build()</c> method for the single-file case.
/// </para>
/// </remarks>
public interface IFileItemBuilder<T> where T : notnull
{
  /// <summary>The catalog item label (shared with the directory item).</summary>
  string Label { get; }

  /// <summary>
  /// Default filename pattern for the directory's filesystem scan
  /// (e.g. <c>*.csv</c>, <c>*.parquet</c>). The directory builder
  /// uses this when the user doesn't override via
  /// <see cref="DirectoryOfBuilder{T}.WithFilePattern(string)"/>.
  /// </summary>
  string DefaultFilePattern { get; }

  /// <summary>
  /// Build a per-file <see cref="IStorageAdapter{T}"/> for the given
  /// path, using whatever format-specific configuration the builder
  /// has accumulated (resolver, sheet name, null sentinels, etc.).
  /// Called once per file by the directory adapter at iteration time.
  /// </summary>
  IStorageAdapter<T> CreateAdapterForFile(string filePath);
}
