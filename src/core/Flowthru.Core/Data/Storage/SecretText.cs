using System;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Flowthru.Data.Storage;

/// <summary>
/// A string whose value must never surface in a log, an error message, a
/// <c>ToString</c>, or a serializer <em>by accident</em>. It is the leaf
/// containment type for credential material carried by a byte-location access
/// handoff (see <see cref="ByteLocation.RemoteUri"/>): because a containing
/// <c>record</c>'s synthesized <c>ToString</c> recurses into this redacted
/// leaf, an enclosing handoff cannot print its secrets by accident.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Containment, not cryptography.</strong> <see cref="SecretText"/>
/// holds no key, algorithm, or IV — it is an API-shape discipline over an
/// enumerable leak surface. Its guarantee holds only against
/// <c>ToString</c>-based renderers (string interpolation,
/// <c>string.Format</c>, the default logger message formatter, the debugger).
/// It does <strong>not</strong> stop a field-walking destructurer (Serilog
/// <c>{@x}</c>), and its serialization refusal (below) is enforced only for
/// System.Text.Json's reflection serializer — Newtonsoft and source-generated
/// contexts are documented boundaries, not covered guarantees. It makes
/// <strong>no</strong> memory-zeroing claim: .NET strings are immutable and
/// GC-copied, so the plaintext lives on the managed heap until collected. See
/// ADR-0026.
/// </para>
/// <para>
/// <strong>Revealing is explicit and greppable.</strong> <see cref="Reveal"/>
/// is the single way to obtain the plaintext, so every intentional
/// materialization is an audit point. Do not pass its result into a log,
/// an interpolation, or an exception message; the plaintext must only reach
/// the boundary that consumes it (e.g. an engine's native client
/// configuration), and that reveal site is responsible for scrubbing its own
/// failures.
/// </para>
/// </remarks>
[DebuggerDisplay("[redacted]")]
[JsonConverter(typeof(SecretTextJsonConverter))]
public sealed class SecretText : IEquatable<SecretText>
{
  /// <summary>The placeholder <see cref="ToString"/> renders in place of the value.</summary>
  public const string Redacted = "[redacted]";

  private readonly string _value;

  /// <summary>
  /// Wrap <paramref name="value"/> as a secret. The value is held as-is;
  /// <see cref="SecretText"/> makes no memory-protection claim (see class
  /// remarks) — its guarantee is that the value does not leak through
  /// <c>ToString</c>, the debugger, or System.Text.Json by accident.
  /// </summary>
  /// <exception cref="ArgumentNullException"><paramref name="value"/> is null.</exception>
  public SecretText(string value)
  {
    _value = value ?? throw new ArgumentNullException(nameof(value));
  }

  /// <summary>
  /// The one explicit, greppable way to obtain the plaintext — every call site
  /// is an audit point. The result must not reach a log, an interpolation, or
  /// an exception message (the design-time <c>Reveal()</c>-position analyzer
  /// flags those argument positions).
  /// </summary>
  public string Reveal() => _value;

  /// <summary>Always the redacted placeholder — never the value.</summary>
  public override string ToString() => Redacted;

  /// <summary>
  /// Ordinal value equality, compared in fixed time so the equality check is
  /// not itself a timing side-channel. Supports the deduplication of identical
  /// access handoffs (two endpoints resolving the same object with the same
  /// credentials) without revealing the value.
  /// </summary>
  public bool Equals(SecretText? other)
  {
    if (other is null)
    {
      return false;
    }
    if (ReferenceEquals(this, other))
    {
      return true;
    }
    var mine = Encoding.UTF8.GetBytes(_value);
    var theirs = Encoding.UTF8.GetBytes(other._value);
    return CryptographicOperations.FixedTimeEquals(mine, theirs);
  }

  /// <inheritdoc/>
  public override bool Equals(object? obj) => Equals(obj as SecretText);

  /// <summary>
  /// Ordinal hash of the value, consistent with <see cref="Equals(SecretText?)"/>
  /// within a process. A hash code is a non-reversible <c>int</c>, so this does
  /// not widen the leak surface.
  /// </summary>
  public override int GetHashCode() => _value.GetHashCode(StringComparison.Ordinal);
}

/// <summary>
/// The System.Text.Json converter for <see cref="SecretText"/>. Both directions
/// throw: serializing a secret would violate ADR-0020's "a secret never enters
/// the catalog or the DAG" invariant, and no legitimate path deserializes one.
/// Failing fast beats a converter that silently emits <c>{}</c> (the shape a
/// property-less type would otherwise serialize to).
/// </summary>
/// <remarks>
/// The refusal is enforced for System.Text.Json's <em>reflection</em>
/// serializer. It does not cover Newtonsoft (which ignores this attribute) or a
/// source-generated <see cref="JsonSerializerContext"/> that may not honor a
/// runtime, type-level converter — those are documented boundaries per
/// ADR-0026, not covered guarantees.
/// </remarks>
public sealed class SecretTextJsonConverter : JsonConverter<SecretText>
{
  /// <inheritdoc/>
  public override SecretText Read(
    ref Utf8JsonReader reader,
    System.Type typeToConvert,
    JsonSerializerOptions options
  ) =>
    throw new JsonException(
      "A SecretText cannot be deserialized: secret material is minted at "
        + "resolution time and never carried through JSON."
    );

  /// <inheritdoc/>
  public override void Write(
    Utf8JsonWriter writer,
    SecretText value,
    JsonSerializerOptions options
  ) =>
    throw new JsonException(
      "A SecretText cannot be serialized: a credential handoff must never enter "
        + "the catalog, the DAG, or a persisted document (ADR-0020, ADR-0026)."
    );
}
