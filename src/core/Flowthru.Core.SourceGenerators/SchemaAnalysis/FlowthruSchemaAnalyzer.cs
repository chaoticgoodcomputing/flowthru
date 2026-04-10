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

  private static readonly string[] MarkerInterfaceNames =
  {
    "Flowthru.Core.Abstractions.IFlatSchema",
    "Flowthru.Core.Abstractions.INestedSchema",
    "Flowthru.Core.Abstractions.ITextSerializable",
    "Flowthru.Core.Abstractions.IBinarySerializable",
    "Flowthru.Core.Abstractions.IStructuredSerializable",
  };

  public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
    ImmutableArray.Create(
      SchemaGeneratorDiagnostics.TypeMustBePartial,
      SchemaGeneratorDiagnostics.ConflictingManualInterface
    );

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
      return;

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

    // FT1002 — manually-applied conflicting marker interfaces
    var manual = typeSymbol
      .Interfaces.Select(i => i.ToDisplayString())
      .Where(n => MarkerInterfaceNames.Contains(n))
      .ToList();

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
      return true;

    // Nullable<T> where T is primitive
    if (
      type is INamedTypeSymbol named
      && named.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T
    )
      return IsPrimitiveLike(named.TypeArguments[0]);

    return false;
  }
}
