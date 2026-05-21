using Flowthru.Data.Schema;
using Flowthru.Data.Schema.Mapping;
using Flowthru.Data.Storage;

namespace Flowthru.Tests.Kits.Storage;

/// <summary>
/// Laws every format-adapter implementer that opts into the
/// <see cref="SerializedEnumAttribute"/> contract must satisfy. Each
/// inheriting fixture binds a concrete
/// <see cref="IFormatSerializer{TRow}"/> (or an equivalent format-level
/// entry point) and inherits round-trip + error-path tests for free.
/// </summary>
/// <remarks>
/// <para>
/// Replaces the prior <c>SerializedEnumConformance</c> kit. Behaves
/// identically; renamed per §2.11 to align with the algebra-laws
/// framing.
/// </para>
/// <para>
/// <strong>The contract this enforces.</strong> Every adapter that
/// claims <see cref="SerializedEnumAttribute"/> support must:
/// </para>
/// <list type="number">
///   <item>Write enum values using the
///     <see cref="SerializedEnumAttribute.Value"/> string, not the CLR
///     member name or ordinal.</item>
///   <item>Read those same strings back into the correct enum value
///     bidirectionally.</item>
///   <item>Fail loudly on unknown serialized strings (an external
///     producer wrote a string Flowthru's schema doesn't recognize).</item>
///   <item>Fail loudly on cast-from-int enum values outside the
///     declared range (a step produced an undeclared enum value).</item>
/// </list>
/// <para>
/// The bidirectional mapping is single-sourced by
/// <see cref="SerializedEnumMappings.Build"/>; the adapter reads it off
/// the planner's <see cref="EnumBindingInfo"/>. Different formats
/// (JSON, CSV, Parquet, Excel, EFCore, …) translate at their own field
/// boundary, but they all consume the same mapping — so the rule is
/// "one declared mapping, every format honors it identically".
/// </para>
/// <para>
/// <strong>Implementer ceremony.</strong> Inheritors override
/// <see cref="CreateSerializer"/> to build a fresh serializer
/// instance per test case. The conformance fixture supplies the test
/// fixtures (enums and rows) so every implementer exercises identical
/// inputs.
/// </para>
/// </remarks>
[TestFixture]
public abstract class ISerializedEnumLaws
{
  /// <summary>
  /// Build a fresh <see cref="IFormatSerializer{TRow}"/> instance for
  /// the conformance fixture row. The convention is to build a
  /// default-configured serializer — exactly the shape end users get.
  /// </summary>
  protected abstract IFormatSerializer<KitRow> CreateSerializer();

  /// <summary>
  /// True when the format produces UTF-8-text payloads (JSON, CSV, XML, …)
  /// whose bytes can be inspected directly for declared serialized values.
  /// False for binary formats (Parquet's columnar pages, Excel's zipped
  /// XML) where the declared value is encoded in a format-specific way
  /// — those formats skip the wire-format / external-producer assertions
  /// and rely on the round-trip + undeclared-value tests instead. The
  /// round-trip law alone catches the most common regression (silent
  /// fallback to CLR member name or ordinal).
  /// </summary>
  protected virtual bool IsTextFormat => true;

  /// <summary>
  /// True when the adapter rejects cast-from-int enum values that fall
  /// outside the declared <see cref="SerializedEnumAttribute"/> range
  /// at write time. Adapters that store enums as integers (and therefore
  /// accept any int silently) override to <c>false</c> with a documented
  /// reason — the gap is then visible as a skipped test in run summaries
  /// rather than hidden via a passing-but-non-conformant adapter. The
  /// flag should be removed (and the adapter brought into conformance)
  /// before a v1 release that promises cross-format consistency.
  /// </summary>
  protected virtual bool EnforcesUndeclaredEnumWriteCheck => true;

  // ── Round-trip ─────────────────────────────────────────────────────────

  /// <summary>
  /// Sanity baseline — write then read recovers the value.
  /// </summary>
  [Test]
  public async Task RoundTrip_PreservesDeclaredEnumValue()
  {
    var serializer = CreateSerializer();
    var input = new KitRow { Status = KitCheckStatus.Complete, Rarity = KitRarity.MythicRare };

    using var stream = new MemoryStream();
    await serializer.SerializeRows(stream, OneRow(input));

    stream.Position = 0;
    var output = await ReadOne(serializer, stream);

    Assert.That(output.Status, Is.EqualTo(input.Status));
    Assert.That(output.Rarity, Is.EqualTo(input.Rarity));
  }

