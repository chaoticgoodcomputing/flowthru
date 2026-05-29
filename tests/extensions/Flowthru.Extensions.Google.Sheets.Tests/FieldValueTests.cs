using Flowthru.Data.Storage.Sheets;

namespace Flowthru.Extensions.Google.Sheets.Tests;

/// <summary>
/// Value-semantics of <see cref="FieldValue"/> — the neutral field struct the
/// whole seam speaks. Equality, hashing, and the debug <c>ToString</c> are
/// kind-discriminated, so each kind is exercised: two fields are equal only when
/// kind and payload match, equal fields hash alike, and different kinds never
/// collide on value.
/// </summary>
[TestFixture]
public sealed class FieldValueTests
{
  private static readonly DateTime When = new(2024, 1, 2, 3, 4, 5);

  private static IEnumerable<FieldValue> EachKind() => new[]
  {
    FieldValue.Empty,
    FieldValue.Number(1.5),
    FieldValue.Bool(true),
    FieldValue.Text("hi"),
    FieldValue.Temporal(When, TemporalKind.DateTime),
  };

  [Test]
  public void Equals_SameKindAndPayload_AreEqual_AndHashAlike()
  {
    foreach (var a in EachKind())
    {
      var b = a.Kind switch
      {
        FieldKind.Number => FieldValue.Number(a.NumberValue),
        FieldKind.Bool => FieldValue.Bool(a.BoolValue),
        FieldKind.Text => FieldValue.Text(a.TextValue!),
        FieldKind.Temporal => FieldValue.Temporal(a.TemporalValue, a.TemporalKind),
        _ => FieldValue.Empty,
      };

      Assert.Multiple(() =>
      {
        Assert.That(a.Equals(b), Is.True, $"{a.Kind} should equal an identical field");
        Assert.That(a.Equals((object)b), Is.True, "object overload agrees");
        Assert.That(a.GetHashCode(), Is.EqualTo(b.GetHashCode()), "equal fields hash alike");
      });
    }
  }

  [Test]
  public void Equals_DifferingPayload_AreNotEqual()
  {
    Assert.Multiple(() =>
    {
      Assert.That(FieldValue.Number(1).Equals(FieldValue.Number(2)), Is.False);
      Assert.That(FieldValue.Bool(true).Equals(FieldValue.Bool(false)), Is.False);
      Assert.That(FieldValue.Text("a").Equals(FieldValue.Text("b")), Is.False);
      Assert.That(
        FieldValue.Temporal(When, TemporalKind.Date)
          .Equals(FieldValue.Temporal(When, TemporalKind.Time)),
        Is.False, "the temporal kind is part of identity");
    });
  }

  [Test]
  public void Equals_DifferentKinds_AreNotEqual()
  {
    var kinds = EachKind().ToList();
    for (var i = 0; i < kinds.Count; i++)
    {
      for (var j = i + 1; j < kinds.Count; j++)
      {
        Assert.That(kinds[i].Equals(kinds[j]), Is.False,
          $"{kinds[i].Kind} must not equal {kinds[j].Kind}");
      }
    }
  }

  [Test]
  public void Equals_AgainstNonFieldValue_IsFalse()
  {
    Assert.That(FieldValue.Number(1).Equals("not a field"), Is.False);
  }

  [Test]
  public void ToString_NamesTheKindAndPayload()
  {
    Assert.Multiple(() =>
    {
      Assert.That(FieldValue.Empty.ToString(), Is.EqualTo("Empty"));
      Assert.That(FieldValue.Number(1.5).ToString(), Does.Contain("Number"));
      Assert.That(FieldValue.Bool(true).ToString(), Does.Contain("Bool"));
      Assert.That(FieldValue.Text("hi").ToString(), Does.Contain("Text").And.Contains("hi"));
      Assert.That(
        FieldValue.Temporal(When, TemporalKind.DateTime).ToString(),
        Does.Contain("Temporal").And.Contains("DateTime"));
    });
  }
}
