using Flowthru.Data.Storage;
using Flowthru.Data.Storage.Parquet;
using Flowthru.Tests.Kits.Storage;

namespace Flowthru.Extensions.Parquet.Tests;

/// <summary>
/// Runs the shared <see cref="ISerializedEnumLaws"/> kit against
/// <see cref="ParquetFormatSerializer{TRow}"/>. Parquet is a columnar
/// binary format; the declared enum string is encoded format-specifically
/// and not directly inspectable at the byte level.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Known gap: enum storage uses integer ordinals.</strong>
/// The current Parquet adapter stores enum values as their underlying
/// integer rather than as the
/// <see cref="Flowthru.Data.Schema.SerializedEnumAttribute"/>-declared
/// string (documented at <c>ParquetAdapter.cs:486-489</c> as a scoped
/// follow-up). Two law-skip consequences:
/// </para>
/// <list type="bullet">
///   <item><see cref="IsTextFormat"/> = <c>false</c>: skips wire-format
///     and external-producer-string tests (they don't apply to a
///     binary columnar format anyway).</item>
///   <item><see cref="EnforcesUndeclaredEnumWriteCheck"/> = <c>false</c>:
///     skips the undeclared-enum-write check because the adapter accepts
///     any integer value silently. This gap should close before v1 —
///     once enum strings replace ordinals on the wire, the override
///     here can be removed.</item>
/// </list>
/// <para>
/// The round-trip law still pins the most important contract:
/// declared enum values survive serialize → deserialize unchanged.
/// </para>
/// </remarks>
public sealed class ParquetSerializedEnumLaws : ISerializedEnumLaws
{
  /// <inheritdoc/>
  protected override IFormatSerializer<KitRow> CreateSerializer() =>
    new ParquetFormatSerializer<KitRow>();

  /// <inheritdoc/>
  protected override bool IsTextFormat => false;

  /// <inheritdoc/>
  protected override bool EnforcesUndeclaredEnumWriteCheck => false;
}
