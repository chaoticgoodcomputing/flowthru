using System.Collections.Generic;
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
  /// <inheritdoc/>
  public override ImmutableArray<string> FixableDiagnosticIds =>
    ImmutableArray.Create(FunitDiagnosticAnalyzer.Fu001.Id);

  /// <inheritdoc/>
  public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

  /// <inheritdoc/>
  public override async Task RegisterCodeFixesAsync(CodeFixContext context)
  {
    var root = await context
      .Document.GetSyntaxRootAsync(context.CancellationToken)
      .ConfigureAwait(false);
    if (root is null)
    {
      return;
    }

    var diagnostic = context.Diagnostics.First();
    var span = diagnostic.Location.SourceSpan;
    var classDecl = root.FindToken(span.Start)
      .Parent?.AncestorsAndSelf()
      .OfType<ClassDeclarationSyntax>()
      .FirstOrDefault();

    if (classDecl is null)
    {
      return;
    }

    context.RegisterCodeFix(
      CodeAction.Create(
        title: $"Add nested 'Tests : FunitContext' class inside '{classDecl.Identifier.Text}'",
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
    {
      return document;
    }

    var sourceText = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);

    // Determine base indent from class declaration line
    var classLine = sourceText.Lines.GetLineFromPosition(classDecl.SpanStart);
    var classIndent = classLine.ToString().Length - classLine.ToString().TrimStart().Length;
    var indent = new string(' ', classIndent + 2);

    var stepName = classDecl.Identifier.Text;

    var stub = $$"""

#if FUNIT_ENABLED
{{indent}}/// <summary>FUnit tests for <see cref="{{stepName}}"/>.</summary>
{{indent}}public class Tests : FunitContext
{{indent}}{
{{indent}}    [StepTest(typeof({{stepName}}))]
{{indent}}    public void TODO_WriteYourTestHere()
{{indent}}    {
{{indent}}        throw new System.NotImplementedException();
{{indent}}    }
{{indent}}}
#endif

""";

    // Collect both text edits against the ORIGINAL source positions, then apply
    // in reverse order so earlier insertions don't shift later positions.
    var edits = new List<(TextSpan Span, string Text)>();

    // Edit 1 (earlier in file): add `using Flowthru.FUnit;` if not already present.
    if (root is CompilationUnitSyntax compilationUnit)
    {
      const string funitNs = "Flowthru.FUnit";
      bool hasUsing =
        compilationUnit.Usings.Any(u => u.Name?.ToString() == funitNs)
        || compilationUnit
          .Members.OfType<BaseNamespaceDeclarationSyntax>()
          .Any(ns => ns.Usings.Any(u => u.Name?.ToString() == funitNs));

      if (!hasUsing)
      {
        // Insert after last top-level using, or at the start of the first member.
        int insertPos =
          compilationUnit.Usings.LastOrDefault()?.FullSpan.End
          ?? compilationUnit.Members.FirstOrDefault()?.FullSpan.Start
          ?? 0;
        edits.Add((new TextSpan(insertPos, 0), "using Flowthru.FUnit;\n"));
      }
    }

    // Edit 2 (later in file): insert stub before close brace.
    var closeBrace = classDecl.CloseBraceToken;
    edits.Add((new TextSpan(closeBrace.FullSpan.Start, 0), stub));

    // Apply edits from the end of the file backwards so positions stay valid.
    var newSourceText = sourceText;
    foreach (var (span, text) in edits.OrderByDescending(e => e.Span.Start))
    {
      newSourceText = newSourceText.Replace(span, text);
    }

    return document.WithText(newSourceText);
  }
}
