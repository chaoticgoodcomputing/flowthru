using System.Security.Cryptography;
using System.Text;
using Flowthru.Data.Catalog;
using Flowthru.Data.Storage;
using Flowthru.Prelude;
using Flowthru.Validation.Runtime;
using Microsoft.Extensions.Configuration;

namespace Flowthru.Data.Catalog.Configuration;

/// <summary>
/// A catalog item backed by an <see cref="IConfigurationSection"/>.
/// Phase 5 of the smart-caching-and-slicing RFC re-introduces the
/// pre-0.17 config-as-catalog pattern on the FP foundation: an
/// <see cref="IConfiguration"/> section becomes a typed, fingerprintable
/// input to flows, subject to the same DAG and cache-planning rules as
/// any other catalog item.
/// </summary>
/// <typeparam name="T">
/// Bound payload type. Must be a reference type with a parameterless
/// constructor — <see cref="ConfigurationBinder.Get{T}(IConfiguration)"/>
/// uses property setters to populate the instance.
/// </typeparam>
/// <remarks>
/// <para>
/// <b>Read-only.</b> The item implements
/// <see cref="IReadOnlyItem{T}"/>; the source-gen analyzer
/// <c>FT1102</c> rejects passing a <see cref="ConfigurationItem{T}"/>
/// to a step's <c>outputs:</c> position at build time. Direct
/// invocation of <see cref="Save"/> always fails with a deterministic
/// <see cref="RuntimeError.External"/>.
/// </para>
/// <para>
/// <b>Fingerprint.</b> <see cref="TryGetFingerprint"/> hashes the
/// flattened key/value pairs of the bound section (recursing into
/// child sections) so any value change produces a distinct fingerprint.
/// Phase 6 of the RFC consumes this in the cache plan — a config-value
/// change correctly invalidates the downstream step's cached output.
/// </para>
/// <para>
/// <b>Secrets.</b> The fingerprint is a hash of the values themselves;
/// if your section contains secrets and you log fingerprints, the
/// secret material is implicitly part of telemetry. The recommended
/// mitigation is to keep secret-bearing sections out of fingerprinted
/// catalog items (split your config so the cacheable section never
/// includes raw secrets), or to subclass and return <c>null</c> from
/// <see cref="TryGetFingerprint"/> to opt out of caching for that item.
/// A future RFC may add a redaction-aware fingerprint variant.
/// </para>
/// <para>
/// <b>Reload semantics.</b> v1 binds at FlowthruService construction;
/// host-level <see cref="IConfiguration"/> reloads do not propagate
/// once the flow is running. If your config changes between runs, that
/// change is visible at the next pre-flight pass (and will produce a
/// distinct fingerprint).
/// </para>
/// </remarks>
public sealed class ConfigurationItem<T> : IReadOnlyItem<T>
  where T : class, new()
{
  private readonly IConfigurationSection _section;

  /// <summary>
  /// Construct a <see cref="ConfigurationItem{T}"/> wrapping the given
  /// configuration section.
  /// </summary>
  /// <param name="label">Catalog label for DAG resolution.</param>
  /// <param name="section">The bound configuration section.</param>
  public ConfigurationItem(string label, IConfigurationSection section)
  {
    Label = label ?? throw new ArgumentNullException(nameof(label));
    _section = section ?? throw new ArgumentNullException(nameof(section));
  }

  /// <inheritdoc/>
  public string Label { get; }

  /// <inheritdoc/>
  public NodeTraits Traits => new() { CanInspect = true };

  /// <inheritdoc/>
  public FlowIO<T> Load() =>
    FlowIO.Lift(() => _section.Get<T>() ?? new T(), source: $"ConfigurationItem[{Label}].Load");

  /// <summary>
  /// Always fails — configuration items are read-only. The source-gen
  /// analyzer <c>FT1102</c> catches this at build time when a
  /// <see cref="ConfigurationItem{T}"/> is wired as a step output;
  /// this runtime guard backs the same invariant for any direct
  /// invocation that escapes the build-time check.
  /// </summary>
  public FlowIO<FlowUnit> Save(T data) =>
    FlowIO.Fail<FlowUnit>(new RuntimeError.External(
      $"ConfigurationItem[{Label}].Save",
      new InvalidOperationException(
        $"Configuration item '{Label}' is read-only — flows must not write to "
        + "configuration. ConfigurationItem<T> implements IReadOnlyItem<T>; the "
        + "FT1102 analyzer normally catches this at build time. If you reached "
        + "this error at runtime, the analyzer was suppressed or the item was "
        + "bound to an output position via reflection."
      )
    ));

  /// <inheritdoc/>
  public FlowIO<bool> Exists() =>
    FlowIO.Lift(() => _section.Exists(), source: $"ConfigurationItem[{Label}].Exists");

  /// <inheritdoc/>
  public FlowIO<ValidationResult> InspectShallow(int sampleSize = 100) =>
    FlowIO.Lift(
      () => _section.Exists()
        ? ValidationResult.Success()
        : ValidationResult.Failure(
            Label,
            ValidationErrorType.NotFound,
            $"Configuration section '{_section.Path}' for item '{Label}' is not present in the bound IConfiguration."
          ),
      source: $"ConfigurationItem[{Label}].InspectShallow"
    );

  /// <inheritdoc/>
  public FlowIO<ValidationResult> InspectDeep() => InspectShallow();

  /// <inheritdoc/>
  public FlowIO<ValidationResult> InspectTarget() =>
    // Configuration items are never write targets; report a
    // constraint-flavored failure rather than pretending success.
    FlowIO.Pure(
      ValidationResult.Failure(
        Label,
        ValidationErrorType.WriteAccessDenied,
        $"Configuration item '{Label}' is read-only; it cannot be inspected as a write target."
      )
    );

  /// <inheritdoc/>
  public FlowIO<ValidationResult> Validate() => InspectShallow();

  /// <summary>
  /// Returns a stable hash of the flattened key/value pairs under the
  /// bound section. Two runs against the same configuration produce
  /// identical fingerprints; any value change produces a distinct
  /// fingerprint. Consumed by Phase 6's cache plan.
  /// </summary>
  /// <remarks>
  /// The hash treats keys as case-insensitive (matching
  /// <see cref="IConfiguration"/>'s own comparison policy) and orders
  /// them deterministically. Section paths are emitted relative to the
  /// bound section so renaming the parent of an unchanged section does
  /// not perturb the fingerprint.
  /// </remarks>
  public FlowIO<string>? TryGetFingerprint() =>
    FlowIO.Lift(() => ComputeFingerprint(_section), source: $"ConfigurationItem[{Label}].Fingerprint");

  private static string ComputeFingerprint(IConfigurationSection section)
  {
    using var sha = SHA256.Create();
    var canonicalized = new StringBuilder();
    foreach (var (key, value) in EnumerateLeaves(section))
    {
      canonicalized.Append(key);
      canonicalized.Append('=');
      canonicalized.Append(value ?? string.Empty);
      canonicalized.Append(';');
    }
    var bytes = Encoding.UTF8.GetBytes(canonicalized.ToString());
    var hash = sha.ComputeHash(bytes);
    return Convert.ToHexString(hash).Substring(0, 16);
  }

  /// <summary>
  /// Yield every leaf key/value pair under <paramref name="root"/> in
  /// deterministic order. Leaf detection is "no children" — matches
  /// the shape <see cref="ConfigurationBinder.Get{T}(IConfiguration)"/>
  /// itself iterates over and means every observable input to the
  /// binder participates in the hash.
  /// </summary>
  private static IEnumerable<(string Key, string? Value)> EnumerateLeaves(IConfigurationSection root)
  {
    // Path-relative keys: strip the root prefix so renaming the root
    // doesn't perturb the fingerprint. Sort case-insensitively to
    // match IConfiguration's own key-comparison policy.
    var prefix = root.Path is { Length: > 0 } p ? p + ":" : string.Empty;
    var collected = new List<(string Key, string? Value)>();
    Walk(root, collected);
    return collected
      .Select(kv => (
        Key: kv.Key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
          ? kv.Key.Substring(prefix.Length)
          : kv.Key,
        kv.Value
      ))
      .OrderBy(kv => kv.Key, StringComparer.OrdinalIgnoreCase)
      .ToList();
  }

  private static void Walk(IConfigurationSection section, List<(string Key, string? Value)> accumulator)
  {
    var children = section.GetChildren().ToList();
    if (children.Count == 0)
    {
      accumulator.Add((section.Path, section.Value));
      return;
    }
    foreach (var child in children)
    {
      Walk(child, accumulator);
    }
  }

  // ── IItem (untyped) explicit implementations ──

  Type IItem.DataType => typeof(T);

  FlowIO<object> IItem.LoadUntyped() => Load().Map(value => (object)value!);

  FlowIO<FlowUnit> IItem.SaveUntyped(object data) => Save((T)data);
}
