using System.Text.Json;
using Flowthru.Data.Storage;

namespace Flowthru.Core.Tests.Storage;

/// <summary>
/// Structural containment laws for the <see cref="RemoteAccess"/> handoff
/// (ADR-0026): a handoff carrying credentials never renders them through
/// <c>ToString</c>, refuses serialization, and cannot silently drop a secret
/// from its <see cref="RemoteAccess.Secrets"/> scrub-list. These make the
/// guarantee a property of the types, not of case-author discipline — a future
/// medium's case that regresses one of them fails the build.
/// </summary>
/// <remarks>
/// These exercise the <see cref="RemoteAccess"/> types directly rather than
/// through <c>ISupportsByteLocationLaws</c>: the offline S3 probe returns a
/// <see cref="ByteLocation.LocalFile"/>, so the handoff-specific laws have no
/// <see cref="ByteLocation.RemoteUri"/> to assert over there.
/// </remarks>
[TestFixture]
public class RemoteAccessContainmentTests
{
  private const string KeyId = "AKIAIOSFODNN7EXAMPLE";
  private const string SecretKey = "wJalrXUtnFEMI/K7MDENG/bPxRfiCYEXAMPLEKEY";
  private const string SessionToken = "FwoGZXIvYXdzEExampleSessionTokenValue";

  private static ByteLocation.RemoteUri LocationWithCredentials() =>
    new(
      new Uri("s3://bucket/key.parquet"),
      new RemoteAccess.S3Compatible(
        "us-east-1", null, false,
        new S3Credentials(
          new SecretText(KeyId), new SecretText(SecretKey), new SecretText(SessionToken))));

  // ── ToString redaction, by composition through the whole location ───────

  [Test]
  public void Location_ToString_ContainsNoneOfItsSecretValues()
  {
    var rendered = LocationWithCredentials().ToString();

    Assert.Multiple(() =>
    {
      Assert.That(rendered, Does.Not.Contain(KeyId));
      Assert.That(rendered, Does.Not.Contain(SecretKey));
      Assert.That(rendered, Does.Not.Contain(SessionToken));
      Assert.That(rendered, Does.Contain(SecretText.Redacted),
        "A handoff's synthesized ToString must redact its secret leaves.");
      Assert.That(rendered, Does.Contain("s3://bucket/key.parquet"),
        "The non-secret address must still render.");
    });
  }

  // ── Serialization refusal ───────────────────────────────────────────────

  [Test]
  public void Location_WithSecrets_RefusesJsonSerialization()
  {
    Assert.That(() => JsonSerializer.Serialize(LocationWithCredentials()),
      Throws.InstanceOf<JsonException>(),
      "A handoff carrying secrets must refuse serialization — never enter the catalog or DAG.");
  }

  [Test]
  public void Anonymous_HasNoSecrets()
  {
    Assert.That(new RemoteAccess.Anonymous().Secrets, Is.Empty);
  }

  [Test]
  public void Match_DispatchesToTheHandlerForEachCase()
  {
    RemoteAccess anonymous = new RemoteAccess.Anonymous();
    RemoteAccess s3 = new RemoteAccess.S3Compatible("us-east-1", null, false, null);

    Assert.Multiple(() =>
    {
      Assert.That(anonymous.Match(_ => "anon", _ => "s3"), Is.EqualTo("anon"));
      Assert.That(s3.Match(_ => "anon", _ => "s3"), Is.EqualTo("s3"));
    });
  }

  // ── Secrets-enumeration completeness (the reflection law) ───────────────

  [Test]
  public void S3Credentials_Secrets_ContainsEverySecretTextField()
  {
    var credentials = new S3Credentials(
      new SecretText(KeyId), new SecretText(SecretKey), new SecretText(SessionToken));

    var declaredSecretFields = typeof(S3Credentials)
      .GetProperties()
      .Where(p => p.PropertyType == typeof(SecretText))
      .Select(p => (SecretText?)p.GetValue(credentials))
      .Where(v => v is not null)
      .Select(v => v!)
      .ToArray();

    Assert.That(declaredSecretFields, Is.Not.Empty,
      "Guard: the record must actually carry SecretText fields for this law to bite.");
    foreach (var secret in declaredSecretFields)
    {
      Assert.That(credentials.Secrets, Does.Contain(secret),
        "Every SecretText-typed field must be reachable from Secrets — a forgotten "
        + "enumeration would silently escape a reveal site's redaction scrub-list.");
    }
  }

  [Test]
  public void S3Credentials_WithoutSessionToken_EnumeratesTwoSecrets()
  {
    var credentials = new S3Credentials(new SecretText(KeyId), new SecretText(SecretKey), null);
    Assert.That(credentials.Secrets, Has.Count.EqualTo(2));
  }

  [Test]
  public void S3Compatible_Secrets_DelegateToItsCredentials()
  {
    var withCreds = new RemoteAccess.S3Compatible(
      "us-east-1", null, false,
      new S3Credentials(new SecretText(KeyId), new SecretText(SecretKey), null));
    Assert.That(withCreds.Secrets, Has.Count.EqualTo(2));

    var withoutCreds = new RemoteAccess.S3Compatible("us-east-1", null, false, null);
    Assert.That(withoutCreds.Secrets, Is.Empty,
      "An endpoint with no credentials carries no secrets to scrub.");
  }
}
