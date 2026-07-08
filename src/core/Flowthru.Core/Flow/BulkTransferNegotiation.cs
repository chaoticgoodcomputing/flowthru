using Flowthru.Data.Catalog;
using Flowthru.Data.Storage;

namespace Flowthru.Flow;

/// <summary>
/// The execution rung a bulk transfer runs on, selected at pre-flight by
/// <see cref="BulkTransferNegotiation.Negotiate{T}"/>.
/// </summary>
public enum BulkTransferRung
{
  /// <summary>
  /// The row-level fallback: the source streams a
  /// <see cref="FlowSource{T}"/> into the target's
  /// <see cref="IFlowSink{T}"/> in O(batch) memory. Always the bottom
  /// rung — correct for any pairing whose endpoints can stream and sink,
  /// but pays row-at-a-time marshalling costs a native pairing avoids.
  /// </summary>
  Streaming,

  /// <summary>
  /// A provider-native byte passthrough between a paired
  /// <see cref="ISupportsBulkExport"/> source and
  /// <see cref="ISupportsBulkImport"/> target (same provider, same wire
  /// format). The execution machinery for this rung has not shipped yet,
  /// so negotiation never selects it in the current version; a matched
  /// capability pair downgrades — visibly — to <see cref="Streaming"/>.
  /// </summary>
  Native,
}

/// <summary>
/// The outcome of pre-flight rung negotiation for one bulk transfer step:
/// which rung was selected and, in <see cref="Reason"/>, why — including
/// the capability-pair status, so a downgrade to the streaming fallback is
/// visible in the validated plan rather than silent.
/// </summary>
/// <param name="StepLabel">The transfer step the decision belongs to.</param>
/// <param name="Rung">The selected execution rung.</param>
/// <param name="Reason">Human-readable selection rationale.</param>
public sealed record BulkTransferDecision(
  string StepLabel,
  BulkTransferRung Rung,
  string Reason
);

/// <summary>
/// Pre-flight rung negotiation for
/// <see cref="FlowBuilderBulkTransferExtensions.AddBulkTransfer{T}"/>:
/// probe both endpoints' capabilities, check pair compatibility, and
/// select an execution rung — or fail with an accumulated
/// <see cref="PreFlightError.BulkTransferRungUnavailable"/> when no rung
/// can execute the pairing.
/// </summary>
/// <remarks>
/// <para>
/// Negotiation is deliberately zero-I/O: it reads capability interfaces
/// and their identity metadata but never opens a channel, so it runs in
/// the hermetic pre-flight tier and an offline smoke test still sees an
/// impossible transfer. The same function backs the transfer step's
/// runtime endpoints, so what pre-flight reports is — by construction —
/// what the step executes.
/// </para>
/// </remarks>
public static class BulkTransferNegotiation
{
  /// <summary>
  /// Negotiate the execution rung for a <paramref name="source"/> →
  /// <paramref name="target"/> transfer. Valid results carry the selected
  /// rung and its rationale; Invalid results accumulate one
  /// <see cref="PreFlightError.BulkTransferRungUnavailable"/> per unmet
  /// requirement so the user sees every problem at once.
  /// </summary>
  /// <typeparam name="T">The row type moving from source to target.</typeparam>
  /// <param name="source">The transfer's source item.</param>
  /// <param name="target">The transfer's target item.</param>
  /// <param name="options">Transfer options; null = <see cref="BulkTransferOptions.Default"/>.</param>
  /// <param name="stepLabel">
  /// The transfer step's label, reported on the decision and any errors.
  /// Defaults to the label <c>AddBulkTransfer</c> derives from the
  /// endpoint items.
  /// </param>
  public static Validated<PreFlightError, BulkTransferDecision> Negotiate<T>(
    IItem<IEnumerable<T>> source,
    IItem<IEnumerable<T>> target,
    BulkTransferOptions? options = null,
    string? stepLabel = null
  )
    where T : notnull
  {
    if (source is null) throw new ArgumentNullException(nameof(source));
    if (target is null) throw new ArgumentNullException(nameof(target));

    var label = stepLabel ?? DefaultStepLabel(source, target);
    var effectiveOptions = options ?? BulkTransferOptions.Default;

    // Capability-pair probe — type tests and identity metadata only, no
    // channel is opened. The status string rides into the decision's
    // Reason (or the RequireNative error) so the pairing verdict is
    // always visible, whichever way selection goes.
    var export = source.TryGetBulkExport();
    var import = target.TryGetBulkImport();
    string pairStatus;
    if (export is null || import is null)
    {
      var exportStatus = export is null
        ? $"source '{source.Label}' declares no bulk-export capability"
        : $"source '{source.Label}' exports {export.BulkProvider}/{export.BulkWireFormat}";
      var importStatus = import is null
        ? $"target '{target.Label}' declares no bulk-import capability"
        : $"target '{target.Label}' imports {import.BulkProvider}/{import.BulkWireFormat}";
      pairStatus = $"no native capability pair ({exportStatus}; {importStatus})";
    }
    else if (
      !string.Equals(export.BulkProvider, import.BulkProvider, StringComparison.Ordinal)
      || !string.Equals(export.BulkWireFormat, import.BulkWireFormat, StringComparison.Ordinal)
    )
    {
      pairStatus =
        $"capability pair incompatible (source '{source.Label}' exports "
        + $"{export.BulkProvider}/{export.BulkWireFormat}; target '{target.Label}' imports "
        + $"{import.BulkProvider}/{import.BulkWireFormat})";
    }
    else
    {
      // A matched pair still cannot run natively: the native rung's
      // execution machinery has not shipped yet. Saying so here keeps the
      // downgrade visible instead of silently taking the slow path.
      pairStatus =
        $"capability pair matched ({export.BulkProvider}/{export.BulkWireFormat}), but the "
        + "native transfer rung is not available in this version of Flowthru";
    }

    if (effectiveOptions.RequireNative)
    {
      return Validated<PreFlightError, BulkTransferDecision>.Fail(
        new PreFlightError.BulkTransferRungUnavailable(
          label,
          $"RequireNative is set but no native path is available — {pairStatus}. "
          + "Unset RequireNative to allow the streaming fallback."
        )
      );
    }

    // Streaming-fallback feasibility. The two requirements are
    // independent, so they accumulate rather than short-circuit — a
    // pairing that can neither stream nor sink reports both at once.
    var errors = new List<PreFlightError>();
    if (ResolveStreamingView(source) is null)
    {
      errors.Add(new PreFlightError.BulkTransferRungUnavailable(
        label,
        $"the streaming fallback needs a streaming-capable source, but '{source.Label}' cannot "
        + "stream reads. Use a streaming-capable format for the source, or wire an ordinary "
        + "step for the eager path."
      ));
    }
    if (ResolveStreamingSink(target) is null)
    {
      errors.Add(new PreFlightError.BulkTransferRungUnavailable(
        label,
        $"the streaming fallback needs a sink-capable target, but '{target.Label}' cannot "
        + "receive a batch sink. Use a sink-capable target, or wire an ordinary step for "
        + "the eager path."
      ));
    }
    if (errors.Count > 0)
    {
      return Validated<PreFlightError, BulkTransferDecision>.Fail(errors);
    }

    return Validated<PreFlightError, BulkTransferDecision>.Pure(
      new BulkTransferDecision(
        label,
        BulkTransferRung.Streaming,
        $"streaming fallback selected — {pairStatus}"
      )
    );
  }

