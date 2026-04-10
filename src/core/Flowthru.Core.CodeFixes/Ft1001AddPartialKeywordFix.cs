using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Flowthru.Core.SourceGenerators.SchemaAnalysis;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Flowthru.Core.CodeFixes;

/// <summary>
/// Code fix for FT1001: adds the <c>partial</c> modifier to a type annotated with
/// <c>[FlowthruSchema]</c> that is missing the keyword.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(Ft1001AddPartialKeywordFix)), Shared]
public sealed class Ft1001AddPartialKeywordFix : CodeFixProvider
{
  public override ImmutableArray<string> FixableDiagnosticIds =>
    ImmutableArray.Create(SchemaGeneratorDiagnostics.TypeMustBePartial.Id);

  public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

  public override async Task RegisterCodeFixesAsync(CodeFixContext context)
  {
    var root = await context
      .Document.GetSyntaxRootAsync(context.CancellationToken)
      .ConfigureAwait(false);
    if (root is null)
      return;

    var diagnostic = context.Diagnostics.First();
    var span = diagnostic.Location.SourceSpan;
    var typeDecl = root.FindToken(span.Start)
      .Parent?.AncestorsAndSelf()
      .OfType<TypeDeclarationSyntax>()
      .FirstOrDefault();

    if (typeDecl is null)
      return;

    context.RegisterCodeFix(
      CodeAction.Create(
        title: "Add 'partial' modifier",
        createChangedDocument: ct => AddPartialModifierAsync(context.Document, typeDecl, ct),
        equivalenceKey: nameof(Ft1001AddPartialKeywordFix)
      ),
      diagnostic
    );
  }

  private static async Task<Document> AddPartialModifierAsync(
    Document document,
    TypeDeclarationSyntax typeDecl,
    CancellationToken cancellationToken
  )
  {
    var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
    if (root is null)
      return document;

    // Insert 'partial' before the 'class'/'record'/'struct' keyword
    var partialToken = SyntaxFactory
      .Token(SyntaxKind.PartialKeyword)
      .WithTrailingTrivia(SyntaxFactory.Space);

    var newModifiers = typeDecl.Modifiers.Add(partialToken);
    var newDecl = typeDecl.WithModifiers(newModifiers);
    var newRoot = root.ReplaceNode(typeDecl, newDecl);
    return document.WithSyntaxRoot(newRoot);
  }
}
