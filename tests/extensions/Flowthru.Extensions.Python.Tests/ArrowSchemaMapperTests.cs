using Apache.Arrow;
using Apache.Arrow.Types;
using Flowthru.Data.Schema;
using Flowthru.Step.Python.Internal;

namespace Flowthru.Extensions.Python.Tests;

/// <summary>
/// Pins <see cref="ArrowSchemaMapper"/>'s CLR-to-Arrow mapping. The mapper is
/// the entire pre-flight contract between a C# schema record and the pandas
/// DataFrame the Python subprocess sees — if these mappings drift silently,
/// the failure surfaces inside the Python worker as a confusing pyarrow
/// type-coercion error, exactly the runtime fog Flowthru tries to avoid.
/// These tests lock the supported types, nullability rules, field-name
/// resolution, and dtype-dict shape so the contract breaks loudly at the
/// C# layer instead.
/// </summary>
[TestFixture]
[Category("Python")]
public class ArrowSchemaMapperTests
{
  // ---------------------------------------------------------------------
  // Probe schemas. One [FlowthruSchema] partial record per coverage need.
  // ---------------------------------------------------------------------

  [FlowthruSchema]
  public partial record AllPrimitivesSchema
  {
    public required int IntCol { get; init; }
    public required long LongCol { get; init; }
    public required float FloatCol { get; init; }
    public required double DoubleCol { get; init; }
    public required bool BoolCol { get; init; }
    public required string StringCol { get; init; }
  }

  [FlowthruSchema]
  public partial record TemporalSchema
  {
    public required DateTime Naive { get; init; }
    public required DateTimeOffset WithOffset { get; init; }
    public required TimeSpan Duration { get; init; }
  }

  [FlowthruSchema]
  public partial record SpecialTypesSchema
  {
    public required Guid Id { get; init; }
    public required byte[] Blob { get; init; }
  }

  public enum Color
  {
    [SerializedEnum("R")]
    Red,

    [SerializedEnum("G")]
    Green,
  }

  [FlowthruSchema]
  public partial record EnumSchema
  {
    public required Color Hue { get; init; }
  }

  [FlowthruSchema]
  public partial record ListSchema
  {
    public required int[] IntArray { get; init; }
    public required List<string> StringList { get; init; }
    public required IReadOnlyList<double> DoubleList { get; init; }
    public required IEnumerable<int> IntEnumerable { get; init; }
  }

  [FlowthruSchema]
  public partial record NestedListSchema
  {
    public required List<List<int>> Matrix { get; init; }
  }

  [FlowthruSchema]
  public partial record StringAndBlobNotListsSchema
  {
    // Both are technically IEnumerable<T> in CLR terms (char and byte
    // respectively); the mapper must short-circuit before the list resolver.
    public required string Text { get; init; }
    public required byte[] Blob { get; init; }
  }

  [FlowthruSchema]
  public partial record NullabilitySchema
  {
    public required int NonNullValue { get; init; }
    public required int? NullableValue { get; init; }
    public required string Reference { get; init; }
    public required byte[] BlobRef { get; init; }
  }

  [FlowthruSchema]
  public partial record DefaultFieldNameSchema
  {
    public required int PropertyName { get; init; }
  }

  [FlowthruSchema]
  public partial record LabeledFieldSchema
  {
    [SerializedLabel("snake_name")]
    public required int PropertyName { get; init; }
  }

  [FlowthruSchema]
  public partial record UnsupportedMapperSchema
  {
    public required int Id { get; init; }
    public required decimal Amount { get; init; }
  }

  [FlowthruSchema]
  public partial record UnsupportedNullableMapperSchema
  {
    public required int Id { get; init; }
    public required decimal? Amount { get; init; }
  }

  [FlowthruSchema]
  public partial record MixedDtypeSchema
  {
    public required int IntCol { get; init; }
    public required long LongCol { get; init; }
    public required float FloatCol { get; init; }
    public required double DoubleCol { get; init; }
    public required bool BoolCol { get; init; }
    public required string StringCol { get; init; }
    public required Guid IdCol { get; init; }
    public required byte[] BlobCol { get; init; }
    public required Color EnumCol { get; init; }
    public required DateTime NaiveTime { get; init; }
    public required DateTimeOffset UtcTime { get; init; }
    public required TimeSpan Span { get; init; }
    public required List<int> Items { get; init; }
  }

