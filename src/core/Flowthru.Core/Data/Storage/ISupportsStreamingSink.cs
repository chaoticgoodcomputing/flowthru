using Flowthru.Prelude;

namespace Flowthru.Data.Storage;

/// <summary>
/// Optional capability — a storage adapter (or a custom item) implementing
/// this interface can receive rows through a batch-lifecycle
/// <see cref="IFlowSink{T}"/>, making it a valid target for the streaming
/// rung of a bulk transfer (<c>FlowSource.Compile().Into</c>, O(batch)
/// memory). The write-side sibling of <see cref="ISupportsStreamingView{TRow}"/>.
/// </summary>
/// <remarks>
/// Presence of the interface is the opt-in; adapters whose writes are
/// inherently one-shot (composed file formats, in-memory stores) simply
/// don't implement it, and pre-flight rung negotiation rejects them as
/// streaming-transfer targets instead of silently materialising.
/// </remarks>
/// <typeparam name="TRow">The row type the sink consumes.</typeparam>
public interface ISupportsStreamingSink<TRow>
  where TRow : notnull
{
  /// <summary>
  /// Build the batch sink this target writes through. Construction must be
  /// cheap and effect-free — the sink's <see cref="IFlowSink{T}.OpenAsync"/>
  /// performs the real work (open a transaction, begin a bulk writer), so a
  /// sink that is built but never driven acquires nothing.
  /// </summary>
  IFlowSink<TRow> OpenStreamingSink();
}
