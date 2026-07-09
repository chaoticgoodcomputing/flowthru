namespace Flowthru.Data.Storage.EFCore.Npgsql;

/// <summary>
/// What a bulk transfer does to rows already in the PostgreSQL target
/// table before landing the incoming data. The mode applies to both
/// transfer rungs — the native raw binary <c>COPY</c> passthrough and the
/// streaming fallback — and always executes inside the same transaction
/// as the load itself, so a failed transfer leaves the target exactly as
/// it was (including its pre-existing rows).
/// </summary>
/// <remarks>
/// Raw <c>COPY FROM</c> appends by nature; Flowthru makes the choice
/// explicit here instead of inheriting that accident. The default is
/// <see cref="Replace"/> because a bulk transfer's motivating use is
/// promotion — "make the target table this source table" — and because it
/// matches the EFCore item's default save semantics (replace).
/// </remarks>
public enum NpgsqlBulkImportMode
{
  /// <summary>
  /// Empty the target table (<c>TRUNCATE</c>), then load — the target
  /// ends up an exact copy of the source. The truncate runs inside the
  /// transfer's transaction, so a failed load rolls the old rows back
  /// into place. Note that <c>TRUNCATE</c> requires the
  /// <c>TRUNCATE</c> privilege and fails when other tables hold foreign
  /// keys into the target.
  /// </summary>
  Replace,

  /// <summary>
  /// Keep existing rows and add the incoming ones. Key collisions
  /// between existing and incoming rows fail the transfer (and roll it
  /// back) — there is no upsert in a raw byte passthrough.
  /// </summary>
  Append,
}
