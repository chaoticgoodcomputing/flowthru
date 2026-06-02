using System.Reflection;
using Flowthru.Data.Storage.Csv;
using Flowthru.Data.Storage.EFCore;
using Flowthru.Data.Storage.Excel;
using Flowthru.Data.Storage.Gql;
using Flowthru.Data.Storage.Http;
using Flowthru.Data.Storage.Parquet;
using Flowthru.Data.Storage.Xml;
using Flowthru.Diagnostics.Json;
using Flowthru.Diagnostics.Mermaid;
using Flowthru.Diagnostics.Run;
using Flowthru.Step.Python;

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
          // VerifyEFCoreSchema) plus UseEFCore() as extension methods on
          // IFlowthruBuilder, declared in the Hosting algebra root
          // per §3.2 / §4.8.0.5.
          "Flowthru.Hosting",
          // The DbScope conflict dependency + its profile contributor
          // (ADR-0019) live alongside Core's Validation.Runtime closed
          // sums — same placement as Python's PythonServiceDependency.
          "Flowthru.Validation.Runtime.EFCore",
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
      new ExtensionMirror(
        Name: "Flowthru.Extensions.Http",
        ProbeType: typeof(HttpStorageMedium),
        AllowedNamespacePrefixes: new[]
        {
          "Flowthru.Data.Storage.Http",
          // Http contributes the UseHttp() extension method on
          // IFlowthruBuilder, declared in the Hosting algebra root
          // — same shape as EFCore's VerifyEFCoreXxx hooks.
          "Flowthru.Hosting",
        },
        ForbiddenAlgebraRoots: new[]
        {
          "Flowthru.Data.Storage",
        }
      ),
      new ExtensionMirror(
        Name: "Flowthru.Extensions.Metadata.Diagnostics",
        ProbeType: typeof(StepTimingProvider),
        AllowedNamespacePrefixes: new[]
        {
          "Flowthru.Diagnostics.Run",
          "Flowthru.Diagnostics",
        },
        ForbiddenAlgebraRoots: Array.Empty<string>()
      ),
      new ExtensionMirror(
        Name: "Flowthru.Extensions.GQL",
        ProbeType: typeof(GqlSingleStorageAdapter<,>),
        AllowedNamespacePrefixes: new[]
        {
          "Flowthru.Data.Storage.Gql",
          "Flowthru.Data.Catalog",
        },
        ForbiddenAlgebraRoots: new[]
        {
          "Flowthru.Data.Storage",
        }
      ),
      new ExtensionMirror(
        Name: "Flowthru.Extensions.Python",
        ProbeType: typeof(PythonStep<,>),
        AllowedNamespacePrefixes: new[]
        {
          "Flowthru.Step.Python",
          // FlowBuilder.AddPythonStep extension methods live in
          // FlowBuilder's algebra root.
          "Flowthru.Flow",
          // UsePython() extension method on IFlowthruBuilder.
          "Flowthru.Hosting",
          // PythonRuntimeError + PythonServiceDependency live alongside
          // Core's Validation.Runtime closed sums.
          "Flowthru.Validation.Runtime.Python",
          // PythonPreFlightError + PythonStepValidationHook.
          "Flowthru.Validation.PreFlight.Python",
        },
        ForbiddenAlgebraRoots: new[]
        {
          // Step archetypes are Core's bookkeeping; concrete step
          // impls must live in a sub-namespace.
          "Flowthru.Step",
        }
      ),
    };

  /// <summary>
  /// Robust replacement for <see cref="Assembly.GetTypes"/> — extensions
  /// that depend on native runtimes (Python.NET, etc.) can fail to
  /// fully load their type closure; we fall back to the partial list
  /// the loader did manage to materialise.
  /// </summary>
  private static IEnumerable<Type> SafeGetTypes(Assembly assembly)
  {
    try
    {
      return assembly.GetTypes();
    }
    catch (ReflectionTypeLoadException ex)
    {
      return ex.Types.Where(t => t is not null).Cast<Type>();
    }
  }

  // ── Mirror invariants (one parameterised case per extension) ────────

  [TestCaseSource(nameof(MigratedExtensions))]
  public void PublicTypes_LiveInAllowedNamespaces(ExtensionMirror ext)
  {
    var publicTypes = SafeGetTypes(ext.ProbeType.Assembly)
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

  /// <summary>
  /// Internal types in an extension assembly must follow the same
  /// namespace-prefix rule as public types. The risk this catches is
  /// drift: an <c>internal</c> helper added under <c>Internal/</c> on
  /// disk that lands in the wrong namespace because the public-type
  /// test filters it out. Even though such drift doesn't leak to
  /// downstream users, it breaks the "namespace mirrors source-tree
  /// layout" promise that the Core <see cref="NamespaceLayoutTests"/>
  /// enforces inside Core and that extensions are expected to honour
  /// internally. Restricting the assembly-types view to a single
  /// assembly here keeps the law per-extension rather than asserting
  /// a global rule.
  /// </summary>
  [TestCaseSource(nameof(MigratedExtensions))]
  public void InternalTypes_LiveInAllowedNamespaces(ExtensionMirror ext)
  {
    var internalTypes = SafeGetTypes(ext.ProbeType.Assembly)
      // Top-level non-public types. Nested types (compiler-generated
      // closures from lambdas, anonymous classes) are walked through
      // their declaring type's visibility, so they're skipped here.
      .Where(t => !t.IsPublic && !t.IsNested)
      // Compiler-generated infrastructure (CS$<>, <PrivateImplementationDetails>,
      // generic instantiation markers) lives in the global namespace
      // or has internal-marker names — skip them.
      .Where(t => !string.IsNullOrEmpty(t.Namespace))
      .Where(t => !t.Name.StartsWith('<'))
      // Tooling-injected types: Coverlet emits one
      // Coverlet.Core.Instrumentation.Tracker.<asm>_<guid> per
      // instrumented assembly at test-instrument time. These aren't
      // source code; the namespace rule shouldn't apply.
      .Where(t => !(t.Namespace ?? string.Empty).StartsWith("Coverlet.", StringComparison.Ordinal))
      .ToList();

    var offenders = internalTypes
      .Where(t => !ext.AllowedNamespacePrefixes.Any(prefix =>
        (t.Namespace ?? string.Empty) == prefix
        || (t.Namespace ?? string.Empty).StartsWith(prefix + ".")
      ))
      .ToList();

    Assert.That(offenders, Is.Empty,
      $"{ext.Name}'s internal types must live in one of: "
      + $"[{string.Join(", ", ext.AllowedNamespacePrefixes)}]. Offenders: "
      + string.Join(", ", offenders.Select(t => t.FullName)));
  }

  [TestCaseSource(nameof(MigratedExtensions))]
  public void NoTypesLeakIntoUnscopedAlgebraRoot(ExtensionMirror ext)
  {
    var publicTypes = SafeGetTypes(ext.ProbeType.Assembly)
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
          || p == "Flowthru.Flow"           // step extensions: AddXxx on FlowBuilder
          || p == "Flowthru.Diagnostics"
          || p == "Flowthru.Hosting"
      )
      .ToHashSet();

    var extensionMethodClasses = SafeGetTypes(assembly)
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
