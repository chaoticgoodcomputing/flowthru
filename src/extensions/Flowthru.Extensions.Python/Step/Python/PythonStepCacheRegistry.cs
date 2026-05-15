using System.Collections.Concurrent;
using System.Runtime.CompilerServices;

namespace Flowthru.Step.Python;

/// <summary>
/// Process-wide registry of Python steps that opted into Flowthru's
/// cache plan via <c>@step(cacheable=True)</c>. The Python source
/// generator emits a <c>[ModuleInitializer]</c> in the consuming
/// project that calls <see cref="Register"/> once for each cacheable
/// step at module load; <see cref="Lookup"/> is consulted from the
/// generated <c>AddPythonStep</c> overloads to decide whether to
/// auto-derive a <c>CodeVersion</c> for the step.
/// </summary>
/// <remarks>
/// <para>
/// This is the Python-extension twin of
/// <c>Flowthru.Step.StepMetadataRegistry</c>. The shape is identical:
/// a process-wide map keyed by step identity, populated at module
/// load, queried at flow-construction time. End users never name
/// this type directly — the generator emits both halves.
/// </para>
/// <para>
/// Registration is idempotent: calling <see cref="Register"/> twice
/// for the same (module, function) key replaces the prior entry. This
/// matters in test scenarios where the same generated initializer
/// might run more than once (e.g., a test harness re-loading
/// assemblies).
/// </para>
/// </remarks>
public static class PythonStepCacheRegistry
{
  /// <summary>
  /// Metadata recorded for one cacheable Python step. The
  /// <see cref="PyFilePath"/> is baked at build time;
  /// <see cref="LockfileCandidates"/> carries every plausible lockfile
  /// location walking up from the .py file's directory, in priority
  /// order. Source generators can't do filesystem IO (RS1035), so
  /// existence checks are deferred to fingerprint time — the runtime
  /// picks the first candidate that exists.
  /// </summary>
  public sealed record Entry(string PyFilePath, IReadOnlyList<string> LockfileCandidates)
  {
    /// <summary>
    /// Return the first candidate path that actually resolves to a file
    /// on disk, or <c>null</c> when no candidate exists. The matching
    /// candidate becomes the manifest dimension folded into the step's
    /// CodeVersion.
    /// </summary>
    public string? ResolveLockfile()
    {
      foreach (var candidate in LockfileCandidates)
      {
        if (!string.IsNullOrWhiteSpace(candidate) && System.IO.File.Exists(candidate))
        {
          return candidate;
        }
      }
      return null;
    }
  }

  private static readonly ConcurrentDictionary<string, Entry> _entries =
    new(StringComparer.Ordinal);

  /// <summary>
  /// Register a cacheable Python step. Typically called from a
  /// <see cref="ModuleInitializerAttribute"/>-decorated helper that
  /// the source generator emits per consuming project.
  /// </summary>
  /// <param name="module">
  /// Dotted Python module path, e.g.,
  /// <c>"Flows.DataScience.Steps.split_data"</c>. Must match the
  /// <c>module</c> argument passed to <c>AddPythonStep</c>.
  /// </param>
  /// <param name="function">
  /// Python function name as it appears in the source file.
  /// </param>
  /// <param name="pyFilePath">
  /// Absolute filesystem path to the <c>.py</c> file containing
  /// <paramref name="function"/>. Captured at build time.
  /// </param>
  /// <param name="lockfileCandidates">
  /// Candidate paths to the project's resolved dependency manifest, in
  /// priority order (<c>uv.lock</c> → <c>poetry.lock</c> →
  /// <c>requirements.txt</c> → <c>Pipfile.lock</c> →
  /// <c>pyproject.toml</c>). The runtime picks the first one that
  /// exists; passing an empty array means "no lockfile dimension" —
  /// the cache still works but invalidation on dependency changes is
  /// silent.
  /// </param>
  public static void Register(
    string module,
    string function,
    string pyFilePath,
    params string[] lockfileCandidates
  )
  {
    if (string.IsNullOrWhiteSpace(module))
      throw new ArgumentException("Module must not be empty.", nameof(module));
    if (string.IsNullOrWhiteSpace(function))
      throw new ArgumentException("Function must not be empty.", nameof(function));
    if (string.IsNullOrWhiteSpace(pyFilePath))
      throw new ArgumentException("PyFilePath must not be empty.", nameof(pyFilePath));

    _entries[Key(module, function)] = new Entry(pyFilePath, lockfileCandidates ?? Array.Empty<string>());
  }

  /// <summary>
  /// Look up the cache entry for a (module, function) pair, or
  /// <c>null</c> when the step was not registered (i.e., the
  /// <c>@step</c> decorator did not declare <c>cacheable=True</c>).
  /// </summary>
  public static Entry? Lookup(string module, string function)
  {
    if (string.IsNullOrWhiteSpace(module)) return null;
    if (string.IsNullOrWhiteSpace(function)) return null;
    return _entries.TryGetValue(Key(module, function), out var e) ? e : null;
  }

  /// <summary>
  /// Test-only seam: clear every recorded entry. The cache registry is
  /// process-wide, so unit tests that exercise registration directly
  /// (rather than through a generated initializer) need a way to reset
  /// state between fixtures.
  /// </summary>
  internal static void ClearForTests() => _entries.Clear();

  private static string Key(string module, string function) =>
    module + "." + function;
}
