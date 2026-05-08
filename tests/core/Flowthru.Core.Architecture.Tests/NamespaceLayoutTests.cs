using System.Reflection;
using Flowthru.Data.Catalog;
using Flowthru.Data.Storage;
using Flowthru.Diagnostics;
using Flowthru.Hosting;
using Flowthru.Step;

namespace Flowthru.Core.Architecture.Tests;

/// <summary>
/// Asserts the §2.10 namespace inventory: every algebra type lives
/// in the namespace named for its algebra, and concrete interpreters
/// live in the algebra's <c>.&lt;Name&gt;</c> sub-namespace. The
/// rewrite's whole layout intent gets locked in here — adding a
/// type in the wrong namespace becomes a CI failure, not a code
/// review note.
/// </summary>
[TestFixture]
public class NamespaceLayoutTests
{
  private static IEnumerable<Type> CoreTypes =>
    typeof(IItem<>).Assembly.GetTypes().Where(t => t.IsPublic);

  private static IEnumerable<Type> CliTypes =>
    typeof(global::Flowthru.Cli.FlowthruCli).Assembly.GetTypes().Where(t => t.IsPublic);

  // ── Algebra-namespace invariants ───────────────────────────────────────

  [Test]
  public void EveryIItemImplementor_LivesInDataCatalogOrSubNamespace()
  {
    var offenders = CoreTypes
      .Where(t => !t.IsAbstract && !t.IsInterface)
      .Where(t => t.GetInterfaces().Any(i =>
        i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IItem<>)
      ))
      .Where(t => !(t.Namespace ?? "").StartsWith("Flowthru.Data.Catalog"))
      .ToList();
    Assert.That(offenders, Is.Empty,
      "IItem<T> implementations must live in Flowthru.Data.Catalog (or a sub-namespace). "
      + "Offenders: " + string.Join(", ", offenders.Select(t => t.FullName)));
  }

  [Test]
  public void EveryIStorageAdapterImplementor_LivesInDataStorageOrSubNamespace()
  {
    var offenders = CoreTypes
      .Where(t => !t.IsAbstract && !t.IsInterface)
      .Where(t => t.GetInterfaces().Any(i =>
        i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IStorageAdapter<>)
      ))
      .Where(t => !(t.Namespace ?? "").StartsWith("Flowthru.Data.Storage"))
      .ToList();
    Assert.That(offenders, Is.Empty,
      "IStorageAdapter<T> implementations must live in Flowthru.Data.Storage (or a sub-namespace). "
      + "Offenders: " + string.Join(", ", offenders.Select(t => t.FullName)));
  }

  [Test]
  public void EveryIStepNodeImplementor_LivesInFlowthruStepOrSubNamespace()
  {
    var offenders = CoreTypes
      .Where(t => !t.IsAbstract && !t.IsInterface)
      .Where(t => typeof(IStepNode).IsAssignableFrom(t))
      .Where(t => !(t.Namespace ?? "").StartsWith("Flowthru.Step"))
      .ToList();
    Assert.That(offenders, Is.Empty,
      "IStepNode implementations must live in Flowthru.Step (or a sub-namespace). "
      + "Offenders: " + string.Join(", ", offenders.Select(t => t.FullName)));
  }

  [Test]
  public void EveryIMetadataProviderImplementor_LivesInFlowthruDiagnosticsOrSubNamespace()
  {
    var offenders = CoreTypes
      .Where(t => !t.IsAbstract && !t.IsInterface)
      .Where(t => typeof(IMetadataProvider).IsAssignableFrom(t)
                  || typeof(IPostRunMetadataProvider).IsAssignableFrom(t))
      .Where(t => !(t.Namespace ?? "").StartsWith("Flowthru.Diagnostics"))
      .ToList();
    Assert.That(offenders, Is.Empty,
      "IMetadataProvider / IPostRunMetadataProvider implementations must live in "
      + "Flowthru.Diagnostics (or a sub-namespace). Offenders: "
      + string.Join(", ", offenders.Select(t => t.FullName)));
  }

  [Test]
  public void IFlowthruServiceImplementor_LivesInHostingNamespace()
  {
    var offenders = CoreTypes
      .Where(t => !t.IsAbstract && !t.IsInterface)
      .Where(t => typeof(IFlowthruService).IsAssignableFrom(t))
      .Where(t => !(t.Namespace ?? "").StartsWith("Flowthru.Hosting"))
      .ToList();
    Assert.That(offenders, Is.Empty,
      "IFlowthruService implementations belong in Flowthru.Hosting. Offenders: "
      + string.Join(", ", offenders.Select(t => t.FullName)));
  }

  // ── Closed-namespace invariants (§2.10) ────────────────────────────────

  [Test]
  public void FlowNamespace_HasNoExtensionContributions()
  {
    var flowTypes = CoreTypes
      .Where(t => (t.Namespace ?? "").StartsWith("Flowthru.Flow"))
      .ToList();
    var nonFlowAssembly = flowTypes
      .Where(t => t.Assembly != typeof(IItem<>).Assembly)
      .ToList();
    Assert.That(nonFlowAssembly, Is.Empty,
      "Flowthru.Flow is a closed namespace per §2.10 — no extension types may live there.");
  }

  [Test]
  public void PreludeNamespace_HasNoExtensionContributions()
  {
    var preludeTypes = CoreTypes
      .Where(t => (t.Namespace ?? "").StartsWith("Flowthru.Prelude"))
      .ToList();
    var nonCoreAssembly = preludeTypes
      .Where(t => t.Assembly != typeof(IItem<>).Assembly)
      .ToList();
    Assert.That(nonCoreAssembly, Is.Empty,
      "Flowthru.Prelude is a closed namespace per §2.10 — only Core defines its FP atoms.");
  }

  // ── Project-mirror invariants ──────────────────────────────────────────

  [Test]
  public void Cli_TypesAreNamespaced_FlowthruCli()
  {
    var offenders = CliTypes
      .Where(t => !(t.Namespace ?? "").StartsWith("Flowthru.Cli"))
      .ToList();
    Assert.That(offenders, Is.Empty,
      "Flowthru.Cli's public types must live in the Flowthru.Cli namespace. Offenders: "
      + string.Join(", ", offenders.Select(t => t.FullName)));
  }
}
