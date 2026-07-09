using Flowthru.Data.Storage;
using Flowthru.Step.DuckDb.Internal;

namespace Flowthru.Extensions.DuckDB.Tests;

/// <summary>
/// Pins the pure S3-secret planning the engine runs before an
/// <c>s3://</c> transform: how the gateway's access-handoff vocabulary
/// maps onto DuckDB's <c>CREATE SECRET</c> parameters, how secrets are
/// <c>SCOPE</c>d so multi-credential inputs never bleed into each
/// other, and how credential material is scrubbed from engine messages.
/// All offline — no engine, no network.
/// </summary>
[TestFixture]
[Category("DuckDB")]
public class DuckDbS3SecretSqlTests
{
  // ── Access-handoff → CREATE SECRET mapping ──────────────────────────────

  [Test]
  public void Plan_MapsEveryGatewayAccessKey_ToItsDuckDbParameter()
  {
    var endpoint = Remote("s3://bucket/data/in.parquet", new Dictionary<string, string>
    {
      ["region"] = "ap-southeast-2",
      ["endpoint"] = "http://localhost:9000",
      ["url_style"] = "path",
      ["access_key_id"] = "AKIAEXAMPLE",
      ["secret_access_key"] = "shhh-secret",
      ["session_token"] = "sts-token",
    });

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
    var endpoint = Remote("s3://b/k.parquet", new Dictionary<string, string>
    {
      ["endpoint"] = "https://minio.example.com",
      ["access_key_id"] = "id",
      ["secret_access_key"] = "secret",
    });

    var sql = DuckDbS3SecretSql.Plan(new[] { endpoint }).Single().Sql;

    Assert.Multiple(() =>
    {
      Assert.That(sql, Does.Contain("ENDPOINT 'minio.example.com'"));
      Assert.That(sql, Does.Contain("USE_SSL true"));
    });
  }

  [Test]
  public void Plan_WithoutEndpointEntry_OmitsEndpointAndSsl()
  {
    // Plain AWS: the gateway mints no endpoint entry, and DuckDB's
    // default (s3.amazonaws.com, SSL on) is exactly right.
    var endpoint = Remote("s3://b/k.parquet", new Dictionary<string, string>
    {
      ["region"] = "us-east-1",
      ["access_key_id"] = "id",
      ["secret_access_key"] = "secret",
    });

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
    var endpoint = Remote("s3://b/k.parquet", new Dictionary<string, string>
    {
      ["access_key_id"] = "id",
      ["secret_access_key"] = "it's'quoted",
    });

    var sql = DuckDbS3SecretSql.Plan(new[] { endpoint }).Single().Sql;

    Assert.That(sql, Does.Contain("SECRET 'it''s''quoted'"),
      "Single quotes in credential values must be doubled, not truncate the literal.");
  }

  [Test]
  public void Plan_EmptyAccessHandoff_PlansNoSecret()
  {
    var endpoint = Remote("s3://public-bucket/k.parquet", new Dictionary<string, string>());

    Assert.That(DuckDbS3SecretSql.Plan(new[] { endpoint }), Is.Empty,
      "No handoff means nothing to configure — DuckDB's defaults apply (public object).");
  }

  // ── Scoping across multiple endpoints ───────────────────────────────────

  [Test]
  public void Plan_MultiCredentialEndpoints_GetDistinctSecrets_EachScopedToItsOwnObject()
  {
    var first = Remote("s3://bucket-a/in.parquet", new Dictionary<string, string>
    {
      ["access_key_id"] = "key-a",
      ["secret_access_key"] = "secret-a",
    });
    var second = Remote("s3://bucket-b/in.parquet", new Dictionary<string, string>
    {
      ["access_key_id"] = "key-b",
      ["secret_access_key"] = "secret-b",
      ["session_token"] = "token-b",
    });

    var planned = DuckDbS3SecretSql.Plan(new[] { first, second });

    Assert.That(planned, Has.Count.EqualTo(2));
    Assert.Multiple(() =>
    {
      Assert.That(planned.Select(p => p.Name), Is.Unique,
        "DuckDB secret names must be unique within a connection.");
      Assert.That(planned[0].Sql, Does.Contain("SCOPE 's3://bucket-a/in.parquet'"));
      Assert.That(planned[0].Sql, Does.Contain("KEY_ID 'key-a'"));
      Assert.That(planned[0].Sql, Does.Not.Contain("key-b"),
        "Credentials must never leak across endpoint secrets.");
      Assert.That(planned[1].Sql, Does.Contain("SCOPE 's3://bucket-b/in.parquet'"));
      Assert.That(planned[1].Sql, Does.Contain("SESSION_TOKEN 'token-b'"));
      Assert.That(planned[1].Sql, Does.Not.Contain("secret-a"));
    });
  }

