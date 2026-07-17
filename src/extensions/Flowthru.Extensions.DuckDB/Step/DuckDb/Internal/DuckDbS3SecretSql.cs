using Flowthru.Data.Storage;

namespace Flowthru.Step.DuckDb.Internal;

/// <summary>
/// Pure planning of DuckDB S3 secrets from a byte-location's typed
/// <see cref="RemoteAccess"/> handoff: one <c>CREATE SECRET</c> per distinct
/// <c>s3://</c> object, each <c>SCOPE</c>d to exactly that object's URI so
/// endpoints carrying different credentials never bleed into each other — DuckDB
/// resolves the secret for a path by longest-matching scope prefix, and an
/// exact-object scope is the most specific prefix possible.
/// </summary>
/// <remarks>
/// <para>
/// <strong>The access shape is Core's, not a private string vocabulary.</strong>
/// The consumer reads <see cref="RemoteAccess.S3Compatible"/>'s typed fields and
/// maps them onto DuckDB's <c>CREATE SECRET (TYPE s3, ...)</c> parameters. An
/// <see cref="RemoteAccess.Anonymous"/> handoff — or an all-default
/// <see cref="RemoteAccess.S3Compatible"/> — plans no secret, so DuckDB's
/// defaults apply.
/// </para>
/// <para>
/// <strong>This is a reveal site.</strong> Building the SQL calls
/// <see cref="SecretText.Reveal"/>, so the credentials become plaintext inside
/// the <c>CREATE SECRET</c> text. That text is executed against the transform's
/// private in-memory connection (temporary secrets die with the database
/// instance) and must never be logged or embedded in an error;
/// <see cref="Redact"/> scrubs the credential values — drawn from Core's
/// <see cref="RemoteAccess.Secrets"/> — out of any engine message before it
/// becomes an error detail.
/// </para>
/// </remarks>
internal static class DuckDbS3SecretSql
{
  /// <summary>Replacement for credential material scrubbed by <see cref="Redact"/>.</summary>
  internal const string RedactedPlaceholder = "[redacted]";

  /// <summary>
  /// Credential values shorter than this are not used as scrub needles: a one-
  /// or two-character value would match — and corrupt — unrelated text (a bucket
  /// name, a path segment). A real AWS key never approaches this bound; the guard
  /// only defends against empty or degenerate values.
  /// </summary>
  private const int MinScrubLength = 4;

  /// <summary>One planned secret: its unique name and the <c>CREATE SECRET</c> SQL.</summary>
  internal sealed record PlannedSecret(string Name, string Sql);

  /// <summary>
  /// Plan the connection-scoped secrets for a transform's <c>s3://</c>
  /// endpoints: one secret per distinct object URI, scoped to exactly that URI.
  /// Endpoints whose handoff is <see cref="RemoteAccess.Anonymous"/> (or an
  /// all-default <see cref="RemoteAccess.S3Compatible"/>) plan no secret. The
  /// same URI appearing twice with the same handoff dedupes to one secret; the
  /// same URI with a <em>conflicting</em> handoff throws — two credential sets
  /// for one object is a wiring bug, and silently picking one would be a
  /// MagicAtlas-shaped failure.
  /// </summary>
  public static IReadOnlyList<PlannedSecret> Plan(
    IEnumerable<ByteLocation.RemoteUri> endpoints
  )
  {
    if (endpoints is null) throw new ArgumentNullException(nameof(endpoints));

    var seen = new Dictionary<string, RemoteAccess.S3Compatible>(StringComparer.Ordinal);
    var planned = new List<PlannedSecret>();

    foreach (var endpoint in endpoints)
    {
      if (endpoint.Access is not RemoteAccess.S3Compatible s3 || !s3.HasContent)
      {
        // Anonymous, or an all-default S3Compatible: nothing to configure.
        continue;
      }

      var scope = ScopeFor(endpoint.Uri);
      if (seen.TryGetValue(scope, out var prior))
      {
        // Record equality compares the SecretText fields by value, so two
        // identical handoffs (rotated-but-equal, or the same object listed
        // twice) dedupe rather than conflict.
        if (!prior.Equals(s3))
        {
          throw new InvalidOperationException(
            $"Two endpoints locate the same object '{scope}' with different access "
            + "handoffs — one object cannot carry two credential sets in a single "
            + "transform. Check the endpoints' gateway wiring."
          );
        }
        continue;
      }

      seen.Add(scope, s3);
      var name = $"flowthru_s3_{planned.Count}";
      planned.Add(new PlannedSecret(name, BuildCreateSecretSql(name, scope, s3)));
    }

    return planned;
  }