  [FlowthruSchema]
  public partial record RenamedColumnSchema
  {
    [SerializedLabel("renamed")]
    public required int X { get; init; }
  }

  // ---------------------------------------------------------------------
  // Primitive type mapping
  // ---------------------------------------------------------------------

  [Test]
  public void BuildArrowSchema_Maps_Int_To_Int32Type()
  {
    var schema = ArrowSchemaMapper.BuildArrowSchema<AllPrimitivesSchema>();
    Assert.That(schema.GetFieldByName("IntCol").DataType, Is.InstanceOf<Int32Type>());
  }

  [Test]
  public void BuildArrowSchema_Maps_Long_To_Int64Type()
  {
    var schema = ArrowSchemaMapper.BuildArrowSchema<AllPrimitivesSchema>();
    Assert.That(schema.GetFieldByName("LongCol").DataType, Is.InstanceOf<Int64Type>());
  }

  [Test]
  public void BuildArrowSchema_Maps_Float_To_FloatType()
  {
    var schema = ArrowSchemaMapper.BuildArrowSchema<AllPrimitivesSchema>();
    Assert.That(schema.GetFieldByName("FloatCol").DataType, Is.InstanceOf<FloatType>());
  }

  [Test]
  public void BuildArrowSchema_Maps_Double_To_DoubleType()
  {
    var schema = ArrowSchemaMapper.BuildArrowSchema<AllPrimitivesSchema>();
    Assert.That(schema.GetFieldByName("DoubleCol").DataType, Is.InstanceOf<DoubleType>());
  }

  [Test]
  public void BuildArrowSchema_Maps_Bool_To_BooleanType()
  {
    var schema = ArrowSchemaMapper.BuildArrowSchema<AllPrimitivesSchema>();
    Assert.That(schema.GetFieldByName("BoolCol").DataType, Is.InstanceOf<BooleanType>());
  }

  [Test]
  public void BuildArrowSchema_Maps_String_To_StringType()
  {
    var schema = ArrowSchemaMapper.BuildArrowSchema<AllPrimitivesSchema>();
    Assert.That(schema.GetFieldByName("StringCol").DataType, Is.InstanceOf<StringType>());
  }

  // ---------------------------------------------------------------------
  // Temporal type mapping
  // ---------------------------------------------------------------------

  [Test]
  public void BuildArrowSchema_Maps_DateTime_To_TimestampType_Microsecond_NoTimezone()
  {
    var schema = ArrowSchemaMapper.BuildArrowSchema<TemporalSchema>();
    var field = schema.GetFieldByName("Naive");
    Assert.That(field.DataType, Is.InstanceOf<TimestampType>());
    var ts = (TimestampType)field.DataType;
    Assert.That(ts.Timezone, Is.Null);
    Assert.That(ts.Unit, Is.EqualTo(TimeUnit.Microsecond));
  }

  [Test]
  public void BuildArrowSchema_Maps_DateTimeOffset_To_TimestampType_With_UTC_Timezone()
  {
    var schema = ArrowSchemaMapper.BuildArrowSchema<TemporalSchema>();
    var field = schema.GetFieldByName("WithOffset");
    Assert.That(field.DataType, Is.InstanceOf<TimestampType>());
    var ts = (TimestampType)field.DataType;
    Assert.That(ts.Timezone, Is.EqualTo("UTC"));
    Assert.That(ts.Unit, Is.EqualTo(TimeUnit.Microsecond));
  }

  [Test]
  public void BuildArrowSchema_Maps_TimeSpan_To_Microsecond_DurationType()
  {
    var schema = ArrowSchemaMapper.BuildArrowSchema<TemporalSchema>();
    var field = schema.GetFieldByName("Duration");
    Assert.That(field.DataType, Is.InstanceOf<DurationType>());
    // The mapper hands back the cached DurationType.Microsecond singleton.
    Assert.That(field.DataType, Is.SameAs(DurationType.Microsecond));
  }

  // ---------------------------------------------------------------------
  // Special types
  // ---------------------------------------------------------------------

