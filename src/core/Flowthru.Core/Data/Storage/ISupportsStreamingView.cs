using Flowthru.Prelude;

namespace Flowthru.Data.Storage;

/// <summary>
/// Capability seam for the streaming catalog view: a composed storage adapter
/// whose format can stream can hand out a deferred <see cref="FlowSource{TRow}"/>
/// over its rows — the source that
/// <c>IItem&lt;IEnumerable&lt;TRow&gt;&gt;.AsStream()</c> wraps. The underlying
/// byte source is acquired on the first pull and released on every exit path.
/// </summary>
/// <remarks>
/// Only formats that genuinely stream (implement
/// <see cref="IFormatStreamReader{TRow}"/>) can produce a streaming view;
/// <see cref="SupportsStreaming"/> lets <c>AsStream()</c> reject non-streaming
/// formats and direct adapters (EFCore, Sheets, GQL) at wire-up rather than
/// silently materialising.
/// </remarks>
/// <typeparam name="TRow">The row type produced by the stream.</typeparam>
public interface ISupportsStreamingView<TRow>
  where TRow : notnull
{
  /// <summary>
  /// True when the underlying format supports streaming reads. When false,
  /// <see cref="OpenStreamingSource"/> throws.
  /// </summary>
  bool SupportsStreaming { get; }

  /// <summary>
  /// Build a deferred <see cref="FlowSource{TRow}"/> over this adapter's medium
  /// and reader. Nothing is opened until the source is compiled and run.
  /// </summary>
  FlowSource<TRow> OpenStreamingSource();
}
