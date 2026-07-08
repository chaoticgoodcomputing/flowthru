using Flowthru.Data.Storage;

namespace Flowthru.Step.DuckDb.Internal;

/// <summary>
/// Pure planning of DuckDB S3 secrets from <see cref="ByteLocation.RemoteUri"/>
/// access handoffs: one <c>CREATE SECRET</c> per distinct <c>s3://</c> object,
/// each <c>SCOPE</c>d to exactly that object's URI so endpoints carrying
/// different credentials never bleed into each other — DuckDB resolves the
/// secret for a path by longest-matching scope prefix, and an exact-object
/// scope is the most specific prefix possible.
/// </summary>
/// <remarks>
/// <para>
/// <strong>The access vocabulary is the S3 gateway's.</strong> Keys are the
/// ones <c>AmazonS3Gateway.LocateObject</c> mints (<c>region</c>,
/// <c>endpoint</c>, <c>url_style</c>, <c>access_key_id</c>,
/// <c>secret_access_key</c>, <c>session_token</c>), mapped onto DuckDB's
/// <c>CREATE SECRET (TYPE s3, ...)</c> parameters. Unknown keys are ignored —
/// a newer gateway may mint entries an older engine doesn't understand.
/// </para>
/// <para>
/// <strong>Credential material stays in the SQL text only.</strong> The
/// planned SQL is executed against the transform's private in-memory
/// connection (DuckDB secrets are temporary by default — they die with the
/// database instance) and must never be logged or embedded in an error;
/// <see cref="Redact"/> scrubs credential values out of any engine message
/// before it becomes an error detail.
/// </para>
/// </remarks>
internal static class DuckDbS3SecretSql
{
  // The access-handoff vocabulary the S3 gateway documents on LocateObject.
  private const string RegionKey = "region";
  private const string EndpointKey = "endpoint";
  private const string UrlStyleKey = "url_style";
  private const string AccessKeyIdKey = "access_key_id";
  private const string SecretAccessKeyKey = "secret_access_key";
  private const string SessionTokenKey = "session_token";

  /// <summary>Replacement for credential material scrubbed by <see cref="Redact"/>.</summary>
  internal const string RedactedPlaceholder = "[redacted]";

  /// <summary>One planned secret: its unique name and the <c>CREATE SECRET</c> SQL.</summary>
  internal sealed record PlannedSecret(string Name, string Sql);

  /// <summary>
  /// Plan the connection-scoped secrets for a transform's <c>s3://</c>
  /// endpoints: one secret per distinct object URI, scoped to exactly that
  /// URI. Endpoints with an empty access handoff plan no secret (nothing to
  /// configure — DuckDB's defaults apply). The same URI appearing twice with
  /// the same handoff dedupes to one secret; the same URI with a
  /// <em>conflicting</em> handoff throws — two credential sets for one object
  /// is a wiring bug, and silently picking one would be a MagicAtlas-shaped
  /// failure.
  /// </summary>
  public static IReadOnlyList<PlannedSecret> Plan(
    IEnumerable<ByteLocation.RemoteUri> endpoints
  )
  {
    if (endpoints is null) throw new ArgumentNullException(nameof(endpoints));

    var seen = new Dictionary<string, IReadOnlyDictionary<string, string>>(StringComparer.Ordinal);
    var planned = new List<PlannedSecret>();

    foreach (var endpoint in endpoints)
    {
      if (endpoint.Access.Count == 0) continue;

      var scope = ScopeFor(endpoint.Uri);
      if (seen.TryGetValue(scope, out var priorAccess))
      {
        if (!SameAccess(priorAccess, endpoint.Access))
        {
          throw new InvalidOperationException(
            $"Two endpoints locate the same object '{scope}' with different access "
            + "handoffs — one object cannot carry two credential sets in a single "
            + "transform. Check the endpoints' gateway wiring."
          );
        }
        continue;
      }

      seen.Add(scope, endpoint.Access);
      var name = $"flowthru_s3_{planned.Count}";
      planned.Add(new PlannedSecret(name, BuildCreateSecretSql(name, scope, endpoint.Access)));
    }

    return planned;
  }

  /// <summary>
  /// The credential values (key id, secret key, session token) across the
  /// given endpoints — the strings <see cref="Redact"/> must scrub from any
  /// message that could surface as an error detail or log line.
  /// </summary>
  public static IReadOnlyList<string> SensitiveValues(
    IEnumerable<ByteLocation.RemoteUri> endpoints
  )
  {
    if (endpoints is null) throw new ArgumentNullException(nameof(endpoints));

    var values = new HashSet<string>(StringComparer.Ordinal);
    foreach (var endpoint in endpoints)
    {
      foreach (var key in new[] { AccessKeyIdKey, SecretAccessKeyKey, SessionTokenKey })
      {
        if (endpoint.Access.TryGetValue(key, out var value) && !string.IsNullOrEmpty(value))
        {
          values.Add(value);
        }
      }
    }
    return values.ToArray();
  }

