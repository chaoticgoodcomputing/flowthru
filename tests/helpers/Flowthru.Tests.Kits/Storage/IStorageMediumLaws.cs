using Flowthru.Data.Storage;
using Flowthru.Prelude;

namespace Flowthru.Tests.Kits.Storage;

/// <summary>
/// Laws every <see cref="IStorageMedium"/> implementer must satisfy.
/// Subclasses produce a fresh medium per test (with whatever isolation
/// they need — temp directory, in-memory backend, etc.) and inherit
/// tests covering existence semantics, write-then-read round-trip, and
/// target inspection.
/// </summary>
public abstract class IStorageMediumLaws
{
  /// <summary>Build a fresh medium for one test case. Each call should be independent.</summary>
  protected abstract IStorageMedium CreateMedium();

  // ── Existence laws ─────────────────────────────────────────────────────

  /// <summary>Fresh medium with no data yet returns <c>false</c> from <see cref="IStorageMedium.Exists"/>.</summary>
  [Test]
  public async Task ExistsBeforeAnyWriteLaw()
  {
    var medium = CreateMedium();
    var result = await medium.Exists().Run();
    Assert.That(result, Is.InstanceOf<EffResult<bool>.Success>());
    Assert.That(((EffResult<bool>.Success)result).Value, Is.False,
      "A fresh medium with no prior write should report Exists() = false.");
  }

  /// <summary>After <see cref="IStorageMedium.WriteStream"/>, <see cref="IStorageMedium.Exists"/> returns <c>true</c>.</summary>
  [Test]
  public async Task ExistsAfterWriteLaw()
  {
    var medium = CreateMedium();
    var payload = new byte[] { 1, 2, 3, 4 };
    using var input = new MemoryStream(payload);

    var writeResult = await medium.WriteStream(input).Run();
    Assert.That(writeResult, Is.InstanceOf<EffResult<FlowUnit>.Success>(),
      "WriteStream should succeed for a writable medium.");

    var existsResult = await medium.Exists().Run();
    Assert.That(existsResult, Is.InstanceOf<EffResult<bool>.Success>());
    Assert.That(((EffResult<bool>.Success)existsResult).Value, Is.True,
      "After WriteStream succeeds, Exists() should return true.");
  }

  // ── Round-trip law ─────────────────────────────────────────────────────

  /// <summary>Bytes written via <see cref="IStorageMedium.WriteStream"/> are returned by <see cref="IStorageMedium.ReadStream"/>.</summary>
  [Test]
  public async Task WriteReadRoundTripLaw()
  {
    var medium = CreateMedium();
    var payload = new byte[] { 0x46, 0x6c, 0x6f, 0x77, 0x74, 0x68, 0x72, 0x75 }; // "Flowthru"
    using var input = new MemoryStream(payload);

    var writeResult = await medium.WriteStream(input).Run();
    Assert.That(writeResult, Is.InstanceOf<EffResult<FlowUnit>.Success>());

    var readResult = await medium.ReadStream().Run();
    Assert.That(readResult, Is.InstanceOf<EffResult<Stream>.Success>());

    var readStream = ((EffResult<Stream>.Success)readResult).Value;
    using (readStream)
    {
      using var ms = new MemoryStream();
      await readStream.CopyToAsync(ms);
      var roundTripped = ms.ToArray();
      Assert.That(roundTripped, Is.EqualTo(payload),
        "Bytes read should equal bytes written.");
    }
  }

  // ── Inspect-target law ─────────────────────────────────────────────────

  /// <summary>
  /// Default <see cref="IStorageMedium.InspectTarget"/> should produce a
  /// successful <see cref="ValidationResult"/> for a writable medium —
  /// either via the default-interface implementation (always-success) or
  /// via the medium's own probe.
  /// </summary>
  [Test]
  public async Task InspectTargetReturnsValidForWritableMediumLaw()
  {
    var medium = CreateMedium();
    var result = await medium.InspectTarget().Run();
    Assert.That(result, Is.InstanceOf<EffResult<ValidationResult>.Success>());
    var validation = ((EffResult<ValidationResult>.Success)result).Value;
    Assert.That(validation.IsValid, Is.True,
      $"InspectTarget should succeed for a writable medium. "
      + $"Errors: {string.Join("; ", validation.Errors.Select(e => e.Message))}");
  }
}
