using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Flowthru.FUnit.CodeFixes;

/// <summary>
/// Fix-it for FU001 — scaffolds an empty
/// <c>public class Tests : FUnitContext { }</c> nested inside the
/// flagged <c>[FlowthruStep]</c> class, wrapped in
/// <c>#if FUNIT_ENABLED</c>. The user fills in
/// <c>[FUnitStepTest]</c>-decorated methods.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(Fu001ScaffoldTestsClassFix))]
[Shared]
public sealed class Fu001ScaffoldTestsClassFix : CodeFixProvider
{
  /// <inheritdoc/>
  public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create("FU001");

  /// <inheritdoc/>
  public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

  /// <inheritdoc/>
  public override async Task RegisterCodeFixesAsync(CodeFixContext context)
  {
    var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
    if (root is null) return;

    var diagnostic = context.Diagnostics.First();
    var classNode = root.FindNode(diagnostic.Location.SourceSpan).FirstAncestorOrSelf<ClassDeclarationSyntax>();
    if (classNode is null) return;

    context.RegisterCodeFix(
      CodeAction.Create(
        title: "Scaffold inline FUnit Tests class",
        createChangedDocument: ct => ScaffoldAsync(context.Document, classNode, ct),
        equivalenceKey: nameof(Fu001ScaffoldTestsClassFix)
      ),
      diagnostic
    );
  }

  private static async Task<Document> ScaffoldAsync(
    Document document,
    ClassDeclarationSyntax classNode,
    CancellationToken cancellationToken
  )
  {
    var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
    if (root is null) return document;

    // Build:
    //   #if FUNIT_ENABLED
    //   public class Tests : Flowthru.Step.Testing.FUnitContext
    //   {
    //   }
    //   #endif
    var testsClass = SyntaxFactory
      .ClassDeclaration("Tests")
      .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
      .AddBaseListTypes(
        SyntaxFactory.SimpleBaseType(
          SyntaxFactory.ParseTypeName("global::Flowthru.Step.Testing.FUnitContext")
        )
      )
      .WithLeadingTrivia(
        SyntaxFactory.Trivia(
          SyntaxFactory.IfDirectiveTrivia(
            SyntaxFactory.IdentifierName("FUNIT_ENABLED"),
            isActive: true,
            branchTaken: true,
            conditionValue: true
          )
        )
      )
      .WithTrailingTrivia(
        SyntaxFactory.Trivia(
          SyntaxFactory.EndIfDirectiveTrivia(isActive: true)
        ),
        SyntaxFactory.EndOfLine("\n")
      );

    var newClass = classNode.AddMembers(testsClass);
    var newRoot = root.ReplaceNode(classNode, newClass);
    return document.WithSyntaxRoot(newRoot);
  }
}