  /// <summary>
  /// Round-trips every declared value (full coverage of the mapping).
  /// </summary>
  [TestCaseSource(nameof(EveryStatus))]
  public async Task RoundTrip_EveryDeclaredStatusValue(KitCheckStatus value)
  {
    var serializer = CreateSerializer();
    var input = new KitRow { Status = value, Rarity = KitRarity.Common };

    using var stream = new MemoryStream();
    await serializer.SerializeRows(stream, OneRow(input));

    stream.Position = 0;
    var output = await ReadOne(serializer, stream);

    Assert.That(output.Status, Is.EqualTo(value));
  }

  // ── Wire format ────────────────────────────────────────────────────────

  /// <summary>
  /// The on-disk payload must contain the declared
  /// <see cref="SerializedEnumAttribute.Value"/> string verbatim, not
  /// the CLR member name or an ordinal.
  /// </summary>
  [Test]
  public async Task WireFormat_ContainsDeclaredSerializedString()
  {
    if (!IsTextFormat)
    {
      Assert.Ignore(
        "Binary format — wire-format inspection is format-specific and not asserted "
          + "at the kit level. The round-trip law (above) covers the regression-net case."
      );
    }
    var serializer = CreateSerializer();
    var input = new KitRow { Status = KitCheckStatus.Complete, Rarity = KitRarity.MythicRare };

    using var stream = new MemoryStream();
    await serializer.SerializeRows(stream, OneRow(input));

    var text = System.Text.Encoding.UTF8.GetString(stream.ToArray());
    // Word-boundary regex matching is format-agnostic: it accepts JSON's
    // `"mythic_rare"`, CSV's `,mythic_rare\n` (or value-at-line-end), XML's
    // `>mythic_rare<`, etc. — all forms where the declared string stands as
    // a complete token on the wire.
    Assert.That(
      System.Text.RegularExpressions.Regex.IsMatch(text, @"\bt\b"),
      Is.True,
      "On-disk payload must contain the declared serialized string for "
        + "[SerializedEnum(\"t\")] Complete as a complete token, not the CLR member "
        + "name 'Complete' or its ordinal. Got payload: " + text
    );
    Assert.That(
      System.Text.RegularExpressions.Regex.IsMatch(text, @"\bmythic_rare\b"),
      Is.True,
      "Multi-character snake_case mappings must round-trip verbatim. Got payload: " + text
    );
    Assert.That(
      text,
      Does.Not.Contain("Complete"),
      "Adapters that don't honor [SerializedEnum] tend to fall back to writing the "
        + "CLR member name — that's the regression this assertion catches."
    );
    Assert.That(
      text,
      Does.Not.Contain("MythicRare"),
      "CLR member name leakage — same regression as 'Complete'."
    );
  }

  // ── Failure modes ──────────────────────────────────────────────────────

  /// <summary>
  /// An external producer wrote a string the schema doesn't recognize.
  /// Adapters must surface this as a deserialization error, not silently
  /// pick the default enum value.
  /// </summary>
  [Test]
  public void Read_UnknownSerializedString_Fails()
  {
    if (!IsTextFormat)
    {
      Assert.Ignore(
        "Binary format — synthesising an undeclared-string payload requires format-specific "
          + "byte manipulation outside the kit's scope. Format-specific test fixtures may "
          + "add their own version of this assertion."
      );
    }
    var serializer = CreateSerializer();
    var payload = BuildPayloadWithUnknownStatus(serializer);

    Assert.That(
      async () =>
      {
        using var stream = new MemoryStream(payload);
        _ = await ReadOne(serializer, stream);
      },
      Throws.Exception,
      "Reading an undeclared enum string must throw — silent fallback to default "
        + "would let upstream schema drift slip through unnoticed."
    );
  }

