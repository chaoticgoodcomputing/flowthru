using Apache.Arrow;
using Apache.Arrow.Types;
using Flowthru.Data.Schema;
using Flowthru.Step.Python;
using Flowthru.Step.Python.Internal;

namespace Flowthru.Extensions.Python.Tests;

/// <summary>
/// Pins the scalar half of <see cref="ArrowMarshaller"/>'s contract — the
/// types <see cref="ArrowMarshallerListTests"/> doesn't cover. Each test
/// nails one behavior: a single scalar round-trip, a single error path, or
/// a single coercion rule. The companion files cover list types and the
/// reflection-wrapper visibility regression.
/// </summary>
[TestFixture]
[Category("Python")]
public class ArrowMarshallerScalarTests
{
  // ──────────────────────────────────────────────────────────────
  // Schemas
  // ──────────────────────────────────────────────────────────────

  /// <summary>
  /// One row across the full scalar matrix. Property order is also the
  /// asserted column order in <see cref="Roundtrip_All_Scalars_Preserves_Column_Order"/>.
  /// </summary>
  [FlowthruSchema]
  public partial record AllScalars
  {
    public required int IntField { get; init; }
    public required long LongField { get; init; }
    public required float FloatField { get; init; }
    public required double DoubleField { get; init; }
    public required bool BoolField { get; init; }
    public required string StringField { get; init; }
    public required DateTime DateTimeField { get; init; }
    public required DateTimeOffset DateTimeOffsetField { get; init; }
    public required TimeSpan TimeSpanField { get; init; }
    public required Guid GuidField { get; init; }
    public required byte[] BinaryField { get; init; }
  }

  [FlowthruSchema]
  public partial record AllNullableScalars
  {
    public required int? IntField { get; init; }
    public required long? LongField { get; init; }
    public required float? FloatField { get; init; }
    public required double? DoubleField { get; init; }
    public required bool? BoolField { get; init; }
    // string is reference-typed; nullable on the CLR side is annotation-only
    public required string? StringField { get; init; }
    public required DateTime? DateTimeField { get; init; }
    public required DateTimeOffset? DateTimeOffsetField { get; init; }
    public required TimeSpan? TimeSpanField { get; init; }
    public required Guid? GuidField { get; init; }
    public required byte[]? BinaryField { get; init; }
  }

  [FlowthruSchema]
  public partial record SingleIntRow
  {
    public required int Value { get; init; }
  }

  [FlowthruSchema]
  public partial record SingleDateTimeRow
  {
    public required DateTime When { get; init; }
  }

  [FlowthruSchema]
  public partial record SingleDateTimeOffsetRow
  {
    public required DateTimeOffset When { get; init; }
  }

  [FlowthruSchema]
  public partial record SingleGuidRow
  {
    public required Guid Id { get; init; }
  }

  [FlowthruSchema]
  public partial record SingleBinaryRow
  {
    public required byte[] Payload { get; init; }
  }

  [FlowthruSchema]
  public partial record LabeledRow
  {
    [SerializedLabel("custom_name")]
    public required int Value { get; init; }
  }

  // Each error-path test gets its own enum type, because
  // ArrowMarshaller caches the [SerializedEnum] map per Type forever and
  // a single bad enum would poison later happy-path tests.
  public enum HappyEnum
  {
    [SerializedEnum("R")] Red,
    [SerializedEnum("G")] Green,
    [SerializedEnum("B")] Blue,
  }

  public enum MissingAttrEnum
  {
    [SerializedEnum("A")] Alpha,
    // Intentionally missing [SerializedEnum] — triggers the runtime
    // InvalidOperationException in GetSerializedEnumMap.
    Beta,
  }

  public enum UnknownStringEnum
  {
    [SerializedEnum("ok")] Ok,
  }

  [FlowthruSchema]
  public partial record HappyEnumRow
  {
    public required int Id { get; init; }
    public required HappyEnum Color { get; init; }
  }

  [FlowthruSchema]
  public partial record MissingAttrEnumRow
  {
    public required int Id { get; init; }
    public required MissingAttrEnum Choice { get; init; }
  }

  [FlowthruSchema]
  public partial record UnknownStringEnumRow
  {
    public required int Id { get; init; }
    public required UnknownStringEnum Status { get; init; }
  }

  // Sibling schemas used by the numeric-coercion tests — same field name
  // ("Value"), different declared property type, so we can manually
  // assemble an Int32/Float batch and decode it into the wider CLR type.
  [FlowthruSchema]
  public partial record LongValueRow
  {
    public required long? Value { get; init; }
  }

  [FlowthruSchema]
  public partial record DoubleValueRow
  {
    public required double? Value { get; init; }
  }

  [FlowthruSchema]
  public partial record NullableListRow
  {
    public required int Id { get; init; }
    // The whole list is nullable — this exercises BuildListArray's
    // "listValue is null" branch on encode and ListArray.IsNull on decode.
    public required List<int>? Items { get; init; }
  }