  /// <summary>
  /// The step label <c>AddBulkTransfer</c> derives when the caller does
  /// not supply one.
  /// </summary>
  internal static string DefaultStepLabel(IItem source, IItem target) =>
    $"BulkTransfer_{source.Label}_to_{target.Label}";

  /// <summary>
  /// Resolve the streaming-read capability behind <paramref name="source"/>,
  /// or null when it cannot stream. Mirrors the probe
  /// <c>CatalogItemExtensions.AsStream</c> performs (including the
  /// <c>Constrain()</c> unwrap, which never affects reads), and
  /// additionally recognises items that implement the capability directly.
  /// </summary>
  internal static ISupportsStreamingView<T>? ResolveStreamingView<T>(IItem<IEnumerable<T>> source)
    where T : notnull
  {
    if (source is ISupportsStreamingView<T> { SupportsStreaming: true } direct) return direct;

    var adapter = (source as Item<IEnumerable<T>>)?.Storage;
    if (adapter is ConstrainedStorageAdapter<IEnumerable<T>> constrained)
    {
      adapter = constrained.Inner;
    }
    return adapter is ISupportsStreamingView<T> { SupportsStreaming: true } view ? view : null;
  }

  /// <summary>
  /// Resolve the streaming-sink capability behind <paramref name="target"/>,
  /// or null when it cannot receive a batch sink. Deliberately does
  /// <em>not</em> unwrap <c>Constrain()</c> wrappers: narrowing an item's
  /// write traits must also withhold its raw sink, or the constraint could
  /// be bypassed.
  /// </summary>
  internal static ISupportsStreamingSink<T>? ResolveStreamingSink<T>(IItem<IEnumerable<T>> target)
    where T : notnull
  {
    if (target is ISupportsStreamingSink<T> direct) return direct;
    return (target as Item<IEnumerable<T>>)?.Storage as ISupportsStreamingSink<T>;
  }
}

/// <summary>
/// Framework-internal window onto a bulk transfer step's negotiation,
/// implemented by the target endpoint item
/// <c>AddBulkTransfer</c> wires as the step's output. The pre-flight
/// pipeline probes step outputs for this interface to fold negotiation
/// failures into the aggregate, and the host probes it to report the
/// selected rung in the run's validation output.
/// </summary>
internal interface IBulkTransferEndpoint
{
  /// <summary>The (lazily computed, cached) negotiation outcome for this transfer.</summary>
  Validated<PreFlightError, BulkTransferDecision> Negotiation { get; }
}
