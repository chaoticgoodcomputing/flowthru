using Flowthru.Core.Graph.Meta.Models;
using Flowthru.Core.Meta.Providers;

namespace Flowthru.Tests.Kits.Metadata;

/// <summary>
/// Abstract conformance suite for <see cref="IMetadataProvider"/> implementors. Subclasses
/// inherit this and supply provider construction; the kit verifies the contract is honored.
/// </summary>
/// <remarks>
/// <para>
/// Providers that also implement <see cref="IPostRunMetadataProvider"/> should override
/// <see cref="SamplePostRun"/> to opt into the post-run scenarios; otherwise those tests
/// pass trivially.
/// </para>
/// </remarks>
public abstract class MetadataProviderConformance
{
  /// <summary>Builds a fresh provider instance pointing at a working configuration
  /// (e.g., a writable temp directory for file-emitting providers).</summary>
  protected abstract IMetadataProvider CreateProvider();

  /// <summary>
  /// A minimal DagMetadata snapshot used for the pre-run consume scenario. Override to
  /// supply a richer fixture (e.g., one that exercises specific edge cases).
  /// </summary>
  protected virtual DagMetadata SampleDag =>
    new DagMetadata
    {
      FlowName = "ConformanceTestFlow",
      Steps = new(),
      CatalogItems = new(),
      Edges = new(),
    };

  /// <summary>
  /// Sample <see cref="RunMetadata"/> for post-run scenarios. Default is null — providers
  /// that implement <see cref="IPostRunMetadataProvider"/> should override.
  /// </summary>
  protected virtual RunMetadata? SamplePostRun => null;

  // ── Contract: provider identity ─────────────────────────────────────────

  [Test]
  public void Name_IsNonEmpty()
  {
    var provider = CreateProvider();
    Assert.That(provider.Name, Is.Not.Null.And.Not.Empty, "Provider Name must be non-empty.");
  }

  // ── Contract: pre-run Consume ───────────────────────────────────────────

  [Test]
  public void Consume_PreRunDag_DoesNotThrow()
  {
    var provider = CreateProvider();
    Assert.DoesNotThrow(
      () => provider.Consume(SampleDag),
      "IMetadataProvider.Consume(DagMetadata) must not throw on a well-formed DAG. "
        + "Per the interface contract, errors are logged but should never propagate; if your "
        + "provider needs to bail on bad input, surface the error through its own diagnostics."
    );
  }

  // ── Contract: post-run Consume (when applicable) ────────────────────────

  [Test]
  public void Consume_PostRun_DoesNotThrow_WhenImplemented()
  {
    var provider = CreateProvider();

    if (provider is not IPostRunMetadataProvider postRun)
    {
      Assert.Pass($"{provider.Name} does not implement IPostRunMetadataProvider; skipping.");
    }

    var sample = SamplePostRun;
    if (sample is null)
    {
      Assert.Inconclusive(
        "Provider implements IPostRunMetadataProvider but the conformance subclass did not "
          + "override SamplePostRun. Provide a RunMetadata sample to exercise the post-run path."
      );
    }

    Assert.DoesNotThrow(
      () => ((IPostRunMetadataProvider)provider).Consume(sample!),
      "IPostRunMetadataProvider.Consume(RunMetadata) must not throw on a well-formed run."
    );
  }

  // ── Contract: idempotency ───────────────────────────────────────────────

  [Test]
  public void Consume_PreRunDag_IsIdempotent()
  {
    var provider = CreateProvider();
    Assert.DoesNotThrow(
      () =>
      {
        provider.Consume(SampleDag);
        provider.Consume(SampleDag);
      },
      "Calling Consume twice with the same DAG must not throw. Second-call failures often "
        + "indicate the provider holds stale state between runs."
    );
  }
}
