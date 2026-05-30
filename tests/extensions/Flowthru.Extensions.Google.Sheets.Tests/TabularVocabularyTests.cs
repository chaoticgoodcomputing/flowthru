using System.Text.Json;
using Flowthru.Data.Storage.Sheets;

namespace Flowthru.Extensions.Google.Sheets.Tests;

/// <summary>
/// Tests for the neutral tabular vocabulary itself — field-value equality, JSON
/// round-trip (the offline gateway persists rows as JSON), and schema/data
/// shape.
/// </summary>
[TestFixture]
public sealed class TabularVocabularyTests
{
  [Test]
  public void FieldValue_Equality_DiscriminatesOnKindAndValue()
  {
    var one = FieldValue.Number(1);
    var alsoOne = FieldValue.Number(1);
    var empty = FieldValue.Empty;
    var alsoEmpty = FieldValue.Empty;

    Assert.That(one, Is.EqualTo(alsoOne));
    Assert.That(one, Is.Not.EqualTo(FieldValue.Number(2)));
    Assert.That(one, Is.Not.EqualTo(FieldValue.Text("1")));
    Assert.That(empty, Is.EqualTo(alsoEmpty));
    Assert.That(
      FieldValue.Temporal(new DateTime(2020, 1, 1), TemporalKind.Date),
      Is.Not.EqualTo(FieldValue.Temporal(new DateTime(2020, 1, 1), TemporalKind.DateTime)));
  }

  [Test]
  public void FieldValue_JsonRoundTrips_PreservingKindAndValue()
  {
    FieldValue[] originals =
    {
      FieldValue.Number(3.14),
      FieldValue.Bool(true),
      FieldValue.Text("hi"),
      FieldValue.Temporal(new DateTime(2024, 6, 1, 12, 0, 0), TemporalKind.DateTime),
      FieldValue.Empty,
    };

    foreach (var field in originals)
    {
      var json = JsonSerializer.Serialize(field);
      var back = JsonSerializer.Deserialize<FieldValue>(json);
      Assert.That(back, Is.EqualTo(field), $"round-trip of {field}");
    }
  }

  [Test]
  public void TableSchema_PreservesColumnOrderAndCount()
  {
    var schema = new TableSchema(new[]
    {
      new TableColumn("Name", ColumnType.Text),
      new TableColumn("Amount", ColumnType.Number),
      new TableColumn("When", ColumnType.DateTime),
    });

    Assert.That(schema.ColumnCount, Is.EqualTo(3));
    Assert.That(schema.Columns[0].Name, Is.EqualTo("Name"));
    Assert.That(schema.Columns[1].Type, Is.EqualTo(ColumnType.Number));
    Assert.That(schema.Columns[2].Name, Is.EqualTo("When"));
  }

  [Test]
  public void TableData_Empty_HasSchemaButNoRows()
  {
    var schema = new TableSchema(new[] { new TableColumn("A", ColumnType.Text) });
    var data = TableData.Empty(schema);

    Assert.That(data.RowCount, Is.EqualTo(0));
    Assert.That(data.Schema, Is.SameAs(schema));
  }
}