  [Test]
  public void BuildArrowSchema_Maps_Guid_To_StringType()
  {
    var schema = ArrowSchemaMapper.BuildArrowSchema<SpecialTypesSchema>();
    Assert.That(schema.GetFieldByName("Id").DataType, Is.InstanceOf<StringType>());
  }

  [Test]
  public void BuildArrowSchema_Maps_ByteArray_To_BinaryType()
  {
    var schema = ArrowSchemaMapper.BuildArrowSchema<SpecialTypesSchema>();
    Assert.That(schema.GetFieldByName("Blob").DataType, Is.InstanceOf<BinaryType>());
  }

  [Test]
  public void BuildArrowSchema_Maps_Enum_To_StringType()
  {
    var schema = ArrowSchemaMapper.BuildArrowSchema<EnumSchema>();
    Assert.That(schema.GetFieldByName("Hue").DataType, Is.InstanceOf<StringType>());
  }

  // ---------------------------------------------------------------------
  // List / collection mapping
  // ---------------------------------------------------------------------

  [Test]
  public void BuildArrowSchema_Maps_IntArray_To_ListType_Of_Int32()
  {
    var schema = ArrowSchemaMapper.BuildArrowSchema<ListSchema>();
    var field = schema.GetFieldByName("IntArray");
    Assert.That(field.DataType, Is.InstanceOf<ListType>());
    var list = (ListType)field.DataType;
    Assert.That(list.ValueDataType, Is.InstanceOf<Int32Type>());
    Assert.That(list.ValueField.Name, Is.EqualTo("item"));
  }

  [Test]
  public void BuildArrowSchema_Maps_ListOfString_To_ListType_Of_String()
  {
    var schema = ArrowSchemaMapper.BuildArrowSchema<ListSchema>();
    var field = schema.GetFieldByName("StringList");
    Assert.That(field.DataType, Is.InstanceOf<ListType>());
    var list = (ListType)field.DataType;
    Assert.That(list.ValueDataType, Is.InstanceOf<StringType>());
    Assert.That(list.ValueField.Name, Is.EqualTo("item"));
  }

  [Test]
  public void BuildArrowSchema_Maps_IReadOnlyListOfDouble_To_ListType_Of_Double()
  {
    var schema = ArrowSchemaMapper.BuildArrowSchema<ListSchema>();
    var field = schema.GetFieldByName("DoubleList");
    Assert.That(field.DataType, Is.InstanceOf<ListType>());
    var list = (ListType)field.DataType;
    Assert.That(list.ValueDataType, Is.InstanceOf<DoubleType>());
    Assert.That(list.ValueField.Name, Is.EqualTo("item"));
  }

  [Test]
  public void BuildArrowSchema_Maps_IEnumerableOfInt_To_ListType_Of_Int32()
  {
    var schema = ArrowSchemaMapper.BuildArrowSchema<ListSchema>();
    var field = schema.GetFieldByName("IntEnumerable");
    Assert.That(field.DataType, Is.InstanceOf<ListType>());
    var list = (ListType)field.DataType;
    Assert.That(list.ValueDataType, Is.InstanceOf<Int32Type>());
    Assert.That(list.ValueField.Name, Is.EqualTo("item"));
  }

  [Test]
  public void BuildArrowSchema_Maps_NestedList_To_ListType_Of_ListType_Of_Int32()
  {
    var schema = ArrowSchemaMapper.BuildArrowSchema<NestedListSchema>();
    var field = schema.GetFieldByName("Matrix");
    Assert.That(field.DataType, Is.InstanceOf<ListType>());
    var outer = (ListType)field.DataType;
    Assert.That(outer.ValueDataType, Is.InstanceOf<ListType>());
    var inner = (ListType)outer.ValueDataType;
    Assert.That(inner.ValueDataType, Is.InstanceOf<Int32Type>());
  }

  [Test]
  public void BuildArrowSchema_Does_Not_Treat_String_As_List_Despite_IEnumerableOfChar()
  {
    var schema = ArrowSchemaMapper.BuildArrowSchema<StringAndBlobNotListsSchema>();
    Assert.That(schema.GetFieldByName("Text").DataType, Is.InstanceOf<StringType>());
  }