  /// <summary>
  /// A step produced a cast-from-int enum value outside the declared
  /// range (e.g. <c>(KitCheckStatus)999</c>). Adapters must refuse to
  /// write it.
  /// </summary>
  [Test]
  public void Write_UndeclaredEnumValue_Fails()
  {
    if (!EnforcesUndeclaredEnumWriteCheck)
    {
      Assert.Ignore(
        "Adapter currently stores enums as their integer ordinal and silently accepts "
          + "any cast-from-int value (see EnforcesUndeclaredEnumWriteCheck = false on "
          + "the implementing fixture). The deferred gap should be closed before v1 — "
          + "until then the test is skipped to keep the run summary honest."
      );
    }
    var serializer = CreateSerializer();
    var bogus = new KitRow
    {
      Status = (KitCheckStatus)999,
      Rarity = KitRarity.Common,
    };

    Assert.That(
      async () =>
      {
        using var stream = new MemoryStream();
        await serializer.SerializeRows(stream, OneRow(bogus));
      },
      Throws.Exception,
      "Writing a cast-from-int enum value outside the declared range must throw — "
        + "silently writing a numeric or empty string would corrupt the on-disk schema."
    );
  }

  // ── Adapter-provided hooks ─────────────────────────────────────────────

  /// <summary>
  /// Build a wire-format payload whose <see cref="KitRow.Status"/> field
  /// carries a string the schema does not recognize. The default
  /// implementation writes a valid payload, then word-boundary-replaces
  /// the declared <c>"t"</c> token with <c>"definitely_unknown_value"</c>
  /// — format-agnostic enough to work for JSON, CSV, XML, and any other
  /// text format that emits the declared string as a standalone token.
  /// Adapters with format-specific encodings can override this hook.
  /// </summary>
  protected virtual byte[] BuildPayloadWithUnknownStatus(IFormatSerializer<KitRow> serializer)
  {
    using var stream = new MemoryStream();
    var task = serializer.SerializeRows(
      stream,
      OneRow(new KitRow { Status = KitCheckStatus.Complete, Rarity = KitRarity.Common })
    );
    task.GetAwaiter().GetResult();
    var text = System.Text.Encoding.UTF8.GetString(stream.ToArray());
    text = System.Text.RegularExpressions.Regex.Replace(
      text, @"\bt\b", "definitely_unknown_value"
    );
    return System.Text.Encoding.UTF8.GetBytes(text);
  }

  // ── Helpers ────────────────────────────────────────────────────────────

  private static async IAsyncEnumerable<KitRow> OneRow(KitRow row)
  {
    yield return row;
    await Task.CompletedTask;
  }

  private static async Task<KitRow> ReadOne(IFormatSerializer<KitRow> serializer, Stream stream)
  {
    await foreach (var row in serializer.DeserializeRows(stream))
    {
      return row;
    }
    throw new InvalidOperationException("Stream contained no rows.");
  }

  private static IEnumerable<KitCheckStatus> EveryStatus() =>
    System.Enum.GetValues<KitCheckStatus>();
}

// ── Laws fixtures (shared across every format) ─────────────────────────

/// <summary>
/// Two-member enum exercising the shortest declared serialization form
/// (single-character abbreviations) — chosen so the wire-format check
/// catches adapters that fall back to the CLR member name.
/// </summary>
public enum KitCheckStatus
{
  /// <summary>Complete; serialized as <c>"t"</c>.</summary>
  [SerializedEnum("t")] Complete,
  /// <summary>Incomplete; serialized as <c>"f"</c>.</summary>
  [SerializedEnum("f")] Incomplete,
}

/// <summary>
/// Four-member enum exercising multi-character snake_case
/// serialization — ensures the conformance check distinguishes "the
/// adapter wrote the serialized form" from "the adapter wrote a string
/// that happens to alphabetically prefix-match the CLR name".
/// </summary>
public enum KitRarity
{
  [SerializedEnum("common")] Common,
  [SerializedEnum("uncommon")] Uncommon,
  [SerializedEnum("rare")] Rare,
  [SerializedEnum("mythic_rare")] MythicRare,
}

/// <summary>
/// Conformance row carrying both enums. Required-init members exercise
/// the planner's required-property classification at the same time.
/// </summary>
[FlowthruSchema]
public partial record KitRow
{
  /// <summary>Two-member enum field.</summary>
  public required KitCheckStatus Status { get; init; }
  /// <summary>Four-member enum field with snake_case serialization.</summary>
  public required KitRarity Rarity { get; init; }
}