  [Test]
  public void Plan_SameObjectTwice_WithSameHandoff_DedupesToOneSecret()
  {
    var access = new Dictionary<string, string>
    {
      ["access_key_id"] = "id",
      ["secret_access_key"] = "secret",
    };

    var planned = DuckDbS3SecretSql.Plan(new[]
    {
      Remote("s3://bucket/k.parquet", access),
      Remote("s3://bucket/k.parquet", access),
    });

    Assert.That(planned, Has.Count.EqualTo(1),
      "Two secrets with the same scope would leave DuckDB's pick ambiguous.");
  }

  [Test]
  public void Plan_SameObject_WithConflictingHandoffs_Throws()
  {
    Assert.That(
      () => DuckDbS3SecretSql.Plan(new[]
      {
        Remote("s3://bucket/k.parquet", new Dictionary<string, string>
        {
          ["access_key_id"] = "id-1", ["secret_access_key"] = "secret-1",
        }),
        Remote("s3://bucket/k.parquet", new Dictionary<string, string>
        {
          ["access_key_id"] = "id-2", ["secret_access_key"] = "secret-2",
        }),
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
      Remote("s3://a/k.parquet", new Dictionary<string, string>
      {
        ["access_key_id"] = "AKIAEXAMPLE",
        ["secret_access_key"] = "shhh-secret",
        ["session_token"] = "sts-token",
        ["region"] = "us-east-1",
      }),
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
      Remote("s3://a/k.parquet", new Dictionary<string, string>
      {
        ["access_key_id"] = "the-key-id",
        ["secret_access_key"] = "the-secret",
        ["session_token"] = "the-token",
        ["region"] = "us-east-1",
        ["endpoint"] = "http://localhost:9000",
        ["url_style"] = "path",
      }),
    };

    var sensitive = DuckDbS3SecretSql.SensitiveValues(endpoints);

    Assert.Multiple(() =>
    {
      Assert.That(sensitive, Is.EquivalentTo(new[] { "the-key-id", "the-secret", "the-token" }),
        "Region/endpoint/url-style are addressing, not secrets — redacting them "
        + "would destroy diagnostic value.");
    });
  }

  // ── Endpoint parsing ────────────────────────────────────────────────────

  [TestCase("http://localhost:9000", "localhost:9000", false)]
  [TestCase("https://s3.us-west-2.amazonaws.com", "s3.us-west-2.amazonaws.com", true)]
  [TestCase("https://minio.internal:9443", "minio.internal:9443", true)]
  public void ParseEndpoint_SplitsSchemeHostAndPort(string raw, string host, bool ssl)
  {
    var (parsedHost, useSsl) = DuckDbS3SecretSql.ParseEndpoint(raw);
    Assert.Multiple(() =>
    {
      Assert.That(parsedHost, Is.EqualTo(host));
      Assert.That(useSsl, Is.EqualTo(ssl));
    });
  }

  [Test]
  public void ParseEndpoint_NonUrlValue_PassesThroughUntouched()
  {
    var (host, useSsl) = DuckDbS3SecretSql.ParseEndpoint("s3.amazonaws.com");
    Assert.Multiple(() =>
    {
      Assert.That(host, Is.EqualTo("s3.amazonaws.com"));
      Assert.That(useSsl, Is.Null, "No scheme → leave DuckDB's SSL default alone.");
    });
  }

  // ── Harness ─────────────────────────────────────────────────────────────

  private static ByteLocation.RemoteUri Remote(
    string uri, IReadOnlyDictionary<string, string> access
  ) =>
    new(new Uri(uri), access);
}
