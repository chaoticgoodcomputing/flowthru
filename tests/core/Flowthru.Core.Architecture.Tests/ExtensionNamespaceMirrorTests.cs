using System.Reflection;
using Flowthru.Data.Storage.Csv;
using Flowthru.Data.Storage.EFCore;
using Flowthru.Data.Storage.Excel;
using Flowthru.Data.Storage.Parquet;
using Flowthru.Data.Storage.Xml;
using Flowthru.Diagnostics.Json;
using Flowthru.Diagnostics.Mermaid;

namespace Flowthru.Core.Architecture.Tests;

/// <summary>
/// Asserts the §2.10 / §4.8 step-2 invariant for migrated extensions:
/// every public type in the extension assembly lives either in the
/// algebra's <c>.&lt;Name&gt;</c> sub-namespace (for interpreters) or
/// directly in the algebra namespace itself (for the
/// extension-method-based smart-constructor surface). One row per
/// migrated extension; unmigrated extensions stay absent until they
/// land.
/// </summary>
[TestFixture]
public class ExtensionNamespaceMirrorTests
{
  /// <summary>
  /// Per-extension descriptor consumed by the parameterised mirror
  /// laws below. Adding a row here is the only change needed when a
  /// new extension lands.
  /// </summary>
  /// <param name="ProbeType">
  /// Any public type from the extension assembly — used to obtain the
  /// assembly handle without naming an internal class.
  /// </param>
  /// <param name="AllowedNamespacePrefixes">
  /// The set of namespaces extension-public types are allowed to live
  /// in. Typically the algebra sub-namespace
  /// (<c>Flowthru.&lt;Algebra&gt;.&lt;Name&gt;</c>) for interpreters
  /// and the algebra root (<c>Flowthru.&lt;Algebra&gt;</c>) for
  /// extension-method smart constructors.
  /// </param>
  /// <param name="ForbiddenAlgebraRoots">
  /// Algebra-root namespaces in which no public type from the extension
  /// is allowed to leak — Core's bookkeeping for "the algebra root
  /// belongs to Core."
  /// </param>
  public sealed record ExtensionMirror(
    string Name,
    Type ProbeType,
    IReadOnlyList<string> AllowedNamespacePrefixes,
    IReadOnlyList<string> ForbiddenAlgebraRoots
  )
  {
    public override string ToString() => Name;
  }

  /// <summary>
  /// The set of migrated extensions. Append a row when a new extension
  /// lands; the parameterised laws below pick it up automatically.
  /// </summary>
  public static IEnumerable<ExtensionMirror> MigratedExtensions =>
    new[]
    {
      new ExtensionMirror(
        Name: "Flowthru.Extensions.Csv",
        ProbeType: typeof(CsvFormatSerializer<>),
        AllowedNamespacePrefixes: new[]
        {
          "Flowthru.Data.Storage.Csv",
          "Flowthru.Data.Catalog",
        },
        ForbiddenAlgebraRoots: new[]
        {
          "Flowthru.Data.Storage",
        }
      ),
      new ExtensionMirror(
        Name: "Flowthru.Extensions.Excel",
        ProbeType: typeof(ExcelFormatSerializer<>),
        AllowedNamespacePrefixes: new[]
        {
          "Flowthru.Data.Storage.Excel",
          "Flowthru.Data.Catalog",
        },
        ForbiddenAlgebraRoots: new[]
        {
          "Flowthru.Data.Storage",
        }
      ),
      new ExtensionMirror(
        Name: "Flowthru.Extensions.Parquet",
        ProbeType: typeof(ParquetFormatSerializer<>),
        AllowedNamespacePrefixes: new[]
        {
          "Flowthru.Data.Storage.Parquet",
          "Flowthru.Data.Catalog",
        },
        ForbiddenAlgebraRoots: new[]
        {
          "Flowthru.Data.Storage",
        }
      ),
      new ExtensionMirror(
        Name: "Flowthru.Extensions.Metadata.Json",
        ProbeType: typeof(JsonMetadataProvider),
        AllowedNamespacePrefixes: new[]
        {
          "Flowthru.Diagnostics.Json",
          "Flowthru.Diagnostics",
        },
        ForbiddenAlgebraRoots: Array.Empty<string>()
      ),
      new ExtensionMirror(
        Name: "Flowthru.Extensions.Metadata.Mermaid",
        ProbeType: typeof(MermaidMetadataProvider),
        AllowedNamespacePrefixes: new[]
        {
          "Flowthru.Diagnostics.Mermaid",
          "Flowthru.Diagnostics",
        },
        ForbiddenAlgebraRoots: Array.Empty<string>()
      ),
      new ExtensionMirror(
        Name: "Flowthru.Extensions.EFCore",
        ProbeType: typeof(EFCoreStorageAdapter<>),
        AllowedNamespacePrefixes: new[]
        {
          "Flowthru.Data.Storage.EFCore",
          "Flowthru.Data.Catalog",
          // EFCore contributes registration-validation hooks
          // (VerifyEFCoreConnection / VerifyEFCoreConfiguration /
          // VerifyEFCoreSchema) as extension methods on
          // IFlowthruBuilder, declared in the Hosting algebra root
          // per §3.2 / §4.8.0.5.
          "Flowthru.Hosting",
        },
        ForbiddenAlgebraRoots: new[]
        {
          "Flowthru.Data.Storage",
        }
      ),
      new ExtensionMirror(
        Name: "Flowthru.Extensions.Xml",
        ProbeType: typeof(SingletonXmlAdapter<>),
        AllowedNamespacePrefixes: new[]
        {
          "Flowthru.Data.Storage.Xml",
          "Flowthru.Data.Catalog",
        },
        ForbiddenAlgebraRoots: new[]
        {
          "Flowthru.Data.Storage",
        }
      ),
    };