  // Decimal probe schemas — default precision/scale, explicit attribute, nullable.

  [FlowthruSchema]
  public partial record DecimalRow
  {
    public required decimal Amount { get; init; }
  }

  [FlowthruSchema]
  public partial record NullableDecimalRow
  {
    public required decimal? Amount { get; init; }
  }

  [FlowthruSchema]
  public partial record ExplicitPrecisionDecimalRow
  {
    [ArrowDecimal(20, 4)]
    public required decimal Amount { get; init; }
  }

  [FlowthruSchema]
  public partial record TightPrecisionDecimalRow
  {
    [ArrowDecimal(5, 2)]
    public required decimal Amount { get; init; }
  }

  // ──────────────────────────────────────────────────────────────
  // Public surface guards
  // ──────────────────────────────────────────────────────────────

  [Test]
  public void ToRecordBatch_NullRows_Throws_ArgumentNullException()
  {
    var ex = Assert.Throws<ArgumentNullException>(
      () => ArrowMarshaller.ToRecordBatch<SingleIntRow>(null!)
    );
    Assert.That(ex!.ParamName, Is.EqualTo("rows"));
  }

  [Test]
  public void FromRecordBatch_NullBatch_Throws_ArgumentNullException()
  {
    var ex = Assert.Throws<ArgumentNullException>(
      () => ArrowMarshaller.FromRecordBatch<SingleIntRow>(null!).ToList()
    );
    Assert.That(ex!.ParamName, Is.EqualTo("batch"));
  }

  [Test]
  public void ToIpcBuffer_NullBatch_Throws_ArgumentNullException()
  {
    var ex = Assert.Throws<ArgumentNullException>(() => ArrowMarshaller.ToIpcBuffer(null!));
    Assert.That(ex!.ParamName, Is.EqualTo("batch"));
  }

  [Test]
  public void FromIpcBuffer_NullBuffer_Throws_ArgumentNullException()
  {
    var ex = Assert.Throws<ArgumentNullException>(() => ArrowMarshaller.FromIpcBuffer(null!));
    Assert.That(ex!.ParamName, Is.EqualTo("buffer"));
  }

  [Test]
  public void FromIpcBuffer_EmptyBuffer_Throws_InvalidData()
  {
    var ex = Assert.Throws<InvalidDataException>(
      () => ArrowMarshaller.FromIpcBuffer(System.Array.Empty<byte>())
    );
    Assert.That(ex!.Message, Does.Contain("empty").IgnoreCase);
  }

  [Test]
  public void FromIpcBuffer_GarbageBytes_Throws()
  {
    // The reader has no commitment about *which* exception escapes —
    // it could be an InvalidDataException (we threw it ourselves), an
    // Arrow-thrown exception, or an underlying stream error. Whichever
    // it is, what matters is that garbage doesn't silently produce an
    // empty batch.
    var garbage = new byte[] { 0xDE, 0xAD, 0xBE, 0xEF, 0xFA, 0xCE, 0xC0, 0xDE };
    Assert.That(
      () => ArrowMarshaller.FromIpcBuffer(garbage),
      Throws.Exception,
      "Garbage bytes must throw — silent success would mask a corrupted IPC frame."
    );
  }

  [Test]
  public void FromRecordBatch_Missing_Required_Field_Throws_InvalidOperation()
  {
    // Build a batch from a *different* schema (missing the field the
    // target expects), then ask FromRecordBatch to decode it as
    // SingleDateTimeRow — which expects a field named "When".
    //
    var batch = ArrowMarshaller.ToRecordBatch(new[] { new SingleIntRow { Value = 1 } });

    var ex = Assert.Throws<InvalidOperationException>(
      () => ArrowMarshaller.FromRecordBatch<SingleDateTimeRow>(batch).ToList()
    );
    Assert.That(ex!.Message, Does.Contain("When"),
      "Diagnostic must name the missing field so developers can locate the schema drift.");
    Assert.That(ex.Message, Does.Contain("SingleDateTimeRow"),
      "Diagnostic must name the target type so the offending property is unambiguous.");
  }

  // ──────────────────────────────────────────────────────────────
  // Scalar round-trips
  // ──────────────────────────────────────────────────────────────

