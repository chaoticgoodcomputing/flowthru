using StreamingBulkLoad.Data._01_Raw.Schemas;

namespace StreamingBulkLoad.Flows.Shared;

/// <summary>
/// The one stateless transform both ingest variants apply, factored out so the
/// eager and streaming paths are provably identical work — only the memory
/// grain differs. <see cref="Normalize"/> is the <c>.Map</c>; <see cref="IsValid"/>
/// is the <c>.Where</c>.
/// </summary>
/// <remarks>
/// Stateless and row-local by construction: neither function looks beyond the
/// single row it is handed, which is exactly why the work composes as lazy
/// streaming combinators (<c>source.Map(Normalize).Where(IsValid)</c>) with no
/// buffering. A transform that needed to see the whole dataset (a group-by, a
/// sort) would instead consume the eager view — see the README.
/// </remarks>
public static class TransactionCleaning
{
  /// <summary>
  /// Canonicalise the noisy category text (trim, upper-case). Returns a new
  /// record — pure, allocation-per-row, no shared state.
  /// </summary>
  public static TransactionRecord Normalize(TransactionRecord row) =>
    row with { Category = (row.Category ?? string.Empty).Trim().ToUpperInvariant() };

  /// <summary>
  /// Drop rows a downstream consumer would reject: zero-amount placeholders and
  /// rows with no category. The generator injects a fixed fraction of these so
  /// the filter demonstrably removes the same count on both paths.
  /// </summary>
  public static bool IsValid(TransactionRecord row) =>
    row.AmountCents != 0 && !string.IsNullOrWhiteSpace(row.Category);
}
