using Apache.Arrow;
using Apache.Arrow.Types;
using Flowthru.Data.Schema;
using Flowthru.Step.Python.Internal;

namespace Flowthru.Extensions.Python.Tests;

/// <summary>
/// Pins the marshaller's handling of list-typed schema properties (the
/// natural shape for ML keyword lists, score vectors, multi-label tag sets).
/// Before 0.17.5 these surfaced as <c>NotSupportedException</c> from
/// <see cref="ArrowMarshaller"/> wrapped in a useless
/// <c>TargetInvocationException</c>; now they round-trip through Arrow's
/// <see cref="ListType"/> the same way the scalar columns do.
/// </summary>
[TestFixture]
[Category("Python")]
public class ArrowMarshallerListTests
{
  [FlowthruSchema]
  public partial record ClusterLabel
  {
    [SerializedLabel("cluster_id")] public required int ClusterId { get; init; }
    [SerializedLabel("label")] public required string Label { get; init; }
    [SerializedLabel("keywords")] public required string[] Keywords { get; init; }
    [SerializedLabel("size")] public required int Size { get; init; }
  }

  [FlowthruSchema]
  public partial record ScoreVectorRow
  {
    public required int Id { get; init; }
    public required List<double> Scores { get; init; }
  }

  [FlowthruSchema]
  public partial record NestedListRow
  {
    public required int Id { get; init; }
    // List<List<int>> — exercises the recursive ListArray builder both
    // on the encode and decode sides.
    public required List<List<int>> Matrix { get; init; }
  }

  [Test]
  public void Schema_With_StringArray_Property_Maps_To_ListType()
  {
    var schema = ArrowSchemaMapper.BuildArrowSchema<ClusterLabel>();
    var keywords = schema.FieldsList.Single(f => f.Name == "keywords");
    Assert.That(keywords.DataType, Is.InstanceOf<ListType>());
    var elem = ((ListType)keywords.DataType).ValueDataType;
    Assert.That(elem, Is.InstanceOf<StringType>());
  }

  [Test]
  public void Roundtrip_StringArray_Property_Preserves_Values_And_Order()
  {
    var rows = new[]
    {
      new ClusterLabel { ClusterId = 11, Label = "draw card hand", Keywords = new[] { "draw", "card", "hand" }, Size = 2253 },
      new ClusterLabel { ClusterId = 12, Label = "land mana ramp", Keywords = new[] { "land", "mana", "ramp" }, Size = 1404 },
    };

    var batch = ArrowMarshaller.ToRecordBatch(rows);
    var recovered = ArrowMarshaller.FromRecordBatch<ClusterLabel>(batch).ToList();

    Assert.That(recovered.Count, Is.EqualTo(2));
    Assert.That(recovered[0].Keywords, Is.EqualTo(new[] { "draw", "card", "hand" }));
    Assert.That(recovered[1].Keywords, Is.EqualTo(new[] { "land", "mana", "ramp" }));
    Assert.That(recovered[0].ClusterId, Is.EqualTo(11));
    Assert.That(recovered[1].Size, Is.EqualTo(1404));
  }

  [Test]
  public void Roundtrip_DoubleList_Property_Preserves_Values_And_Order()
  {
    var rows = new[]
    {
      new ScoreVectorRow { Id = 1, Scores = new List<double> { 0.1, 0.2, 0.3 } },
      new ScoreVectorRow { Id = 2, Scores = new List<double> { 0.9, 0.8 } },
      new ScoreVectorRow { Id = 3, Scores = new List<double>() },
    };

    var batch = ArrowMarshaller.ToRecordBatch(rows);
    var recovered = ArrowMarshaller.FromRecordBatch<ScoreVectorRow>(batch).ToList();

    Assert.That(recovered.Count, Is.EqualTo(3));
    Assert.That(recovered[0].Scores, Is.EqualTo(new[] { 0.1, 0.2, 0.3 }));
    Assert.That(recovered[1].Scores, Is.EqualTo(new[] { 0.9, 0.8 }));
    Assert.That(recovered[2].Scores, Is.Empty);
  }

  [Test]
  public void Roundtrip_NestedList_Property_Preserves_Inner_Structure()
  {
    var rows = new[]
    {
      new NestedListRow { Id = 1, Matrix = new List<List<int>> { new() { 1, 2 }, new() { 3, 4 } } },
      new NestedListRow { Id = 2, Matrix = new List<List<int>> { new() { 5 } } },
    };

    var batch = ArrowMarshaller.ToRecordBatch(rows);
    var recovered = ArrowMarshaller.FromRecordBatch<NestedListRow>(batch).ToList();

    Assert.That(recovered.Count, Is.EqualTo(2));
    Assert.That(recovered[0].Matrix.Count, Is.EqualTo(2));
    Assert.That(recovered[0].Matrix[0], Is.EqualTo(new[] { 1, 2 }));
    Assert.That(recovered[0].Matrix[1], Is.EqualTo(new[] { 3, 4 }));
    Assert.That(recovered[1].Matrix[0], Is.EqualTo(new[] { 5 }));
  }

  [Test]
  public void DtypeSpec_Emits_Object_For_List_Columns()
  {
    var spec = ArrowSchemaMapper.BuildDtypeSpecDictionary<ClusterLabel>();
    Assert.That(spec["keywords"], Is.EqualTo("object"),
      "List columns must map to pandas 'object' dtype — the C# side declared the canonical element type on the Arrow schema field, "
      + "so dtype coercion on the Python side would only fight pyarrow's list-aware Table.from_pandas.");
    Assert.That(spec["cluster_id"], Is.EqualTo("int32"));
  }
}
