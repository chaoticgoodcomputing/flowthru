using Flowthru.Data.Storage;
using Flowthru.Prelude;

namespace Flowthru.Tests.Kits.Storage;

/// <summary>
/// Shared laws every <see cref="ISupportsFingerprint"/> implementer
/// must satisfy. Subclasses provide a probe that yields a fresh
/// fingerprint source plus a mutator that produces an observable
/// change; inherited tests cover stability across repeat calls and
/// sensitivity to medium-level mutation.
/// </summary>
/// <remarks>
/// <para>
/// This kit lives at the contract level — it asserts the
/// behavioural invariants the cache plan relies on, independent of
/// which adapter you plug in.
/// </para>
/// <para>
/// Implementations should leave <see cref="Mutate"/> as a no-op
/// override only when the medium has no externally-observable
/// mutation (the empty case). Most adapters override both methods.
/// </para>
/// </remarks>
public abstract class ISupportsFingerprintLaws
{
  /// <summary>
  /// Build a fresh fingerprint source in a state that produces a
  /// successful <see cref="ISupportsFingerprint.Fingerprint"/>
  /// result. Each call should be independent.
  /// </summary>
  protected abstract ISupportsFingerprint CreateProbe();

  /// <summary>
  /// Apply an externally-observable mutation to the probe returned
  /// by the most recent <see cref="CreateProbe"/> call. After
  /// <c>Mutate</c>, a subsequent call to <c>Fingerprint()</c> on the
  /// same probe must return a different value than before.
  /// </summary>
  protected abstract Task Mutate(ISupportsFingerprint probe);

  // ── Stability law ─────────────────────────────────────────────────────

  /// <summary>
  /// Repeated <see cref="ISupportsFingerprint.Fingerprint"/> calls
  /// without intervening mutation return the same value.
  /// </summary>
  [Test]
  public async Task FingerprintStabilityLaw()
  {
    var probe = CreateProbe();
    var first = await probe.Fingerprint().Run();
    var second = await probe.Fingerprint().Run();

    Assert.That(first, Is.InstanceOf<EffResult<string>.Success>(),
      "Well-formed probe should fingerprint successfully on the first call.");
    Assert.That(second, Is.InstanceOf<EffResult<string>.Success>(),
      "Repeat fingerprint call against an unchanged probe should also succeed.");
    Assert.That(
      ((EffResult<string>.Success)second).Value,
      Is.EqualTo(((EffResult<string>.Success)first).Value),
      "Stability law: repeat calls without intervening state change must return the same value."
    );
  }

  // ── Sensitivity law ───────────────────────────────────────────────────

  /// <summary>
  /// An externally-observable mutation produces a different
  /// fingerprint. The whole point of fingerprinting is to detect
  /// these changes — this is the load-bearing law.
  /// </summary>
  [Test]
  public async Task FingerprintSensitivityLaw()
  {
    var probe = CreateProbe();
    var before = await probe.Fingerprint().Run();

    await Mutate(probe);
    var after = await probe.Fingerprint().Run();

    Assert.That(before, Is.InstanceOf<EffResult<string>.Success>());
    Assert.That(after, Is.InstanceOf<EffResult<string>.Success>());
    Assert.That(
      ((EffResult<string>.Success)after).Value,
      Is.Not.EqualTo(((EffResult<string>.Success)before).Value),
      "Sensitivity law: any change to the medium's content must change the fingerprint."
    );
  }

  // ── Encoding law ──────────────────────────────────────────────────────

  /// <summary>
  /// Successful fingerprints decode as 64-char lowercase hex (SHA-256
  /// digest). Pinning the encoding so cache-manifest readers can
  /// rely on a stable wire format.
  /// </summary>
  [Test]
  public async Task FingerprintEncodingLaw()
  {
    var probe = CreateProbe();
    var result = await probe.Fingerprint().Run();
    Assert.That(result, Is.InstanceOf<EffResult<string>.Success>());
    var value = ((EffResult<string>.Success)result).Value;
    Assert.That(value, Has.Length.EqualTo(64),
      "Fingerprint values are SHA-256 hex digests (64 lowercase hex characters).");
    Assert.That(value, Does.Match("^[0-9a-f]{64}$"));
  }
}