  /// <summary>
  /// Scrub every occurrence of the given credential values out of
  /// <paramref name="message"/>. Applied to engine error text before it is
  /// lifted into a typed error — DuckDB parser/binder messages echo the
  /// offending SQL, and a failed <c>CREATE SECRET</c> would otherwise echo
  /// the credentials.
  /// </summary>
  public static string Redact(string message, IReadOnlyList<string> sensitiveValues)
  {
    if (message is null) throw new ArgumentNullException(nameof(message));
    if (sensitiveValues is null) throw new ArgumentNullException(nameof(sensitiveValues));

    var redacted = message;
    foreach (var value in sensitiveValues)
    {
      redacted = redacted.Replace(value, RedactedPlaceholder, StringComparison.Ordinal);
    }
    return redacted;
  }

  /// <summary>
  /// A secret's scope — the endpoint's exact object URI,
  /// <c>s3://bucket/key</c>. DuckDB matches a requested path to the secret
  /// with the longest scope that prefixes it, so the exact URI guarantees
  /// each endpoint reads and writes with its own handoff and nothing else's.
  /// </summary>
  internal static string ScopeFor(Uri uri)
  {
    if (uri is null) throw new ArgumentNullException(nameof(uri));
    return $"s3://{uri.Host}{uri.AbsolutePath}";
  }

  private static string BuildCreateSecretSql(
    string name,
    string scope,
    IReadOnlyDictionary<string, string> access
  )
  {
    // Temporary (in-memory) secret by design: it lives exactly as long as
    // the transform's private database instance and is never persisted.
    var clauses = new List<string>
    {
      "TYPE s3",
      $"SCOPE {QuoteLiteral(scope)}",
    };

    if (access.TryGetValue(AccessKeyIdKey, out var keyId))
    {
      clauses.Add($"KEY_ID {QuoteLiteral(keyId)}");
    }
    if (access.TryGetValue(SecretAccessKeyKey, out var secret))
    {
      clauses.Add($"SECRET {QuoteLiteral(secret)}");
    }
    if (access.TryGetValue(SessionTokenKey, out var token))
    {
      clauses.Add($"SESSION_TOKEN {QuoteLiteral(token)}");
    }
    if (access.TryGetValue(RegionKey, out var region))
    {
      clauses.Add($"REGION {QuoteLiteral(region)}");
    }
    if (access.TryGetValue(EndpointKey, out var endpoint))
    {
      // The gateway mints the endpoint as the client's ServiceURL (scheme +
      // host + optional port); DuckDB wants a bare host[:port] plus USE_SSL.
      var (host, useSsl) = ParseEndpoint(endpoint);
      clauses.Add($"ENDPOINT {QuoteLiteral(host)}");
      if (useSsl is { } ssl)
      {
        clauses.Add($"USE_SSL {(ssl ? "true" : "false")}");
      }
    }
    if (access.TryGetValue(UrlStyleKey, out var urlStyle))
    {
      clauses.Add($"URL_STYLE {QuoteLiteral(urlStyle)}");
    }

    return $"CREATE SECRET \"{name}\" ({string.Join(", ", clauses)})";
  }

  /// <summary>
  /// Split a gateway-minted endpoint into DuckDB's <c>ENDPOINT</c> value
  /// (<c>host[:port]</c>) and <c>USE_SSL</c>. An endpoint that isn't an
  /// absolute http(s) URL passes through untouched with <c>USE_SSL</c>
  /// unset (DuckDB's default, SSL on).
  /// </summary>
  internal static (string Host, bool? UseSsl) ParseEndpoint(string endpoint)
  {
    if (Uri.TryCreate(endpoint, UriKind.Absolute, out var parsed)
        && parsed.Scheme is "http" or "https")
    {
      var host = parsed.IsDefaultPort ? parsed.Host : $"{parsed.Host}:{parsed.Port}";
      return (host, parsed.Scheme == "https");
    }
    return (endpoint, null);
  }

  /// <summary>Quote a string literal, doubling embedded quotes.</summary>
  private static string QuoteLiteral(string value) =>
    $"'{value.Replace("'", "''")}'";

  private static bool SameAccess(
    IReadOnlyDictionary<string, string> a,
    IReadOnlyDictionary<string, string> b
  )
  {
    if (a.Count != b.Count) return false;
    foreach (var (key, value) in a)
    {
      if (!b.TryGetValue(key, out var other) || !string.Equals(value, other, StringComparison.Ordinal))
      {
        return false;
      }
    }
    return true;
  }
}
