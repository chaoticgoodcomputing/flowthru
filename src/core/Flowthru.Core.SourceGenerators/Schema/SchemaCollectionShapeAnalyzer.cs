using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Flowthru.Core.SourceGenerators.Schema;

/// <summary>
/// Emits FT2010 when a <c>[FlowthruSchema]</c> property's declared type is
/// a set-shaped collection. Set types (<c>HashSet&lt;T&gt;</c>,
/// <c>SortedSet&lt;T&gt;</c>, <c>ISet&lt;T&gt;</c>,
/// <c>IReadOnlySet&lt;T&gt;</c>, and the immutable variants) can lose the
/// converter-dispatch race in System.Text.Json's serializer: in certain
/// configurations the object converter wins, the property serializes as
/// the set wrapper's public surface (<c>{Count, Capacity, Comparer}</c>),
/// and the round-trip fails with "Property set method not found." Arrays
/// and <c>List&lt;T&gt;</c> use STJ's dedicated array/collection
/// converters and don't share the hazard.
/// </summary>
/// <remarks>
/// <para>
/// Scope is deliberately narrow — this catches the exact MagicAtlas
/// failure class (see <c>docs/scratch/reports/magic-atlas-cache-issues.md</c>)
/// and nothing else. The broader schema-boundary contract (schemas
/// declare shape, not storage; collection slots should use interface
/// types so the producer picks the concrete container) is a Wave 3+
/// concern under the container-dispatch matrix in
/// <c>docs/scratch/flowthru-trax-roadmap.md</c>. That work will land its
/// own analyzers when the matrix formalizes which interface fits each
/// family (Materialized / Iterator / Plan).
/// </para>
/// <para>
/// Recursion shape: walks generic type arguments so an outer non-set
/// container containing an inner set still flags (e.g.
/// <c>IReadOnlyList&lt;HashSet&lt;string&gt;&gt;</c> surfaces the inner
/// <c>HashSet&lt;string&gt;</c>). Stops at non-generic leaves.
/// </para>
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class SchemaCollectionShapeAnalyzer : DiagnosticAnalyzer
{
  private const string AttributeFullName = "Flowthru.Data.Schema.FlowthruSchemaAttribute";

  // Fully-qualified open-generic names matched against
  // INamedTypeSymbol.OriginalDefinition.ToDisplayString().
  //
  // Every entry here is a type whose STJ round-trip is conditionally
  // unsafe — the object converter can win over the collection converter
  // depending on the JsonSerializerContext / IncludeFields / property
  // shape combination, producing the {Count, Capacity, Comparer} failure
  // mode MagicAtlas hit. Concrete sets (HashSet, SortedSet) and
  // set-shaped interfaces (ISet, IReadOnlySet) both qualify because the
  // STJ dispatch hazard is rooted in "set" being a wrapper class shape,
  // not in mutability.
  private static readonly HashSet<string> _setShapedTypes = new(System.StringComparer.Ordinal)
  {
    "System.Collections.Generic.HashSet<T>",
    "System.Collections.Generic.SortedSet<T>",
    "System.Collections.Generic.ISet<T>",
    "System.Collections.Generic.IReadOnlySet<T>",
    "System.Collections.Immutable.ImmutableHashSet<T>",
    "System.Collections.Immutable.ImmutableSortedSet<T>",
  };

  /// <inheritdoc/>
  public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
    ImmutableArray.Create(SchemaGeneratorDiagnostics.SchemaPropertyUsesUnsafeSetType);

  /// <inheritdoc/>
  public override void Initialize(AnalysisContext context)
  {
    context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
    context.EnableConcurrentExecution();
    context.RegisterSymbolAction(AnalyzeType, SymbolKind.NamedType);
  }

  private static void AnalyzeType(SymbolAnalysisContext context)
  {
    var typeSymbol = (INamedTypeSymbol)context.Symbol;

    // Only [FlowthruSchema]-decorated types.
    var hasAttr = typeSymbol
      .GetAttributes()
      .Any(a => a.AttributeClass?.ToDisplayString() == AttributeFullName);
    if (!hasAttr) return;

    foreach (var property in typeSymbol.GetMembers().OfType<IPropertySymbol>())
    {
      if (property.DeclaredAccessibility != Accessibility.Public || property.IsStatic) continue;

      var offending = FindSetShapedType(property.Type);
      if (offending is null) continue;

      var location = property.Locations.FirstOrDefault() ?? Location.None;
      context.ReportDiagnostic(
        Diagnostic.Create(
          SchemaGeneratorDiagnostics.SchemaPropertyUsesUnsafeSetType,
          location,
          typeSymbol.Name,
          property.Name,
          offending.ToDisplayString()
        )
      );
    }
  }

  /// <summary>
  /// Walk <paramref name="type"/> and return the first set-shaped type
  /// found (the property's declared type itself, an element type, or a
  /// generic argument deeper down). Returns <c>null</c> when no
  /// set-shaped type is reachable. Unwraps nullable value types
  /// (<c>T?</c>); recurses through arrays and generic types so inner
  /// sets surface even when wrapped in a safe outer container.
  /// </summary>
  private static ITypeSymbol? FindSetShapedType(ITypeSymbol type)
  {
    // Unwrap T? → T for the value-type case.
    if (type is INamedTypeSymbol named
      && named.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T)
    {
      return FindSetShapedType(named.TypeArguments[0]);
    }

    // Arrays are safe themselves — STJ has a dedicated array converter —
    // but recurse into the element type in case it's set-shaped (e.g.
    // HashSet<string>[]).
    if (type is IArrayTypeSymbol array)
    {
      return FindSetShapedType(array.ElementType);
    }

    if (type is INamedTypeSymbol n && n.IsGenericType)
    {
      var openName = n.OriginalDefinition.ToDisplayString();

      // Direct hit: this is a set-shaped type. Surface it as the
      // diagnostic and stop — no need to recurse into its element type
      // (a HashSet<HashSet<T>> would report the outer, which is enough
      // to drive the migration).
      if (_setShapedTypes.Contains(openName))
      {
        return n;
      }

      // Otherwise recurse into the generic arguments so a set nested
      // inside a safe container still surfaces.
      foreach (var arg in n.TypeArguments)
      {
        var inner = FindSetShapedType(arg);
        if (inner is not null) return inner;
      }
    }

    return null;
  }
}
