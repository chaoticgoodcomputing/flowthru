using Flowthru.Data.Storage;
using Flowthru.Step.DuckDb.Internal;

namespace Flowthru.Extensions.DuckDB.Tests;

/// <summary>
/// Pins the pure S3-secret planning the engine runs before an
/// <c>s3://</c> transform: how the gateway's typed <see cref="RemoteAccess"/>
/// handoff maps onto DuckDB's <c>CREATE SECRET</c> parameters, how secrets are
/// <c>SCOPE</c>d so multi-credential inputs never bleed into each other, and how
/// credential material is scrubbed from engine messages. All offline — no
/// engine, no network.
/// </summary>
[TestFixture]
[Category("DuckDB")]
public class DuckDbS3SecretSqlTests
{
  // ── Access-handoff → CREATE SECRET mapping ──────────────────────────────

  [Test]
  public void Plan_MapsEveryGatewayAccessField_ToItsDuckDbParameter()
  {
    var endpoint = Remote("s3://bucket/data/in.parquet", S3(
      region: "ap-southeast-2",
      endpoint: "http://localhost:9000",
      forcePathStyle: true,
      keyId: "AKIAEXAMPLE",
      secret: "shhh-secret",
      sessionToken: "sts-token"
    ));

    var planned = DuckDbS3SecretSql.Plan(new[] { endpoint }).Single();

    Assert.Multiple(() =>
    {
      Assert.That(planned.Sql, Does.StartWith($"CREATE SECRET \"{planned.Name}\" ("));
      Assert.That(planned.Sql, Does.Contain("TYPE s3"));
      Assert.That(planned.Sql, Does.Contain("SCOPE 's3://bucket/data/in.parquet'"),
        "The scope must be the exact object URI — the most specific prefix DuckDB can match.");
      Assert.That(planned.Sql, Does.Contain("KEY_ID 'AKIAEXAMPLE'"));
      Assert.That(planned.Sql, Does.Contain("SECRET 'shhh-secret'"));
      Assert.That(planned.Sql, Does.Contain("SESSION_TOKEN 'sts-token'"));
      Assert.That(planned.Sql, Does.Contain("REGION 'ap-southeast-2'"));
      Assert.That(planned.Sql, Does.Contain("ENDPOINT 'localhost:9000'"),
        "The gateway's ServiceURL must lose its scheme — DuckDB wants host[:port].");
      Assert.That(planned.Sql, Does.Contain("USE_SSL false"),
        "An http endpoint must disable SSL, or DuckDB dials TLS at a plaintext port.");
      Assert.That(planned.Sql, Does.Contain("URL_STYLE 'path'"));
    });
  }

  [Test]
  public void Plan_HttpsEndpoint_KeepsSslOn()
  {
    var endpoint = Remote("s3://b/k.parquet", S3(
      endpoint: "https://minio.example.com",
      keyId: "key-id",
      secret: "secret-value"
    ));

    var sql = DuckDbS3SecretSql.Plan(new[] { endpoint }).Single().Sql;

    Assert.Multiple(() =>
    {
      Assert.That(sql, Does.Contain("ENDPOINT 'minio.example.com'"));
      Assert.That(sql, Does.Contain("USE_SSL true"));
    });
  }

  [Test]
  public void Plan_WithoutEndpoint_OmitsEndpointAndSsl()
  {
    // Plain AWS: the gateway mints no endpoint, and DuckDB's default
    // (s3.amazonaws.com, SSL on) is exactly right.
    var endpoint = Remote("s3://b/k.parquet", S3(
      region: "us-east-1",
      keyId: "key-id",
      secret: "secret-value"
    ));

    var sql = DuckDbS3SecretSql.Plan(new[] { endpoint }).Single().Sql;

    Assert.Multiple(() =>
    {
      Assert.That(sql, Does.Not.Contain("ENDPOINT"));
      Assert.That(sql, Does.Not.Contain("USE_SSL"));
    });
  }

  [Test]
  public void Plan_EscapesEmbeddedQuotes_InValues()
  {
    var endpoint = Remote("s3://b/k.parquet", S3(
      keyId: "key-id",
      secret: "it's'quoted"
    ));

    var sql = DuckDbS3SecretSql.Plan(new[] { endpoint }).Single().Sql;

    Assert.That(sql, Does.Contain("SECRET 'it''s''quoted'"),
      "Single quotes in credential values must be doubled, not truncate the literal.");
  }

  [Test]
  public void Plan_AnonymousHandoff_PlansNoSecret()
  {
    var endpoint = Remote("s3://public-bucket/k.parquet", new RemoteAccess.Anonymous());

    Assert.That(DuckDbS3SecretSql.Plan(new[] { endpoint }), Is.Empty,
      "No handoff means nothing to configure — DuckDB's defaults apply (public object).");
  }