  /// <summary>
  /// The credential values across the given endpoints — the strings
  /// <see cref="Redact"/> must scrub from any message that could surface as an
  /// error detail or log line. Drawn from Core's per-case
  /// <see cref="RemoteAccess.Secrets"/>, so a consumer never re-declares the
  /// credential vocabulary; values below <see cref="MinScrubLength"/> are
  /// excluded (a degenerate needle would corrupt benign text).
  /// </summary>
  public static IReadOnlyList<string> SensitiveValues(
    IEnumerable<ByteLocation.RemoteUri> endpoints
  )
  {
    if (endpoints is null) throw new ArgumentNullException(nameof(endpoints));

    var values = new HashSet<string>(StringComparer.Ordinal);
    foreach (var endpoint in endpoints)
    {
      foreach (var secret in endpoint.Access.Secrets)
      {
        var value = secret.Reveal();
        if (value.Length >= MinScrubLength)
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
  /// offending SQL, and a failed <c>CREATE SECRET</c> would otherwise echo the
  /// credentials. Longest-first, so a value that is a substring of another is
  /// never left partially exposed by a shorter replacement.
  /// </summary>
  public static string Redact(string message, IReadOnlyList<string> sensitiveValues)
  {
    if (message is null) throw new ArgumentNullException(nameof(message));
    if (sensitiveValues is null) throw new ArgumentNullException(nameof(sensitiveValues));

    var redacted = message;
    foreach (var value in sensitiveValues.OrderByDescending(v => v.Length))
    {
      if (value.Length >= MinScrubLength)
      {
        redacted = redacted.Replace(value, RedactedPlaceholder, StringComparison.Ordinal);
      }
    }
    return redacted;
  }

  /// <summary>
  /// A secret's scope — the endpoint's exact object URI, <c>s3://bucket/key</c>.
  /// DuckDB matches a requested path to the secret with the longest scope that
  /// prefixes it, so the exact URI guarantees each endpoint reads and writes with
  /// its own handoff and nothing else's.
  /// </summary>
  internal static string ScopeFor(Uri uri)
  {
    if (uri is null) throw new ArgumentNullException(nameof(uri));
    return $"s3://{uri.Host}{uri.AbsolutePath}";
  }

  private static string BuildCreateSecretSql(
    string name,
    string scope,
    RemoteAccess.S3Compatible access
  )
  {
    // Temporary (in-memory) secret by design: it lives exactly as long as the
    // transform's private database instance and is never persisted.
    var clauses = new List<string>
    {
      "TYPE s3",
      $"SCOPE {QuoteLiteral(scope)}",
    };

    if (access.Credentials is { } credentials)
    {
      clauses.Add($"KEY_ID {QuoteLiteral(credentials.KeyId.Reveal())}");
      clauses.Add($"SECRET {QuoteLiteral(credentials.SecretKey.Reveal())}");
      if (credentials.SessionToken is { } sessionToken)
      {
        clauses.Add($"SESSION_TOKEN {QuoteLiteral(sessionToken.Reveal())}");
      }
    }
    if (access.Region is { } region)
    {
      clauses.Add($"REGION {QuoteLiteral(region)}");
    }
    if (access.Endpoint is { } endpoint)
    {
      // The gateway mints the endpoint as the client's ServiceURL (scheme + host
      // + optional port); DuckDB wants a bare host[:port] plus USE_SSL.
      var (host, useSsl) = ParseEndpoint(endpoint);
      clauses.Add($"ENDPOINT {QuoteLiteral(host)}");
      if (useSsl is { } ssl)
      {
        clauses.Add($"USE_SSL {(ssl ? "true" : "false")}");
      }
    }
    if (access.ForcePathStyle)
    {
      clauses.Add($"URL_STYLE {QuoteLiteral("path")}");
    }

    return $"CREATE SECRET \"{name}\" ({string.Join(", ", clauses)})";
  }

  /// <summary>
  /// Split a gateway-minted endpoint into DuckDB's <c>ENDPOINT</c> value
  /// (<c>host[:port]</c>) and <c>USE_SSL</c>. An endpoint that isn't an absolute
  /// http(s) URI passes through untouched with <c>USE_SSL</c> unset (DuckDB's
  /// default, SSL on).
  /// </summary>
  internal static (string Host, bool? UseSsl) ParseEndpoint(Uri endpoint)
  {
    if (endpoint.IsAbsoluteUri && endpoint.Scheme is "http" or "https")
    {
      var host = endpoint.IsDefaultPort ? endpoint.Host : $"{endpoint.Host}:{endpoint.Port}";
      return (host, endpoint.Scheme == "https");
    }
    return (endpoint.ToString(), null);
  }

  /// <summary>Quote a string literal, doubling embedded quotes.</summary>
  private static string QuoteLiteral(string value) =>
    $"'{value.Replace("'", "''")}'";
}
