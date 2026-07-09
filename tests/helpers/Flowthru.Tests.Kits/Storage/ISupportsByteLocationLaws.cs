using Flowthru.Data.Storage;
using Flowthru.Prelude;

namespace Flowthru.Tests.Kits.Storage;

/// <summary>
/// Shared laws every <see cref="ISupportsByteLocation"/> implementer
/// must satisfy. Subclasses provide a probe whose backing storage
/// already holds bytes and one where nothing has been written yet;
/// inherited tests cover addressability honesty, address stability,
/// and write-target addressability.
/// </summary>
/// <remarks>
/// <para>
/// This kit lives at the contract level — it asserts the behavioural
/// invariants a native-reading consumer (an embedded engine, a bulk
/// copier) relies on, independent of which adapter or medium you plug
/// in.
/// </para>
/// <para>
/// Stability is asserted over the <em>address</em> (the file path or
/// the remote URI), not the whole location: a remote location's access
/// handoff is minted per call and may legitimately differ between
/// calls (e.g. rotated session credentials).
/// </para>
/// </remarks>
public abstract class ISupportsByteLocationLaws
{
  /// <summary>
  /// Build a fresh probe whose backing storage already holds bytes.
  /// Each call should be independent.
  /// </summary>
  protected abstract ISupportsByteLocation CreateProbe();

  /// <summary>
  /// Build a fresh probe whose backing storage holds nothing yet — a
  /// write target before the first write. Each call should be
  /// independent.
  /// </summary>
  protected abstract ISupportsByteLocation CreateAbsentProbe();

  // ── Addressability-honesty law ────────────────────────────────────────

  /// <summary>
  /// An implementer declaring <see cref="ISupportsByteLocation.IsAddressable"/>
  /// resolves a location successfully — the flag and the effect must
  /// agree.
  /// </summary>
  [Test]
  public async Task AddressabilityHonestyLaw()
  {
    var probe = CreateProbe();

    Assert.That(probe.IsAddressable, Is.True,
      "A probe built for these laws must declare itself addressable.");

    var located = await probe.LocateBytes().Run();
    Assert.That(located, Is.InstanceOf<EffResult<ByteLocation>.Success>(),
      "Honesty law: an addressable probe must resolve a location successfully.");
  }

  // ── Address-stability law ─────────────────────────────────────────────

  /// <summary>
  /// Repeated <see cref="ISupportsByteLocation.LocateBytes"/> calls
  /// address the same place. A consumer resolves the location once per
  /// use; a wandering address would point a native reader at different
  /// bytes on each resolve.
  /// </summary>
  [Test]
  public async Task AddressStabilityLaw()
  {
    var probe = CreateProbe();
    var first = await probe.LocateBytes().Run();
    var second = await probe.LocateBytes().Run();

    Assert.That(first, Is.InstanceOf<EffResult<ByteLocation>.Success>(),
      "Well-formed probe should locate successfully on the first call.");
    Assert.That(second, Is.InstanceOf<EffResult<ByteLocation>.Success>(),
      "Repeat locate call against an unchanged probe should also succeed.");
    AssertSameAddress(
      ((EffResult<ByteLocation>.Success)first).Value,
      ((EffResult<ByteLocation>.Success)second).Value
    );
  }

  // ── Write-target law ──────────────────────────────────────────────────

  /// <summary>
  /// A location is where bytes live <em>or would land</em>: locating a
  /// write target before the first write must succeed, so a native
  /// consumer can be pointed at an output as well as an input.
  /// </summary>
  [Test]
  public async Task WriteTargetAddressableLaw()
  {
    var probe = CreateAbsentProbe();
    var located = await probe.LocateBytes().Run();

    Assert.That(located, Is.InstanceOf<EffResult<ByteLocation>.Success>(),
      "Write-target law: absence is Exists()'s question, not LocateBytes()'s — "
      + "an addressable target must locate before anything has been written.");
  }

  // ── Shared assertion ──────────────────────────────────────────────────

  private static void AssertSameAddress(ByteLocation first, ByteLocation second)
  {
    var sameAddress = (first, second) switch
    {
      (ByteLocation.LocalFile a, ByteLocation.LocalFile b) => a.Path == b.Path,
      (ByteLocation.RemoteUri a, ByteLocation.RemoteUri b) => a.Uri == b.Uri,
      _ => false,
    };
    Assert.That(sameAddress, Is.True,
      "Stability law: repeat locate calls must address the same place "
      + $"(first: {first}; second: {second}).");
  }
}