  [Test]
  public void BuildArrowSchema_Does_Not_Treat_ByteArray_As_List_Despite_IEnumerableOfByte()
  {
    var schema = ArrowSchemaMapper.BuildArrowSchema<StringAndBlobNotListsSchema>();
    Assert.That(schema.GetFieldByName("Blob").DataType, Is.InstanceOf<BinaryType>());
  }

  // ---------------------------------------------------------------------
  // Nullability
  // ---------------------------------------------------------------------

  [Test]
  public void BuildArrowSchema_NonNullable_ValueType_Property_Is_Not_Nullable()
  {
    var schema = ArrowSchemaMapper.BuildArrowSchema<NullabilitySchema>();
    Assert.That(schema.GetFieldByName("NonNullValue").IsNullable, Is.False);
  }

  [Test]
  public void BuildArrowSchema_Nullable_ValueType_Property_Is_Nullable()
  {
    var schema = ArrowSchemaMapper.BuildArrowSchema<NullabilitySchema>();
    Assert.That(schema.GetFieldByName("NullableValue").IsNullable, Is.True);
  }

  [Test]
  public void BuildArrowSchema_ReferenceType_Property_Is_Nullable()
  {
    var schema = ArrowSchemaMapper.BuildArrowSchema<NullabilitySchema>();
    Assert.That(schema.GetFieldByName("Reference").IsNullable, Is.True);
  }

  [Test]
  public void BuildArrowSchema_ByteArray_Property_Is_Nullable()
  {
    var schema = ArrowSchemaMapper.BuildArrowSchema<NullabilitySchema>();
    Assert.That(schema.GetFieldByName("BlobRef").IsNullable, Is.True);
  }

  // ---------------------------------------------------------------------
  // Field naming (GetFieldName resolution)
  // ---------------------------------------------------------------------

  [Test]
  public void BuildArrowSchema_Without_SerializedLabel_Uses_Property_Name()
  {
    var schema = ArrowSchemaMapper.BuildArrowSchema<DefaultFieldNameSchema>();
    Assert.That(
      schema.FieldsList.Select(f => f.Name).ToArray(),
      Is.EquivalentTo(new[] { "PropertyName" })
    );
  }

  [Test]
  public void BuildArrowSchema_With_SerializedLabel_Uses_Label_Instead_Of_Property_Name()
  {
    var schema = ArrowSchemaMapper.BuildArrowSchema<LabeledFieldSchema>();
    var names = schema.FieldsList.Select(f => f.Name).ToArray();
    Assert.That(names, Does.Contain("snake_name"));
    Assert.That(names, Does.Not.Contain("PropertyName"));
  }

  // ---------------------------------------------------------------------
  // Non-generic BuildArrowSchema(Type) entry point
  // ---------------------------------------------------------------------

  [Test]
  public void BuildArrowSchema_NonGeneric_Produces_Same_Shape_As_Generic()
  {
    var generic = ArrowSchemaMapper.BuildArrowSchema<AllPrimitivesSchema>();
    var nonGeneric = ArrowSchemaMapper.BuildArrowSchema(typeof(AllPrimitivesSchema));

    Assert.That(
      nonGeneric.FieldsList.Select(f => (f.Name, f.DataType.GetType())),
      Is.EqualTo(generic.FieldsList.Select(f => (f.Name, f.DataType.GetType())))
    );
  }

  // ---------------------------------------------------------------------
  // Error paths
  // ---------------------------------------------------------------------

  [Test]
  public void BuildArrowSchema_With_Unsupported_Property_Type_Throws_NotSupported_Naming_Property_And_Type()
  {
    var ex = Assert.Throws<NotSupportedException>(
      () => ArrowSchemaMapper.BuildArrowSchema<UnsupportedMapperSchema>()
    );

    Assert.That(ex!.Message, Does.Contain("Amount"),
      "Diagnostic must name the offending property so developers know where to look.");
    Assert.That(ex.Message, Does.Contain("Decimal"),
      "Diagnostic must name the offending type so the fix is obvious.");
  }

  [Test]
  public void BuildArrowSchema_With_Unsupported_Nullable_Property_Type_Throws_NotSupported()
  {
    var ex = Assert.Throws<NotSupportedException>(
      () => ArrowSchemaMapper.BuildArrowSchema<UnsupportedNullableMapperSchema>()
    );

    Assert.That(ex!.Message, Does.Contain("Amount"));
    Assert.That(ex.Message, Does.Contain("Decimal"));
  }

