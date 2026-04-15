using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Flowthru.Core.SourceGenerators.SchemaAnalysis;

/// <summary>
/// Analyzer that validates types annotated with <c>[FlowthruSchema]</c>.
/// Emits:
/// <list type="bullet">
/// <item><c>FT1001</c> — type is not declared <c>partial</c>.</item>
/// <item><c>FT1002</c> — type manually implements a marker interface that the generator
///   would emit, creating a conflict.</item>
/// </list>
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class FlowthruSchemaAnalyzer : DiagnosticAnalyzer
{
  private const string AttributeFullName = "Flowthru.Core.Abstractions.FlowthruSchemaAttribute";

  private static readonly string[] _markerInterfaceNames =
  {
    "Flowthru.Core.Abstractions.IFlatSchema",
    "Flowthru.Core.Abstractions.INestedSchema",
    "Flowthru.Core.Abstractions.ITextSerializable",
    "Flowthru.Core.Abstractions.IBinarySerializable",
    "Flowthru.Core.Abstractions.IStructuredSerializable",
  };

  /// <inheritdoc/>
  public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
    ImmutableArray.Create(
      SchemaGeneratorDiagnostics.TypeMustBePartial,
      SchemaGeneratorDiagnostics.ConflictingManualInterface
    );

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

    // Only types annotated with [FlowthruSchema]
    var attr = typeSymbol
      .GetAttributes()
      .FirstOrDefault(a => a.AttributeClass?.ToDisplayString() == AttributeFullName);
    if (attr is null)
    {
      return;
    }

    // Location: use identifier token of first syntax declaration
    var location = typeSymbol.Locations.FirstOrDefault() ?? Location.None;

    // FT1001 — not partial
    var isPartial = typeSymbol
      .DeclaringSyntaxReferences.Select(r => r.GetSyntax())
      .OfType<TypeDeclarationSyntax>()
      .Any(d => d.Modifiers.Any(SyntaxKind.PartialKeyword));

    if (!isPartial)
    {
      context.ReportDiagnostic(
        Diagnostic.Create(SchemaGeneratorDiagnostics.TypeMustBePartial, location, typeSymbol.Name)
      );
      // Still check FT1002 even if not partial
    }

    // FT1002 — manually-applied conflicting marker interfaces.
    // Walk only user-authored partial declarations via syntax; skip generator-emitted *.g.cs
    // files so the generator's own output does not trigger a false-positive FT1002.
    // We match on simple (unqualified) names from the base list, then resolve the full name
    // from the already-computed typeSymbol.Interfaces — no GetSemanticModel() call needed.
    var interfacesBySimpleName = typeSymbol
      .Interfaces.Where(i => _markerInterfaceNames.Contains(i.ToDisplayString()))
      .ToDictionary(i => i.Name, i => i.ToDisplayString());

    var manual = new List<string>();
    foreach (var syntaxRef in typeSymbol.DeclaringSyntaxReferences)
    {
      if (
        syntaxRef.SyntaxTree.FilePath.EndsWith(".g.cs", System.StringComparison.OrdinalIgnoreCase)
      )
      {
        continue;
      }

      if (syntaxRef.GetSyntax() is not TypeDeclarationSyntax typeDecl || typeDecl.BaseList is null)
      {
        continue;
      }

      foreach (var baseTypeSyntax in typeDecl.BaseList.Types)
      {
        var simpleName = GetSimpleName(baseTypeSyntax.Type);
        if (
          simpleName != null
          && interfacesBySimpleName.TryGetValue(simpleName, out var fullName)
          && !manual.Contains(fullName)
        )
        {
          manual.Add(fullName);
        }
      }
    }

    if (manual.Count > 0)
    {
      // Classify flat vs nested the same way the generator does (flat = all primitive properties)
      bool isFlat = typeSymbol
        .GetMembers()
        .OfType<IPropertySymbol>()
        .Where(p => p.DeclaredAccessibility == Accessibility.Public && !p.IsStatic)
        .All(p => IsPrimitiveLike(p.Type));

      var expected = isFlat ? "flat (IFlatSchema)" : "nested (INestedSchema)";
      var conflicting = string.Join(", ", manual.Select(n => n.Split('.').Last()));

      context.ReportDiagnostic(
        Diagnostic.Create(
          SchemaGeneratorDiagnostics.ConflictingManualInterface,
          location,
          typeSymbol.Name,
          conflicting,
          expected
        )
      );
    }
  }

  private static bool IsPrimitiveLike(ITypeSymbol type)
  {
    if (type.IsValueType || type.SpecialType == SpecialType.System_String)
    {
      return true;
    }

    // Nullable<T> where T is primitive
    if (
      type is INamedTypeSymbol named
      && named.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T
    )
    {
      return IsPrimitiveLike(named.TypeArguments[0]);
    }

    return false;
  }

  /// <summary>
  /// Returns the rightmost simple identifier of a <see cref="TypeSyntax"/> without resolving it
  /// semantically (e.g., "IFlatSchema" from both <c>IFlatSchema</c> and
  /// <c>Flowthru.Core.Abstractions.IFlatSchema</c>).
  /// </summary>
  private static string? GetSimpleName(TypeSyntax typeSyntax) =>
    typeSyntax switch
    {
      IdentifierNameSyntax id => id.Identifier.Text,
      QualifiedNameSyntax q => q.Right.Identifier.Text,
      AliasQualifiedNameSyntax a => a.Name.Identifier.Text,
      _ => null,
    };
}
