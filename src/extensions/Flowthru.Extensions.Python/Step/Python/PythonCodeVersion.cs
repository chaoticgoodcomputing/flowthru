using System.Security.Cryptography;
using System.Text;

namespace Flowthru.Step.Python;

/// <summary>
/// Build-time identity for a Python-backed step. Mirrors the core
/// <c>StepMetadataGenerator</c> contract: a SHA-256 prefix that
/// downstream cache-plan logic uses to decide when a step's output can
/// be reused.
/// </summary>
/// <remarks>
/// <para>
/// The Python identity is derived from three sources that together
/// describe a step's reproducible behaviour:
/// </para>
/// <list type="number">
///   <item>The <c>.py</c> source text containing the step function;</item>
///   <item>The interpreter version string (e.g., <c>"Python 3.12.0"</c>);</item>
///   <item>The dependency manifest content (e.g., <c>requirements.txt</c>
///   or <c>pyproject.toml</c>'s lock file).</item>
/// </list>
/// <para>
/// <strong>Interpreter hashing.</strong> Hashing the full Python
/// interpreter binary is slow and brittle (every patch release flips
/// the hash even when no surface contract changed). We deliberately
/// hash only the version <em>string</em>: it captures Python's
/// declared behaviour, scales to giant binaries trivially, and matches
/// what users expect when bumping <c>.python-version</c>.
/// </para>
/// <para>
/// <strong>Fail-safe contract.</strong> Any input that cannot be read
/// — null path, missing file, unreadable bytes — returns null. A null
/// <c>CodeVersion</c> signals "unknown identity" to downstream
/// consumers, which treat it as cache-miss. Silently fabricating an
/// identity in the face of missing inputs would invalidate the very
/// guarantee the value exists to provide.
/// </para>
/// </remarks>
public static class PythonCodeVersion
{
  /// <summary>
  /// Length in hex characters of the SHA-256 prefix returned as the
  /// computed identity. Matches the core generator's choice.
  /// </summary>
  internal const int HexLength = 16;

  /// <summary>
  /// Derive a stable identity from the inputs that constitute a Python
  /// step's behaviour. Returns null if <paramref name="pyPath"/> is
  /// null, empty, or refers to a file that cannot be read — the
  /// downstream "cache-miss" signal.
  /// </summary>
  /// <param name="pyPath">
  /// Filesystem path to the <c>.py</c> file containing the step
  /// function. Hashing the file content makes any logic change
  /// invalidate the identity.
  /// </param>
  /// <param name="interpreterVersion">
  /// The interpreter's <c>--version</c> output (e.g.
  /// <c>"Python 3.12.0"</c>). Hashed verbatim. Null or empty is
  /// treated as the literal empty string.
  /// </param>
  /// <param name="requirementsPath">
  /// Optional path to a dependency manifest
  /// (<c>requirements.txt</c>, <c>uv.lock</c>, etc.). When present the
  /// file's content is folded into the hash; when absent or missing,
  /// the dependency dimension is treated as empty — the .py source
  /// and interpreter version still produce a meaningful identity.
  /// </param>
  /// <returns>
  /// Lowercase hex SHA-256 prefix on success, or null when
  /// <paramref name="pyPath"/> cannot be read.
  /// </returns>
  public static string? Derive(string? pyPath, string? interpreterVersion, string? requirementsPath)
  {
    if (string.IsNullOrWhiteSpace(pyPath)) return null;
    if (!File.Exists(pyPath)) return null;

    byte[] pySource;
    try
    {
      pySource = File.ReadAllBytes(pyPath!);
    }
    catch
    {
      // Filesystem hiccups (permissions, race with another writer) are
      // a runtime concern, not a build-time one — surface null so the
      // cache treats this step as cache-miss rather than asserting a
      // bogus identity.
      return null;
    }

    byte[]? requirements = null;
    if (!string.IsNullOrWhiteSpace(requirementsPath) && File.Exists(requirementsPath))
    {
      try
      {
        requirements = File.ReadAllBytes(requirementsPath!);
      }
      catch
      {
        // Same fail-safe rationale as above — treat the manifest
        // dimension as empty rather than fabricating an identity.
        requirements = null;
      }
    }

    using var sha = SHA256.Create();
    // Fold each input with a length-prefixed delimiter so two distinct
    // input partitions cannot accidentally hash to the same byte
    // stream (e.g. moving content between the .py and requirements
    // files must yield a different digest).
    WriteSection(sha, "py", pySource);
    WriteSection(sha, "interpreter", Encoding.UTF8.GetBytes(interpreterVersion ?? string.Empty));
    WriteSection(sha, "requirements", requirements ?? Array.Empty<byte>());
    sha.TransformFinalBlock(Array.Empty<byte>(), 0, 0);

    var hash = sha.Hash!;
    var sb = new StringBuilder(HexLength);
    for (var i = 0; sb.Length < HexLength && i < hash.Length; i++)
    {
      sb.Append(hash[i].ToString("x2"));
    }
    if (sb.Length > HexLength) sb.Length = HexLength;
    return sb.ToString();
  }

  /// <summary>
  /// Fold a labelled section into the running SHA-256 hash. The label
  /// is included to prevent cross-section collision; the length prefix
  /// avoids ambiguity when two sections meet at a section boundary.
  /// </summary>
  private static void WriteSection(SHA256 sha, string label, byte[] bytes)
  {
    var header = Encoding.UTF8.GetBytes(label + ":" + bytes.Length + "\n");
    sha.TransformBlock(header, 0, header.Length, null, 0);
    if (bytes.Length > 0)
    {
      sha.TransformBlock(bytes, 0, bytes.Length, null, 0);
    }
  }
}
