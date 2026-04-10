using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Flowthru.FUnit.SourceGenerators;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Flowthru.FUnit.CodeFixes;

/// <summary>
/// Code fix for FU001: scaffolds a stub <c>Tests : FunitContext</c> class inside a
/// <c>#if FUNIT_ENABLED</c> / <c>#endif</c> block at the end of the step class body.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(Fu001ScaffoldTestsClassFix)), Shared]
public sealed class Fu001ScaffoldTestsClassFix : CodeFixProvider
{
  public override ImmutableArray<string> FixableDiagnosticIds =>
    ImmutableArray.Create(FunitDiagnosticAnalyzer.Fu001.Id);

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
    var classDecl = root.FindToken(span.Start)
      .Parent?.AncestorsAndSelf()
      .OfType<ClassDeclarationSyntax>()
      .FirstOrDefault();

    if (classDecl is null)
      return;

    context.RegisterCodeFix(
      CodeAction.Create(
        title: "Scaffold Tests class with #if FUNIT_ENABLED",
        createChangedDocument: ct => ScaffoldTestsClassAsync(context.Document, classDecl, ct),
        equivalenceKey: nameof(Fu001ScaffoldTestsClassFix)
      ),
      diagnostic
    );
  }

  private static async Task<Document> ScaffoldTestsClassAsync(
    Document document,
    ClassDeclarationSyntax classDecl,
    CancellationToken cancellationToken
  )
  {
    var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
    if (root is null)
      return document;

    var sourceText = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);

    // Determine base indent from class declaration line
    var classLine = sourceText.Lines.GetLineFromPosition(classDecl.SpanStart);
    var classIndent = classLine.ToString().Length - classLine.ToString().TrimStart().Length;
    var indent = new string(' ', classIndent + 2);

    var stepName = classDecl.Identifier.Text;

    var stub = $$"""

#if FUNIT_ENABLED
{{indent}}/// <summary>FUnit tests for <see cref="{{stepName}}"/>.</summary>
{{indent}}public class Tests : global::Flowthru.FUnit.FunitContext
{{indent}}{
{{indent}}    [global::Flowthru.FUnit.StepTest(typeof({{stepName}}))]
{{indent}}    public void TODO_WriteYourTestHere()
{{indent}}    {
{{indent}}        throw new global::System.NotImplementedException();
{{indent}}    }
{{indent}}}
#endif

""";

    // Insert BEFORE the close brace token's leading whitespace so the closing
    // brace stays properly indented after the inserted block.
    var closeBrace = classDecl.CloseBraceToken;
    var newSourceText = sourceText.Replace(new TextSpan(closeBrace.FullSpan.Start, 0), stub);

    return document.WithText(newSourceText);
  }
}