  [Test]
  public void Roundtrip_All_Scalars_Preserves_Values()
  {
    var dt = new DateTime(2024, 6, 15, 12, 30, 45, DateTimeKind.Utc);
    var dto = new DateTimeOffset(2024, 6, 15, 12, 30, 45, TimeSpan.Zero);
    var guid = Guid.Parse("11111111-2222-3333-4444-555555555555");
    var row = new AllScalars
    {
      IntField = 42,
      LongField = 9_000_000_000L,
      FloatField = 1.5f,
      DoubleField = 3.14159265358979,
      BoolField = true,
      StringField = "hello",
      DateTimeField = dt,
      DateTimeOffsetField = dto,
      TimeSpanField = TimeSpan.FromMinutes(5),
      GuidField = guid,
      BinaryField = new byte[] { 1, 2, 3, 4 },
    };

    var batch = ArrowMarshaller.ToRecordBatch(new[] { row });
    var recovered = ArrowMarshaller.FromRecordBatch<AllScalars>(batch).ToList();

    Assert.That(recovered, Has.Count.EqualTo(1));
    var r = recovered[0];
    Assert.That(r.IntField, Is.EqualTo(42));
    Assert.That(r.LongField, Is.EqualTo(9_000_000_000L));
    Assert.That(r.FloatField, Is.EqualTo(1.5f));
    Assert.That(r.DoubleField, Is.EqualTo(3.14159265358979));
    Assert.That(r.BoolField, Is.True);
    Assert.That(r.StringField, Is.EqualTo("hello"));
    Assert.That(r.DateTimeField, Is.EqualTo(dt));
    Assert.That(r.DateTimeOffsetField, Is.EqualTo(dto));
    Assert.That(r.TimeSpanField, Is.EqualTo(TimeSpan.FromMinutes(5)));
    Assert.That(r.GuidField, Is.EqualTo(guid));
    Assert.That(r.BinaryField, Is.EqualTo(new byte[] { 1, 2, 3, 4 }));
  }

  [Test]
  public void Roundtrip_All_Scalars_Preserves_Column_Order()
  {
    var row = new AllScalars
    {
      IntField = 1, LongField = 2L, FloatField = 3f, DoubleField = 4d,
      BoolField = false, StringField = "x",
      DateTimeField = DateTime.UnixEpoch, DateTimeOffsetField = DateTimeOffset.UnixEpoch,
      TimeSpanField = TimeSpan.Zero, GuidField = Guid.Empty,
      BinaryField = System.Array.Empty<byte>(),
    };

    var batch = ArrowMarshaller.ToRecordBatch(new[] { row });

    var expected = new[]
    {
      nameof(AllScalars.IntField),
      nameof(AllScalars.LongField),
      nameof(AllScalars.FloatField),
      nameof(AllScalars.DoubleField),
      nameof(AllScalars.BoolField),
      nameof(AllScalars.StringField),
      nameof(AllScalars.DateTimeField),
      nameof(AllScalars.DateTimeOffsetField),
      nameof(AllScalars.TimeSpanField),
      nameof(AllScalars.GuidField),
      nameof(AllScalars.BinaryField),
    };
    var actual = batch.Schema.FieldsList.Select(f => f.Name).ToArray();
    Assert.That(actual, Is.EqualTo(expected),
      "Arrow column order must follow the schema's declared property order.");
  }

  [Test]
  public void Roundtrip_All_Nullable_Scalars_With_Null_Values_Preserves_Null()
  {
    var row = new AllNullableScalars
    {
      IntField = null,
      LongField = null,
      FloatField = null,
      DoubleField = null,
      BoolField = null,
      StringField = null,
      DateTimeField = null,
      DateTimeOffsetField = null,
      TimeSpanField = null,
      GuidField = null,
      BinaryField = null,
    };

    var batch = ArrowMarshaller.ToRecordBatch(new[] { row });
    var recovered = ArrowMarshaller.FromRecordBatch<AllNullableScalars>(batch).ToList();

    var r = recovered.Single();
    Assert.That(r.IntField, Is.Null);
    Assert.That(r.LongField, Is.Null);
    Assert.That(r.FloatField, Is.Null);
    Assert.That(r.DoubleField, Is.Null);
    Assert.That(r.BoolField, Is.Null);
    Assert.That(r.StringField, Is.Null);
    Assert.That(r.DateTimeField, Is.Null);
    Assert.That(r.DateTimeOffsetField, Is.Null);
    Assert.That(r.TimeSpanField, Is.Null);
    Assert.That(r.GuidField, Is.Null);
    Assert.That(r.BinaryField, Is.Null);
  }

  [Test]
  public void Roundtrip_All_Nullable_Scalars_With_Mixed_Null_And_Value_Rows()
  {
    var rows = new[]
    {
      new AllNullableScalars
      {
        IntField = 7, LongField = 70L, FloatField = 0.5f, DoubleField = 0.25,
        BoolField = true, StringField = "yes",
        DateTimeField = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
        DateTimeOffsetField = new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero),
        TimeSpanField = TimeSpan.FromSeconds(30),
        GuidField = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"),
        BinaryField = new byte[] { 0xFF },
      },
      new AllNullableScalars
      {
        IntField = null, LongField = null, FloatField = null, DoubleField = null,
        BoolField = null, StringField = null, DateTimeField = null,
        DateTimeOffsetField = null, TimeSpanField = null, GuidField = null,
        BinaryField = null,
      },
    };

    var batch = ArrowMarshaller.ToRecordBatch(rows);
    var recovered = ArrowMarshaller.FromRecordBatch<AllNullableScalars>(batch).ToList();