  [Test]
  public void Plan_AllDefaultS3Compatible_PlansNoSecret()
  {
    // An S3Compatible with nothing set is equivalent to Anonymous.
    var endpoint = Remote("s3://public-bucket/k.parquet", S3());

    Assert.That(DuckDbS3SecretSql.Plan(new[] { endpoint }), Is.Empty,
      "An all-default handoff has nothing to configure.");
  }

  // ── Scoping across multiple endpoints ───────────────────────────────────

  [Test]
  public void Plan_MultiCredentialEndpoints_GetDistinctSecrets_EachScopedToItsOwnObject()
  {
    var first = Remote("s3://bucket-a/in.parquet", S3(keyId: "key-a-id", secret: "secret-a"));
    var second = Remote("s3://bucket-b/in.parquet",
      S3(keyId: "key-b-id", secret: "secret-b", sessionToken: "token-b"));

    var planned = DuckDbS3SecretSql.Plan(new[] { first, second });

    Assert.That(planned, Has.Count.EqualTo(2));
    Assert.Multiple(() =>
    {
      Assert.That(planned.Select(p => p.Name), Is.Unique,
        "DuckDB secret names must be unique within a connection.");
      Assert.That(planned[0].Sql, Does.Contain("SCOPE 's3://bucket-a/in.parquet'"));
      Assert.That(planned[0].Sql, Does.Contain("KEY_ID 'key-a-id'"));
      Assert.That(planned[0].Sql, Does.Not.Contain("key-b-id"),
        "Credentials must never leak across endpoint secrets.");
      Assert.That(planned[1].Sql, Does.Contain("SCOPE 's3://bucket-b/in.parquet'"));
      Assert.That(planned[1].Sql, Does.Contain("SESSION_TOKEN 'token-b'"));
      Assert.That(planned[1].Sql, Does.Not.Contain("secret-a"));
    });
  }

  [Test]
  public void Plan_SameObjectTwice_WithSameHandoff_DedupesToOneSecret()
  {
    var access = S3(keyId: "key-id", secret: "secret-value");

    var planned = DuckDbS3SecretSql.Plan(new[]
    {
      Remote("s3://bucket/k.parquet", access),
      Remote("s3://bucket/k.parquet", access),
    });

    Assert.That(planned, Has.Count.EqualTo(1),
      "Two secrets with the same scope would leave DuckDB's pick ambiguous.");
  }

  [Test]
  public void Plan_SameObject_WithDistinctButEqualHandoffs_DedupesToOneSecret()
  {
    // Two independently-constructed but value-equal handoffs (records + value-
    // equatable SecretText) must dedupe, not conflict.
    var planned = DuckDbS3SecretSql.Plan(new[]
    {
      Remote("s3://bucket/k.parquet", S3(keyId: "key-id", secret: "secret-value")),
      Remote("s3://bucket/k.parquet", S3(keyId: "key-id", secret: "secret-value")),
    });

    Assert.That(planned, Has.Count.EqualTo(1),
      "Value-equal handoffs for the same object are one secret, not a conflict.");
  }

  [Test]
  public void Plan_SameObject_WithConflictingHandoffs_Throws()
  {
    Assert.That(
      () => DuckDbS3SecretSql.Plan(new[]
      {
        Remote("s3://bucket/k.parquet", S3(keyId: "id-one", secret: "secret-one")),
        Remote("s3://bucket/k.parquet", S3(keyId: "id-two", secret: "secret-two")),
      }),
      Throws.InvalidOperationException.With.Message.Contains("s3://bucket/k.parquet"),
      "One object with two credential sets is a wiring bug — silently picking one "
      + "would be the MagicAtlas failure shape."
    );
  }

  // ── Credential redaction ────────────────────────────────────────────────

  [Test]
  public void Redact_ScrubsEveryCredentialValue_FromEngineMessages()
  {
    var endpoints = new[]
    {
      Remote("s3://a/k.parquet",
        S3(region: "us-east-1", keyId: "AKIAEXAMPLE", secret: "shhh-secret", sessionToken: "sts-token")),
    };
    var sensitive = DuckDbS3SecretSql.SensitiveValues(endpoints);

    // The worst case: a DuckDB parser error echoing the CREATE SECRET SQL.
    var message =
      "Parser Error: syntax error near "
      + "\"CREATE SECRET s (TYPE s3, KEY_ID 'AKIAEXAMPLE', SECRET 'shhh-secret', "
      + "SESSION_TOKEN 'sts-token')\"";
    var redacted = DuckDbS3SecretSql.Redact(message, sensitive);

    Assert.Multiple(() =>
    {
      Assert.That(redacted, Does.Not.Contain("AKIAEXAMPLE"));
      Assert.That(redacted, Does.Not.Contain("shhh-secret"));
      Assert.That(redacted, Does.Not.Contain("sts-token"));
      Assert.That(redacted, Does.Contain(DuckDbS3SecretSql.RedactedPlaceholder));
      Assert.That(redacted, Does.Contain("Parser Error"),
        "Redaction must scrub credentials, not the diagnostic content around them.");
    });
  }

