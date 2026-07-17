using Amazon.Runtime;
using Flowthru.Data.Storage.S3;

namespace Flowthru.Extensions.AWS.S3.Tests;

/// <summary>
/// The S3 gateway's reveal site (ADR-0026): resolved AWS credentials become
/// contained <c>SecretText</c>, and a resolution failure is contained as a
/// secret-free exception that retains no raw cause. Exercises the mint and the
/// containment contract offline — the gated MinIO/live backends cover the wiring.
/// </summary>
[TestFixture]
public class S3GatewayCredentialContainmentTests
{
  private const string KeyId = "AKIAIOSFODNN7EXAMPLE";
  private const string Secret = "wJalrXUtnFEMI/K7MDENG/bPxRfiCYEXAMPLEKEY";
  private const string Token = "FwoGZXIvExampleSessionTokenValue";

  // ── ToS3Credentials mint: chain plaintext → contained SecretText ────────

  [Test]
  public void ToS3Credentials_WithSessionToken_MintsThreeContainedSecrets()
  {
    var creds = AmazonS3Gateway.ToS3Credentials(new ImmutableCredentials(KeyId, Secret, Token));

    Assert.That(creds, Is.Not.Null);
    Assert.Multiple(() =>
    {
      Assert.That(creds!.KeyId.Reveal(), Is.EqualTo(KeyId));
      Assert.That(creds.SecretKey.Reveal(), Is.EqualTo(Secret));
      Assert.That(creds.SessionToken, Is.Not.Null);
      Assert.That(creds.SessionToken!.Reveal(), Is.EqualTo(Token));
      Assert.That(creds.Secrets, Has.Count.EqualTo(3));
    });
  }

  [Test]
  public void ToS3Credentials_WithoutSessionToken_MintsTwoSecrets()
  {
    var creds = AmazonS3Gateway.ToS3Credentials(new ImmutableCredentials(KeyId, Secret, null));

    Assert.That(creds, Is.Not.Null);
    Assert.Multiple(() =>
    {
      Assert.That(creds!.SessionToken, Is.Null);
      Assert.That(creds.Secrets, Has.Count.EqualTo(2));
    });
  }

  [Test]
  public void ToS3Credentials_MintedValues_DoNotLeakThroughToString()
  {
    var creds = AmazonS3Gateway.ToS3Credentials(new ImmutableCredentials(KeyId, Secret, Token));
    Assert.That(creds!.ToString(), Does.Not.Contain(Secret),
      "The minted credentials must redact by composition through their record ToString.");
  }

  // ── Resolution-failure containment ─────────────────────────────────────

  [Test]
  public void S3CredentialResolutionException_CarriesNoRawCause_AndNoSecret()
  {
    var ex = new S3CredentialResolutionException("my-bucket", "path/key.parquet", "SomeSdkException");

    Assert.Multiple(() =>
    {
      Assert.That(ex.InnerException, Is.Null,
        "Reveal-site containment: the raw SDK exception is dropped, not retained.");
      Assert.That(ex.Message, Does.Contain("my-bucket"));
      Assert.That(ex.Message, Does.Contain("path/key.parquet"));
      Assert.That(ex.Message, Does.Contain("SomeSdkException"),
        "The cause type name is safe to surface for diagnosis.");
      Assert.That(ex.Bucket, Is.EqualTo("my-bucket"));
      Assert.That(ex.Key, Is.EqualTo("path/key.parquet"));
      Assert.That(ex.CauseType, Is.EqualTo("SomeSdkException"));
    });
  }
}
