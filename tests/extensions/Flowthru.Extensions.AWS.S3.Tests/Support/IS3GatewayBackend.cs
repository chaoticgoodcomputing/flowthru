using Flowthru.Data.Storage.S3;
using Flowthru.Tests.Kits.Prelude;

namespace Flowthru.Extensions.AWS.S3.Tests.Support;

/// <summary>
/// Backend abstraction for <see cref="Contract.S3GatewayLaws{TBackend}"/> — the
/// S3 analogue of the EF Core / Sheets backend matrix, for a behavioral contract
/// over a live object store rather than a resource bracket. Mirrors
/// <c>ISheetsGatewayBackend</c>: the laws touch only <see cref="IS3Gateway"/> and
/// neutral types, and run identically over every backend.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Disjoint-state contract.</strong> A single backend instance lives for
/// a whole fixture; <see cref="CreateResource"/> is called per test and must
/// return a gateway + addressing context whose objects are disjoint from every
/// prior call — a fresh temp root for the offline tier, a unique key prefix on a
/// shared bucket for the live tier — so tests never observe each other's effects.
/// </para>
/// <para>
/// <strong>Constructor contract.</strong> Constructors must be cheap and
/// configuration-only — no network, no client build. Expensive shared setup
/// belongs in <see cref="InitializeAsync"/>, which the laws kit runs only after
/// the <see cref="RequiredCapabilities"/> gate clears.
/// </para>
/// </remarks>
public interface IS3GatewayBackend
{
  /// <summary>
  /// Capabilities this backend depends on. The laws kit's <c>OneTimeSetUp</c>
  /// checks them via <c>Assume.That</c> before any setup — a missing capability
  /// yields an Inconclusive fixture rather than a failure. Empty for the offline
  /// tier.
  /// </summary>
  IReadOnlyList<TestCapability> RequiredCapabilities => [];

  /// <summary>
  /// Expensive shared setup needing an async context (building the S3 client,
  /// confirming the test bucket). Invoked once per fixture after the capability
  /// gate clears. No-op by default.
  /// </summary>
  Task InitializeAsync() => Task.CompletedTask;

  /// <summary>
  /// Build a fresh gateway + addressing context for one test, disjoint from
  /// every prior call.
  /// </summary>
  S3GatewayContext CreateResource();

  /// <summary>
  /// Tear down every object/temp file the fixture created. Best-effort; invoked
  /// once from <c>OneTimeTearDown</c>.
  /// </summary>
  Task Cleanup() => Task.CompletedTask;
}

/// <summary>
/// The per-test addressing context a backend hands the laws: the gateway under
/// test, the bucket to address, and a backend-unique key prefix that keeps each
/// test's objects disjoint from every other test's.
/// </summary>
/// <param name="Gateway">The <see cref="IS3Gateway"/> under test.</param>
/// <param name="Bucket">The bucket the laws address.</param>
/// <param name="KeyPrefix">A unique-per-resource prefix the laws prepend to every key they touch.</param>
public sealed record S3GatewayContext(
  IS3Gateway Gateway,
  string Bucket,
  string KeyPrefix)
{
  /// <summary>Qualify a logical key with this context's unique prefix.</summary>
  public string Key(string logicalName) => $"{KeyPrefix}{logicalName}";
}