  // ---------------------------------------------------------------------
  // BuildDtypeSpecDictionary
  // ---------------------------------------------------------------------

  [Test]
  public void BuildDtypeSpecDictionary_Maps_Int_To_Int32_String()
  {
    var spec = ArrowSchemaMapper.BuildDtypeSpecDictionary<MixedDtypeSchema>();
    Assert.That(spec["IntCol"], Is.EqualTo("int32"));
  }

  [Test]
  public void BuildDtypeSpecDictionary_Maps_Long_To_Int64_String()
  {
    var spec = ArrowSchemaMapper.BuildDtypeSpecDictionary<MixedDtypeSchema>();
    Assert.That(spec["LongCol"], Is.EqualTo("int64"));
  }

  [Test]
  public void BuildDtypeSpecDictionary_Maps_Float_And_Double_To_Pandas_Float_Strings()
  {
    var spec = ArrowSchemaMapper.BuildDtypeSpecDictionary<MixedDtypeSchema>();
    Assert.That(spec["FloatCol"], Is.EqualTo("float32"));
    Assert.That(spec["DoubleCol"], Is.EqualTo("float64"));
  }

  [Test]
  public void BuildDtypeSpecDictionary_Maps_Bool_To_Bool_String()
  {
    var spec = ArrowSchemaMapper.BuildDtypeSpecDictionary<MixedDtypeSchema>();
    Assert.That(spec["BoolCol"], Is.EqualTo("bool"));
  }

  [Test]
  public void BuildDtypeSpecDictionary_Maps_String_Guid_Binary_And_Enum_To_Object()
  {
    var spec = ArrowSchemaMapper.BuildDtypeSpecDictionary<MixedDtypeSchema>();
    Assert.That(spec["StringCol"], Is.EqualTo("object"));
    Assert.That(spec["IdCol"], Is.EqualTo("object"));
    Assert.That(spec["BlobCol"], Is.EqualTo("object"));
    Assert.That(spec["EnumCol"], Is.EqualTo("object"));
  }

  [Test]
  public void BuildDtypeSpecDictionary_Maps_DateTime_Without_Timezone_To_Naive_Pandas_Timestamp()
  {
    var spec = ArrowSchemaMapper.BuildDtypeSpecDictionary<MixedDtypeSchema>();
    Assert.That(spec["NaiveTime"], Is.EqualTo("datetime64[ns]"));
  }

  [Test]
  public void BuildDtypeSpecDictionary_Maps_DateTimeOffset_To_UTC_Pandas_Timestamp()
  {
    var spec = ArrowSchemaMapper.BuildDtypeSpecDictionary<MixedDtypeSchema>();
    Assert.That(spec["UtcTime"], Is.EqualTo("datetime64[ns, UTC]"));
  }

  [Test]
  public void BuildDtypeSpecDictionary_Maps_TimeSpan_To_Pandas_Timedelta()
  {
    var spec = ArrowSchemaMapper.BuildDtypeSpecDictionary<MixedDtypeSchema>();
    Assert.That(spec["Span"], Is.EqualTo("timedelta64[ns]"));
  }

  [Test]
  public void BuildDtypeSpecDictionary_Maps_List_Columns_To_Object()
  {
    var spec = ArrowSchemaMapper.BuildDtypeSpecDictionary<MixedDtypeSchema>();
    Assert.That(spec["Items"], Is.EqualTo("object"),
      "Pandas materializes list columns as object-dtype Series of Python lists; the canonical element "
      + "type is already on the Arrow schema field, so dtype coercion would only fight pyarrow.");
  }

  [Test]
  public void BuildDtypeSpecDictionary_Uses_SerializedLabel_As_Dictionary_Key()
  {
    var spec = ArrowSchemaMapper.BuildDtypeSpecDictionary<RenamedColumnSchema>();
    Assert.That(spec.ContainsKey("renamed"), Is.True,
      "[SerializedLabel] must propagate into the dtype dictionary because the Python worker keys "
      + "its DataFrame columns by the external (serialized) name, not the C# property name.");
    Assert.That(spec.ContainsKey("X"), Is.False);
    Assert.That(spec["renamed"], Is.EqualTo("int32"));
  }
}