    Assert.That(recovered, Has.Count.EqualTo(2));
    Assert.That(recovered[0].IntField, Is.EqualTo(7));
    Assert.That(recovered[0].StringField, Is.EqualTo("yes"));
    Assert.That(recovered[0].BinaryField, Is.EqualTo(new byte[] { 0xFF }));
    Assert.That(recovered[1].IntField, Is.Null);
    Assert.That(recovered[1].StringField, Is.Null);
    Assert.That(recovered[1].BinaryField, Is.Null);
  }

  [Test]
  public void Roundtrip_Empty_Row_Sequence_Produces_Zero_Length_Batch()
  {
    var batch = ArrowMarshaller.ToRecordBatch(System.Array.Empty<SingleIntRow>());
    Assert.That(batch.Length, Is.EqualTo(0));

    var recovered = ArrowMarshaller.FromRecordBatch<SingleIntRow>(batch).ToList();
    Assert.That(recovered, Is.Empty);
  }

  // ──────────────────────────────────────────────────────────────
  // DateTime kind handling
  // ──────────────────────────────────────────────────────────────

  [Test]
  public void Roundtrip_DateTime_Utc_Kind_Preserves_Exact_Instant()
  {
    var utc = new DateTime(2024, 3, 10, 15, 0, 0, DateTimeKind.Utc);
    var batch = ArrowMarshaller.ToRecordBatch(new[] { new SingleDateTimeRow { When = utc } });
    var recovered = ArrowMarshaller.FromRecordBatch<SingleDateTimeRow>(batch).Single();
    Assert.That(recovered.When, Is.EqualTo(utc));
    Assert.That(recovered.When.Kind, Is.EqualTo(DateTimeKind.Utc));
  }

  [Test]
  public void Roundtrip_DateTime_Local_Kind_Converts_To_UTC()
  {
    // Marshaller calls ToUniversalTime() for non-UTC kinds. After
    // round-trip the value is the *UTC instant* of the original local
    // time; the recovered Kind is Utc.
    var local = new DateTime(2024, 3, 10, 15, 0, 0, DateTimeKind.Local);
    var expectedUtc = local.ToUniversalTime();

    var batch = ArrowMarshaller.ToRecordBatch(new[] { new SingleDateTimeRow { When = local } });
    var recovered = ArrowMarshaller.FromRecordBatch<SingleDateTimeRow>(batch).Single();

    Assert.That(recovered.When, Is.EqualTo(expectedUtc));
    Assert.That(recovered.When.Kind, Is.EqualTo(DateTimeKind.Utc));
  }

  [Test]
  public void Roundtrip_DateTime_Unspecified_Kind_Converts_To_UTC()
  {
    // Unspecified is treated as local by ToUniversalTime() — the marshaller
    // documents this as "non-UTC kinds are converted via ToUniversalTime()".
    var unspecified = new DateTime(2024, 3, 10, 15, 0, 0, DateTimeKind.Unspecified);
    var expectedUtc = unspecified.ToUniversalTime();

    var batch = ArrowMarshaller.ToRecordBatch(new[] { new SingleDateTimeRow { When = unspecified } });
    var recovered = ArrowMarshaller.FromRecordBatch<SingleDateTimeRow>(batch).Single();

    Assert.That(recovered.When, Is.EqualTo(expectedUtc));
    Assert.That(recovered.When.Kind, Is.EqualTo(DateTimeKind.Utc));
  }

  [Test]
  public void Roundtrip_DateTimeOffset_NonUtc_Offset_Normalizes_To_UTC()
  {
    var withOffset = new DateTimeOffset(2024, 3, 10, 10, 0, 0, TimeSpan.FromHours(-5));
    var batch = ArrowMarshaller.ToRecordBatch(
      new[] { new SingleDateTimeOffsetRow { When = withOffset } }
    );
    var recovered = ArrowMarshaller.FromRecordBatch<SingleDateTimeOffsetRow>(batch).Single();

    // Same instant, expressed in UTC
    Assert.That(recovered.When.UtcDateTime, Is.EqualTo(withOffset.UtcDateTime));
    Assert.That(recovered.When.Offset, Is.EqualTo(TimeSpan.Zero),
      "Arrow timezone is UTC; the offset round-trips as Zero.");
  }

  // ──────────────────────────────────────────────────────────────
  // Guid round-trip
  // ──────────────────────────────────────────────────────────────

  [Test]
  public void Roundtrip_Guid_Preserves_Exact_Value()
  {
    var guid = Guid.NewGuid();
    var batch = ArrowMarshaller.ToRecordBatch(new[] { new SingleGuidRow { Id = guid } });
    var recovered = ArrowMarshaller.FromRecordBatch<SingleGuidRow>(batch).Single();
    Assert.That(recovered.Id, Is.EqualTo(guid));
  }

  [Test]
  public void ToRecordBatch_Stores_Guid_As_ArrowUuid_Extension_Column()
  {
    // The wire-format contract for Guid is the canonical arrow.uuid
    // extension type (FixedSizeBinary(16) storage). Pin it so a future
    // regression to StringType — or to any other storage — shows up as
    // a test failure here, not as a Python-side parser surprise.
    var guid = Guid.Parse("12345678-1234-1234-1234-123456789abc");
    var batch = ArrowMarshaller.ToRecordBatch(new[] { new SingleGuidRow { Id = guid } });

    Assert.That(batch.Schema.GetFieldByName("Id").DataType, Is.InstanceOf<GuidType>(),
      "Schema field must be the canonical arrow.uuid extension type.");
    var column = batch.Column(0);
    Assert.That(column, Is.InstanceOf<GuidArray>(),
      "Encoded column must be a GuidArray (FixedSizeBinary(16) storage)).");
    Assert.That(((GuidArray)column).GetGuid(0), Is.EqualTo(guid));
  }

  // ──────────────────────────────────────────────────────────────
  // byte[] round-trip
  // ──────────────────────────────────────────────────────────────

  [Test]
  public void Roundtrip_Binary_Empty_Array()
  {
    var batch = ArrowMarshaller.ToRecordBatch(
      new[] { new SingleBinaryRow { Payload = System.Array.Empty<byte>() } }
    );
    var recovered = ArrowMarshaller.FromRecordBatch<SingleBinaryRow>(batch).Single();
    Assert.That(recovered.Payload, Is.Not.Null);
    Assert.That(recovered.Payload, Is.Empty);
  }

  [Test]
  public void Roundtrip_Binary_Multibyte_Array_Preserves_Contents()
  {
    var payload = new byte[] { 0x00, 0x01, 0x02, 0xFE, 0xFF };
    var batch = ArrowMarshaller.ToRecordBatch(new[] { new SingleBinaryRow { Payload = payload } });
    var recovered = ArrowMarshaller.FromRecordBatch<SingleBinaryRow>(batch).Single();
    Assert.That(recovered.Payload, Is.EqualTo(payload));
  }

  // ──────────────────────────────────────────────────────────────
  // IPC envelope round-trip
  // ──────────────────────────────────────────────────────────────

  [Test]
  public void Roundtrip_Via_Ipc_Buffer_Preserves_Batch()
  {
    var rows = new[]
    {
      new SingleIntRow { Value = 10 },
      new SingleIntRow { Value = 20 },
      new SingleIntRow { Value = 30 },
    };

    var original = ArrowMarshaller.ToRecordBatch(rows);
    var buffer = ArrowMarshaller.ToIpcBuffer(original);

    Assert.That(buffer, Is.Not.Null);
    Assert.That(buffer.Length, Is.GreaterThan(0));

    var roundTripped = ArrowMarshaller.FromIpcBuffer(buffer);
    var recovered = ArrowMarshaller.FromRecordBatch<SingleIntRow>(roundTripped).ToList();

    Assert.That(recovered.Select(r => r.Value), Is.EqualTo(new[] { 10, 20, 30 }));
  }

  // ──────────────────────────────────────────────────────────────
  // Enums with [SerializedEnum]
  // ──────────────────────────────────────────────────────────────

  [Test]
  public void Roundtrip_Enum_With_SerializedEnum_Preserves_Value()
  {
    var rows = new[]
    {
      new HappyEnumRow { Id = 1, Color = HappyEnum.Red },
      new HappyEnumRow { Id = 2, Color = HappyEnum.Green },
      new HappyEnumRow { Id = 3, Color = HappyEnum.Blue },
    };

    var batch = ArrowMarshaller.ToRecordBatch(rows);
    var recovered = ArrowMarshaller.FromRecordBatch<HappyEnumRow>(batch).ToList();

    Assert.That(recovered.Select(r => r.Color),
      Is.EqualTo(new[] { HappyEnum.Red, HappyEnum.Green, HappyEnum.Blue }));
  }

  [Test]
  public void ToRecordBatch_Serializes_Enum_Using_SerializedEnum_String()
  {
    // Pin that the on-wire representation is the [SerializedEnum] string,
    // not the C# member name. A future regression to nameof(value) would
    // silently break Python-side comparisons against the contracted strings.
    var batch = ArrowMarshaller.ToRecordBatch(
      new[] { new HappyEnumRow { Id = 1, Color = HappyEnum.Green } }
    );
    var column = (StringArray)batch.Column(1);
    Assert.That(column.GetString(0), Is.EqualTo("G"));
  }

  [Test]
  public void ToRecordBatch_With_Enum_Missing_SerializedEnum_Throws_InvalidOperation()
  {
    // Even a row containing only the *valid* member triggers the failure —
    // GetSerializedEnumMap eagerly iterates every member of the enum type
    // (the missing attribute is a build-time-equivalent contract bug, not
    // a per-value lookup miss).
    var rows = new[] { new MissingAttrEnumRow { Id = 1, Choice = MissingAttrEnum.Alpha } };

    var ex = Assert.Throws<InvalidOperationException>(
      () => ArrowMarshaller.ToRecordBatch(rows)
    );
    Assert.That(ex!.Message, Does.Contain("Beta"),
      "The error must name the offending enum member so the fix is one [SerializedEnum] attribute away.");
  }

  [Test]
  public void FromRecordBatch_Enum_With_Unknown_String_Throws_InvalidOperation()
  {
    // Manually build a batch whose enum column contains a string outside
    // the [SerializedEnum] mapping ("R" maps nowhere on UnknownStringEnum).
    var idArray = BuildInt32(new int?[] { 1 });
    var statusBuilder = new StringArray.Builder();
    statusBuilder.Append("R"); // not in the [SerializedEnum] map
    var statusArray = statusBuilder.Build();

    var schema = new Schema(
      new[]
      {
        new Field("Id", Int32Type.Default, nullable: false),
        new Field("Status", StringType.Default, nullable: true),
      },
      metadata: null
    );
    var batch = new RecordBatch(schema, new IArrowArray[] { idArray, statusArray }, length: 1);

    var ex = Assert.Throws<InvalidOperationException>(
      () => ArrowMarshaller.FromRecordBatch<UnknownStringEnumRow>(batch).ToList()
    );
    Assert.That(ex!.Message, Does.Contain("R"),
      "The error must echo the unrecognized string so the data-quality issue is locatable.");
    Assert.That(ex.Message, Does.Contain(nameof(UnknownStringEnum)),
      "The error must name the enum type whose mapping the value failed against.");
  }

  // ──────────────────────────────────────────────────────────────
  // [SerializedLabel] field naming
  // ──────────────────────────────────────────────────────────────

  [Test]
  public void ToRecordBatch_With_SerializedLabel_Uses_Label_For_Field_Name()
  {
    var batch = ArrowMarshaller.ToRecordBatch(new[] { new LabeledRow { Value = 1 } });

    // Apache.Arrow's Schema.GetFieldIndex throws on miss in v18 rather
    // than returning -1, so we inspect FieldsList directly to assert
    // the absence of "Value" without trapping the throw.
    var fieldNames = batch.Schema.FieldsList.Select(f => f.Name).ToList();
    Assert.That(fieldNames, Contains.Item("custom_name"),
      "[SerializedLabel(\"custom_name\")] must produce the labeled field name.");
    Assert.That(fieldNames, Does.Not.Contain("Value"),
      "The property's C# name must NOT appear when a [SerializedLabel] overrides it.");
  }

  [Test]
  public void Roundtrip_With_SerializedLabel_Preserves_Property_Value()
  {
    var batch = ArrowMarshaller.ToRecordBatch(
      new[] { new LabeledRow { Value = 99 }, new LabeledRow { Value = 100 } }
    );
    var recovered = ArrowMarshaller.FromRecordBatch<LabeledRow>(batch).ToList();
    Assert.That(recovered.Select(r => r.Value), Is.EqualTo(new[] { 99, 100 }));
  }

  // ──────────────────────────────────────────────────────────────
  // Nullable list column
  // ──────────────────────────────────────────────────────────────

  [Test]
  public void Roundtrip_Nullable_List_Property_Preserves_Null_And_Values()
  {
    // ArrowMarshallerListTests covers required (non-null) list columns
    // with empty / non-empty payloads. This pins the *list-itself-null*
    // branch — BuildListArray's `listValue is null` and the symmetric
    // ListArray.IsNull(rowIndex) check on the decode side.
    var rows = new[]
    {
      new NullableListRow { Id = 1, Items = new List<int> { 1, 2, 3 } },
      new NullableListRow { Id = 2, Items = null },
      new NullableListRow { Id = 3, Items = new List<int>() },
    };

    var batch = ArrowMarshaller.ToRecordBatch(rows);
    var recovered = ArrowMarshaller.FromRecordBatch<NullableListRow>(batch).ToList();

    Assert.That(recovered, Has.Count.EqualTo(3));
    Assert.That(recovered[0].Items, Is.EqualTo(new[] { 1, 2, 3 }));
    Assert.That(recovered[1].Items, Is.Null,
      "A null list payload must round-trip as null, not as an empty list — distinguishing "
      + "\"no value supplied\" from \"value is an empty collection\" is part of the schema's "
      + "expressivity contract.");
    Assert.That(recovered[2].Items, Is.Not.Null);
    Assert.That(recovered[2].Items, Is.Empty);
  }

  // ──────────────────────────────────────────────────────────────
  // Numeric coercion (pandas compatibility)
  // ──────────────────────────────────────────────────────────────

  [Test]
  public void FromRecordBatch_Coerces_Int32_Column_To_Long_Property()
  {
    var batch = BuildSingleColumnBatch(
      "Value",
      Int32Type.Default,
      BuildInt32(new int?[] { 5, null, 17 })
    );

    var recovered = ArrowMarshaller.FromRecordBatch<LongValueRow>(batch).ToList();

    Assert.That(recovered.Select(r => r.Value), Is.EqualTo(new long?[] { 5L, null, 17L }));
  }

  [Test]
  public void FromRecordBatch_Coerces_Int32_Column_To_Double_Property()
  {
    var batch = BuildSingleColumnBatch(
      "Value",
      Int32Type.Default,
      BuildInt32(new int?[] { 3, 4 })
    );

    var recovered = ArrowMarshaller.FromRecordBatch<DoubleValueRow>(batch).ToList();

    Assert.That(recovered.Select(r => r.Value), Is.EqualTo(new double?[] { 3.0, 4.0 }));
  }

  [Test]
  public void FromRecordBatch_Coerces_Float_Column_To_Double_Property()
  {
    var batch = BuildSingleColumnBatch(
      "Value",
      FloatType.Default,
      BuildFloat(new float?[] { 1.5f, null, 2.25f })
    );

    var recovered = ArrowMarshaller.FromRecordBatch<DoubleValueRow>(batch).ToList();

    Assert.That(recovered[0].Value, Is.EqualTo(1.5d));
    Assert.That(recovered[1].Value, Is.Null);
    Assert.That(recovered[2].Value, Is.EqualTo(2.25d));
  }

  [Test]
  public void FromRecordBatch_Coerces_Int64_Column_To_Double_Property()
  {
    var batch = BuildSingleColumnBatch(
      "Value",
      Int64Type.Default,
      BuildInt64(new long?[] { 100L, 200L })
    );

    var recovered = ArrowMarshaller.FromRecordBatch<DoubleValueRow>(batch).ToList();

    Assert.That(recovered.Select(r => r.Value), Is.EqualTo(new double?[] { 100.0, 200.0 }));
  }

  // ──────────────────────────────────────────────────────────────
  // Helpers — manual Arrow array construction for coercion tests
  // ──────────────────────────────────────────────────────────────

  private static RecordBatch BuildSingleColumnBatch(
    string fieldName,
    IArrowType arrowType,
    IArrowArray column
  )
  {
    var schema = new Schema(
      new[] { new Field(fieldName, arrowType, nullable: true) },
      metadata: null
    );
    return new RecordBatch(schema, new[] { column }, length: column.Length);
  }

  private static Int32Array BuildInt32(int?[] values)
  {
    var builder = new Int32Array.Builder();
    foreach (var v in values)
    {
      if (v is null) builder.AppendNull();
      else builder.Append(v.Value);
    }
    return builder.Build();
  }

  private static Int64Array BuildInt64(long?[] values)
  {
    var builder = new Int64Array.Builder();
    foreach (var v in values)
    {
      if (v is null) builder.AppendNull();
      else builder.Append(v.Value);
    }
    return builder.Build();
  }

  private static FloatArray BuildFloat(float?[] values)
  {
    var builder = new FloatArray.Builder();
    foreach (var v in values)
    {
      if (v is null) builder.AppendNull();
      else builder.Append(v.Value);
    }
    return builder.Build();
  }

  // ──────────────────────────────────────────────────────────────
  // Decimal128 round-trip
  // ──────────────────────────────────────────────────────────────

  [Test]
  public void Roundtrip_Decimal_With_Default_Precision_Preserves_Midrange_Value()
  {
    var rows = new[] { new DecimalRow { Amount = 1234.5678m } };
    var batch = ArrowMarshaller.ToRecordBatch(rows);

    Assert.That(batch.Schema.GetFieldByName("Amount").DataType, Is.InstanceOf<Decimal128Type>(),
      "Decimal columns must surface as the canonical Decimal128 type.");
    var declared = (Decimal128Type)batch.Schema.GetFieldByName("Amount").DataType;
    Assert.That(declared.Precision, Is.EqualTo(28));
    Assert.That(declared.Scale, Is.EqualTo(9));

    var recovered = ArrowMarshaller.FromRecordBatch<DecimalRow>(batch).Single();
    Assert.That(recovered.Amount, Is.EqualTo(1234.5678m));
  }

  [Test]
  public void Roundtrip_Decimal_With_Default_Precision_Preserves_Negative_Value()
  {
    var rows = new[] { new DecimalRow { Amount = -987.6543m } };
    var batch = ArrowMarshaller.ToRecordBatch(rows);
    var recovered = ArrowMarshaller.FromRecordBatch<DecimalRow>(batch).Single();
    Assert.That(recovered.Amount, Is.EqualTo(-987.6543m));
  }

  [Test]
  public void Roundtrip_Decimal_With_Default_Precision_Preserves_LargeValue_Within_Range()
  {
    // Within (28,9): integer portion up to 19 digits.
    var big = 1234567890123456789m;
    var batch = ArrowMarshaller.ToRecordBatch(new[] { new DecimalRow { Amount = big } });
    var recovered = ArrowMarshaller.FromRecordBatch<DecimalRow>(batch).Single();
    Assert.That(recovered.Amount, Is.EqualTo(big));
  }

  [Test]
  public void Roundtrip_Decimal_With_ArrowDecimal_Attribute_Uses_Declared_PrecisionAndScale()
  {
    var batch = ArrowMarshaller.ToRecordBatch(
      new[] { new ExplicitPrecisionDecimalRow { Amount = 123.4567m } }
    );

    var declared = (Decimal128Type)batch.Schema.GetFieldByName("Amount").DataType;
    Assert.That(declared.Precision, Is.EqualTo(20),
      "[ArrowDecimal(20, 4)] must propagate into the Arrow field type.");
    Assert.That(declared.Scale, Is.EqualTo(4));

    var recovered = ArrowMarshaller.FromRecordBatch<ExplicitPrecisionDecimalRow>(batch).Single();
    Assert.That(recovered.Amount, Is.EqualTo(123.4567m));
  }

  [Test]
  public void Roundtrip_Nullable_Decimal_With_Mixed_Null_And_Value_Preserves_Both()
  {
    var rows = new[]
    {
      new NullableDecimalRow { Amount = 9.99m },
      new NullableDecimalRow { Amount = null },
      new NullableDecimalRow { Amount = -0.01m },
    };

    var batch = ArrowMarshaller.ToRecordBatch(rows);
    var recovered = ArrowMarshaller.FromRecordBatch<NullableDecimalRow>(batch).ToList();

    Assert.That(recovered, Has.Count.EqualTo(3));
    Assert.That(recovered[0].Amount, Is.EqualTo(9.99m));
    Assert.That(recovered[1].Amount, Is.Null);
    Assert.That(recovered[2].Amount, Is.EqualTo(-0.01m));
  }

  [Test]
  public void ToRecordBatch_Decimal_Value_Exceeding_Declared_Precision_Surfaces_Error()
  {
    // 99999.99 fits inside (5,2) precision (5 total digits with 2 after the
    // point). 1234567.89 does NOT — Arrow must reject it rather than
    // silently truncate, otherwise the wire-format contract is meaningless.
    var rows = new[]
    {
      new TightPrecisionDecimalRow { Amount = 1234567.89m },
    };

    Assert.That(
      () => ArrowMarshaller.ToRecordBatch(rows),
      Throws.Exception,
      "A value larger than the declared (5,2) precision must produce an error, not a silently truncated cell."
    );
  }

  // ──────────────────────────────────────────────────────────────
  // Guid (arrow.uuid) extension type
  // ──────────────────────────────────────────────────────────────

  [Test]
  public void Roundtrip_Guid_Via_ArrowUuid_Preserves_Value()
  {
    var guid = Guid.Parse("11111111-2222-3333-4444-555555555555");
    var batch = ArrowMarshaller.ToRecordBatch(new[] { new SingleGuidRow { Id = guid } });
    var recovered = ArrowMarshaller.FromRecordBatch<SingleGuidRow>(batch).Single();

    Assert.That(batch.Schema.GetFieldByName("Id").DataType, Is.InstanceOf<GuidType>());
    Assert.That(recovered.Id, Is.EqualTo(guid));
  }

  [Test]
  public void Roundtrip_Nullable_Guid_With_Mixed_Null_And_Value_Preserves_Both()
  {
    var arbitrary = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
    var rows = new[]
    {
      new AllNullableScalars
      {
        IntField = null, LongField = null, FloatField = null, DoubleField = null,
        BoolField = null, StringField = null, DateTimeField = null,
        DateTimeOffsetField = null, TimeSpanField = null,
        GuidField = Guid.Empty,
        BinaryField = null,
      },
      new AllNullableScalars
      {
        IntField = null, LongField = null, FloatField = null, DoubleField = null,
        BoolField = null, StringField = null, DateTimeField = null,
        DateTimeOffsetField = null, TimeSpanField = null,
        GuidField = null,
        BinaryField = null,
      },
      new AllNullableScalars
      {
        IntField = null, LongField = null, FloatField = null, DoubleField = null,
        BoolField = null, StringField = null, DateTimeField = null,
        DateTimeOffsetField = null, TimeSpanField = null,
        GuidField = arbitrary,
        BinaryField = null,
      },
    };

    var batch = ArrowMarshaller.ToRecordBatch(rows);
    var recovered = ArrowMarshaller.FromRecordBatch<AllNullableScalars>(batch).ToList();

    Assert.That(recovered[0].GuidField, Is.EqualTo(Guid.Empty));
    Assert.That(recovered[1].GuidField, Is.Null);
    Assert.That(recovered[2].GuidField, Is.EqualTo(arbitrary));
  }

  [Test]
  public void ToRecordBatch_Guid_Field_Is_ArrowUuid_Not_StringType()
  {
    // Regression guard: this test fails loudly if the wire encoding
    // silently reverts to StringType (the pre-Phase B representation).
    var batch = ArrowMarshaller.ToRecordBatch(
      new[] { new SingleGuidRow { Id = Guid.NewGuid() } }
    );

    var dataType = batch.Schema.GetFieldByName("Id").DataType;
    Assert.That(dataType, Is.InstanceOf<GuidType>(),
      "Guid columns must travel as the canonical arrow.uuid extension type — never StringType.");
    Assert.That(dataType, Is.Not.InstanceOf<StringType>());
  }
}
