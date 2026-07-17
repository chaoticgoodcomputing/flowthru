using System;
using System.Text.Json;
using Flowthru.Data.Storage;

namespace Flowthru.Core.Tests.Storage;

/// <summary>
/// Pins the <see cref="SecretText"/> containment invariants (ADR-0026): the
/// value is reachable only through <see cref="SecretText.Reveal"/>; it never
/// surfaces through <c>ToString</c> (including by composition through an
/// enclosing record); System.Text.Json refuses it in both directions; and
/// equality is ordinal and value-based so identical handoffs deduplicate.
/// </summary>
[TestFixture]
public class SecretTextTests
{
  private const string Value = "wJalrXUtnFEMI/K7MDENG/bPxRfiCYEXAMPLEKEY";

  // A stand-in for an enclosing handoff record: a record's synthesized
  // ToString/PrintMembers must recurse into the redacted leaf.
  private sealed record Holder(string Bucket, SecretText Secret);

  // ─────────────────────────────────────────────────────────────────────────
  // Reveal — the single explicit accessor.
  // ─────────────────────────────────────────────────────────────────────────

  [Test]
  public void Reveal_ReturnsTheWrappedValue()
  {
    var secret = new SecretText(Value);
    Assert.That(secret.Reveal(), Is.EqualTo(Value));
  }

  [Test]
  public void Constructor_NullValue_ThrowsArgumentNullException()
  {
    Assert.That(() => new SecretText(null!), Throws.TypeOf<ArgumentNullException>());
  }

  // ─────────────────────────────────────────────────────────────────────────
  // ToString — never the value, even by composition.
  // ─────────────────────────────────────────────────────────────────────────

  [Test]
  public void ToString_IsTheRedactedPlaceholder_NeverTheValue()
  {
    var secret = new SecretText(Value);
    Assert.That(secret.ToString(), Is.EqualTo(SecretText.Redacted));
    Assert.That(secret.ToString(), Does.Not.Contain(Value));
  }

  [Test]
  public void Interpolation_DoesNotLeakTheValue()
  {
    var secret = new SecretText(Value);
    var rendered = $"{secret}";
    Assert.That(rendered, Does.Not.Contain(Value));
    Assert.That(rendered, Is.EqualTo(SecretText.Redacted));
  }

  [Test]
  public void EnclosingRecordToString_RedactsTheLeafByComposition()
  {
    var holder = new Holder("my-bucket", new SecretText(Value));
    var rendered = holder.ToString();

    Assert.That(rendered, Does.Not.Contain(Value),
      "A containing record's synthesized ToString must recurse into the redacted leaf."
    );
    Assert.That(rendered, Does.Contain(SecretText.Redacted));
    Assert.That(rendered, Does.Contain("my-bucket"),
      "Non-secret members must still render — only the SecretText leaf is redacted."
    );
  }

  // ─────────────────────────────────────────────────────────────────────────
  // Serialization refusal — both directions (System.Text.Json reflection path).
  // ─────────────────────────────────────────────────────────────────────────

  [Test]
  public void Serialize_ThrowsJsonException()
  {
    var secret = new SecretText(Value);
    Assert.That(() => JsonSerializer.Serialize(secret), Throws.InstanceOf<JsonException>());
  }

  [Test]
  public void Serialize_OfEnclosingType_ThrowsJsonException()
  {
    var holder = new Holder("my-bucket", new SecretText(Value));
    Assert.That(() => JsonSerializer.Serialize(holder), Throws.InstanceOf<JsonException>());
  }

  [Test]
  public void SerializeToString_NeverContainsTheValue()
  {
    // Even if a future change swallowed the throw, the value must not appear.
    var secret = new SecretText(Value);
    string? json = null;
    try
    {
      json = JsonSerializer.Serialize(secret);
    }
    catch (JsonException)
    {
      // expected — refusal
    }
    Assert.That(json ?? string.Empty, Does.Not.Contain(Value));
  }

  [Test]
  public void Deserialize_ThrowsJsonException()
  {
    Assert.That(
      () => JsonSerializer.Deserialize<SecretText>("\"" + Value + "\""),
      Throws.InstanceOf<JsonException>()
    );
  }

  // ─────────────────────────────────────────────────────────────────────────
  // Equality — ordinal, value-based, fixed-time.
  // ─────────────────────────────────────────────────────────────────────────

  [Test]
  public void Equals_SameValue_IsTrue()
  {
    Assert.That(new SecretText(Value).Equals(new SecretText(Value)), Is.True);
  }

  [Test]
  public void Equals_DifferentValue_IsFalse()
  {
    Assert.That(new SecretText(Value).Equals(new SecretText("other")), Is.False);
  }

  [Test]
  public void Equals_DiffersByCase_IsFalse()
  {
    Assert.That(new SecretText("abc").Equals(new SecretText("ABC")), Is.False,
      "Equality is ordinal — case-sensitive."
    );
  }

  [Test]
  public void Equals_Null_IsFalse()
  {
    Assert.That(new SecretText(Value).Equals(null), Is.False);
    Assert.That(new SecretText(Value).Equals((object?)null), Is.False);
  }

  [Test]
  public void Equals_SameInstance_IsTrue()
  {
    var secret = new SecretText(Value);
    Assert.That(secret.Equals(secret), Is.True);
  }

  [Test]
  public void GetHashCode_EqualValues_AreEqual()
  {
    var first = new SecretText(Value);
    var second = new SecretText(Value);
    Assert.That(first.GetHashCode(), Is.EqualTo(second.GetHashCode()));
  }
}