  // ── Mirror invariants (one parameterised case per extension) ────────

  [TestCaseSource(nameof(MigratedExtensions))]
  public void PublicTypes_LiveInAllowedNamespaces(ExtensionMirror ext)
  {
    var publicTypes = ext.ProbeType.Assembly
      .GetTypes()
      .Where(t => t.IsPublic)
      .ToList();

    var offenders = publicTypes
      .Where(t => !ext.AllowedNamespacePrefixes.Any(prefix =>
        (t.Namespace ?? string.Empty) == prefix
        || (t.Namespace ?? string.Empty).StartsWith(prefix + ".")
      ))
      .ToList();

    Assert.That(offenders, Is.Empty,
      $"{ext.Name}'s public types must live in one of: "
      + $"[{string.Join(", ", ext.AllowedNamespacePrefixes)}]. Offenders: "
      + string.Join(", ", offenders.Select(t => t.FullName)));
  }

  [TestCaseSource(nameof(MigratedExtensions))]
  public void NoTypesLeakIntoUnscopedAlgebraRoot(ExtensionMirror ext)
  {
    var publicTypes = ext.ProbeType.Assembly
      .GetTypes()
      .Where(t => t.IsPublic)
      .ToList();

    foreach (var algebraRoot in ext.ForbiddenAlgebraRoots)
    {
      var leaks = publicTypes
        .Where(t => t.Namespace == algebraRoot)
        .ToList();
      Assert.That(leaks, Is.Empty,
        $"{ext.Name} interpreters must live in a sub-namespace, not "
        + $"directly under '{algebraRoot}'. Offenders: "
        + string.Join(", ", leaks.Select(t => t.FullName)));
    }
  }

  [TestCaseSource(nameof(MigratedExtensions))]
  public void SmartConstructorsExposedAsExtensionMethodsOnAlgebraBuilder(ExtensionMirror ext)
  {
    var assembly = ext.ProbeType.Assembly;
    var algebraNamespaces = ext.AllowedNamespacePrefixes
      .Where(p =>
        // Algebra-root namespaces (no further dot) hold smart-constructor
        // extension-method classes; sub-namespaces hold interpreters.
        p == "Flowthru.Data.Catalog"
          || p == "Flowthru.Step"
          || p == "Flowthru.Diagnostics"
          || p == "Flowthru.Hosting"
      )
      .ToHashSet();

    var extensionMethodClasses = assembly
      .GetTypes()
      .Where(t => t.IsPublic && t.IsSealed && t.IsAbstract) // static class
      .Where(t => algebraNamespaces.Contains(t.Namespace ?? string.Empty))
      .ToList();

    Assert.That(extensionMethodClasses, Is.Not.Empty,
      $"{ext.Name} must declare at least one static class in an algebra root "
      + $"({string.Join(", ", algebraNamespaces)}) hosting extension-method "
      + "smart constructors. None found.");

    foreach (var cls in extensionMethodClasses)
    {
      var extensionMethods = cls
        .GetMethods(BindingFlags.Public | BindingFlags.Static)
        .Where(m => m.IsDefined(typeof(System.Runtime.CompilerServices.ExtensionAttribute), inherit: false))
        .ToList();
      Assert.That(extensionMethods, Is.Not.Empty,
        $"{cls.FullName} must declare at least one extension method "
        + $"contributing a smart constructor.");
    }
  }
}
