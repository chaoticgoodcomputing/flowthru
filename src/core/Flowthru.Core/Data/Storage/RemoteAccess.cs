using System;
using System.Collections.Generic;

namespace Flowthru.Data.Storage;

/// <summary>
/// How a native consumer authenticates to reach the bytes behind a
/// <see cref="ByteLocation.RemoteUri"/>. A closed sum owned by Core so a medium
/// and a consumer meet through the type system rather than an extension's
/// private string vocabulary: a wrong field is a design-time compile error, and
/// an unknown case is a typed rejection at the consumer, never a silent skip.
/// </summary>
/// <remarks>
/// <para>
/// The hierarchy is closed via the private constructor — no derived case can be
/// added outside this file. Consume it with <see cref="Match{TResult}"/> (or an
/// exhaustive <c>switch</c>, enforced by <c>FT0001</c>); a case added here
/// surfaces as a compile diagnostic at every consumer until handled.
/// </para>
/// <para>
/// Cases are honestly different — <see cref="S3Compatible"/> is not
/// <c>AzureBlobSas</c>. Core naming an <see cref="S3Compatible"/> case does not
/// leak medium knowledge upward: the S3-compatible access shape is a de-facto
/// protocol (AWS, MinIO, R2, LocalStack), and typing it here moves ownership of
/// the vocabulary from an extension's doc comment into Core's type system. See
/// ADR-0026.
/// </para>
/// <para>
/// <see cref="Secrets"/> is the single scrub-list vocabulary: the credential
/// values a reveal site must contain before any of them can enter an error
/// message. Every <see cref="SecretText"/>-typed field on a case must be
/// reachable from its <see cref="Secrets"/> (a law enforces this), so a case
/// author cannot silently drop one.
/// </para>
/// </remarks>
public abstract record RemoteAccess
{
  private RemoteAccess() { }

  /// <summary>
  /// The credential material this handoff carries — the values a reveal site
  /// must scrub from any error it produces. Empty when the case carries none.
  /// </summary>
  public abstract IReadOnlyList<SecretText> Secrets { get; }

  /// <summary>
  /// The medium hands off nothing; the consumer's own defaults apply. This is
  /// <em>only</em> "no handoff" — it is not overloaded to mean "resolve
  /// credentials yourself." A future consumer-side-resolution opt-in arrives as
  /// a distinct additive case (e.g. <c>DeferToConsumer</c>), never a second
  /// meaning grafted onto this one.
  /// </summary>
  public sealed record Anonymous : RemoteAccess
  {
    /// <inheritdoc/>
    public override IReadOnlyList<SecretText> Secrets => Array.Empty<SecretText>();
  }

  /// <summary>
  /// Access to an S3-compatible object store (AWS S3, MinIO, R2, LocalStack).
  /// The non-secret connection hints are plain; the credentials, when present,
  /// are contained in <see cref="SecretText"/>.
  /// </summary>
  /// <param name="Region">The store's region system name, or null to use the consumer's default.</param>
  /// <param name="Endpoint">A custom endpoint for an S3-compatible store, or null for AWS S3.</param>
  /// <param name="ForcePathStyle">Whether path-style addressing (<c>endpoint/bucket/key</c>) is required.</param>
  /// <param name="Credentials">The access credentials, or null when the endpoint needs none (a public object or a consumer that resolves its own).</param>
  public sealed record S3Compatible(
    string? Region,
    Uri? Endpoint,
    bool ForcePathStyle,
    S3Credentials? Credentials
  ) : RemoteAccess
  {
    /// <inheritdoc/>
    public override IReadOnlyList<SecretText> Secrets =>
      Credentials?.Secrets ?? Array.Empty<SecretText>();

    /// <summary>
    /// Whether the case carries anything a consumer must configure — a region,
    /// a custom endpoint, path-style addressing, or credentials. An
    /// all-default case is equivalent to <see cref="Anonymous"/>: nothing to do.
    /// </summary>
    public bool HasContent =>
      Region is not null || Endpoint is not null || ForcePathStyle || Credentials is not null;
  }

  /// <summary>
  /// Terminal pattern match over the closed sum. Adding a case changes this
  /// signature, so every consumer stops compiling until it handles (or typed-rejects)
  /// the new case.
  /// </summary>
  public TResult Match<TResult>(
    Func<Anonymous, TResult> onAnonymous,
    Func<S3Compatible, TResult> onS3Compatible
  ) =>
    this switch
    {
      Anonymous anonymous => onAnonymous(anonymous),
      S3Compatible s3 => onS3Compatible(s3),
      _ => throw new InvalidOperationException("Unreachable: RemoteAccess is a closed sum"),
    };
}

/// <summary>
/// The credentials for an <see cref="RemoteAccess.S3Compatible"/> handoff. Each
/// value is a <see cref="SecretText"/>, so the enclosing record's synthesized
/// <c>ToString</c> redacts them by composition and serialization refuses them.
/// </summary>
/// <param name="KeyId">The access key id.</param>
/// <param name="SecretKey">The secret access key.</param>
/// <param name="SessionToken">The session token for temporary credentials, or null for long-lived ones.</param>
public sealed record S3Credentials(
  SecretText KeyId,
  SecretText SecretKey,
  SecretText? SessionToken
)
{
  /// <summary>
  /// Every credential value, for a reveal site's scrub-list. The reflection law
  /// asserts this contains every <see cref="SecretText"/>-typed field on the
  /// record, so a value can never silently escape redaction.
  /// </summary>
  public IReadOnlyList<SecretText> Secrets =>
    SessionToken is null
      ? new[] { KeyId, SecretKey }
      : new[] { KeyId, SecretKey, SessionToken };
}