  [Test]
  public void SensitiveValues_CoverCredentialEntriesOnly()
  {
    var endpoints = new[]
    {
      Remote("s3://a/k.parquet", S3(
        region: "us-east-1",
        endpoint: "http://localhost:9000",
        forcePathStyle: true,
        keyId: "the-key-id",
        secret: "the-secret",
        sessionToken: "the-token"
      )),
    };

    var sensitive = DuckDbS3SecretSql.SensitiveValues(endpoints);

    Assert.That(sensitive, Is.EquivalentTo(new[] { "the-key-id", "the-secret", "the-token" }),
      "Region/endpoint/url-style are addressing, not secrets — redacting them "
      + "would destroy diagnostic value.");
  }

  [Test]
  public void SensitiveValues_ExcludesDegenerateShortValues()
  {
    // A one- or two-character credential value would corrupt benign text if used
    // as a scrub needle; it is excluded from the scrub-list.
    var endpoints = new[]
    {
      Remote("s3://a/k.parquet", S3(keyId: "ab", secret: "cd")),
    };

    Assert.That(DuckDbS3SecretSql.SensitiveValues(endpoints), Is.Empty,
      "Sub-minimum-length values are not scrub needles.");
  }

  [Test]
  public void Redact_LongestFirst_FullyScrubsOverlappingValues()
  {
    // A short secret that is a prefix of a longer one must not leave the longer
    // one partially exposed. Shortest-first would replace "abcd" inside the
    // longer value and leave "EFGH1234" dangling; longest-first scrubs whole.
    var sensitive = new[] { "abcd", "abcdEFGH1234" };
    var redacted = DuckDbS3SecretSql.Redact("leaked abcdEFGH1234 then abcd", sensitive);

    Assert.Multiple(() =>
    {
      Assert.That(redacted, Does.Not.Contain("abcdEFGH1234"));
      Assert.That(redacted, Does.Not.Contain("EFGH1234"),
        "The longer secret must be scrubbed whole, not left as a fragment.");
    });
  }

  // ── Endpoint parsing ────────────────────────────────────────────────────

  [TestCase("http://localhost:9000", "localhost:9000", false)]
  [TestCase("https://s3.us-west-2.amazonaws.com", "s3.us-west-2.amazonaws.com", true)]
  [TestCase("https://minio.internal:9443", "minio.internal:9443", true)]
  public void ParseEndpoint_SplitsSchemeHostAndPort(string raw, string host, bool ssl)
  {
    var (parsedHost, useSsl) = DuckDbS3SecretSql.ParseEndpoint(new Uri(raw));
    Assert.Multiple(() =>
    {
      Assert.That(parsedHost, Is.EqualTo(host));
      Assert.That(useSsl, Is.EqualTo(ssl));
    });
  }

  [Test]
  public void ParseEndpoint_NonAbsoluteValue_PassesThroughUntouched()
  {
    var (host, useSsl) = DuckDbS3SecretSql.ParseEndpoint(new Uri("s3.amazonaws.com", UriKind.Relative));
    Assert.Multiple(() =>
    {
      Assert.That(host, Is.EqualTo("s3.amazonaws.com"));
      Assert.That(useSsl, Is.Null, "No absolute http(s) scheme → leave DuckDB's SSL default alone.");
    });
  }

  // ── Harness ─────────────────────────────────────────────────────────────

  private static ByteLocation.RemoteUri Remote(string uri, RemoteAccess access) =>
    new(new Uri(uri), access);

  /// <summary>Build an <see cref="RemoteAccess.S3Compatible"/> handoff; credentials are
  /// present only when both <paramref name="keyId"/> and <paramref name="secret"/> are given.</summary>
  private static RemoteAccess.S3Compatible S3(
    string? region = null,
    string? endpoint = null,
    bool forcePathStyle = false,
    string? keyId = null,
    string? secret = null,
    string? sessionToken = null
  ) =>
    new(
      region,
      endpoint is null ? null : new Uri(endpoint),
      forcePathStyle,
      keyId is not null && secret is not null
        ? new S3Credentials(
            new SecretText(keyId),
            new SecretText(secret),
            sessionToken is null ? null : new SecretText(sessionToken))
        : null
    );
}
