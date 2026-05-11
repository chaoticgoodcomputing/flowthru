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
using Microsoft.CodeAnalysis.Text;

namespace Flowthru.FUnit.CodeFixes;

/// <summary>
/// Code fix for FU100: when a <c>[FUnitStepTest]</c> references a step whose
/// service dependency has no registered stub, this fix inserts a registration
/// template into an existing <c>[FUnitStubContainer]</c> in the project. If
/// no container exists, it scaffolds a new one in the test class's namespace.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(Fu100AddStubRegistrationFix)), Shared]
public sealed class Fu100AddStubRegistrationFix : CodeFixProvider
{
  private const string ServiceFullNameProperty = "ServiceFullName";

  /// <inheritdoc/>
  public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create("FU100");

  /// <inheritdoc/>
  public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

  /// <inheritdoc/>
  public override async Task RegisterCodeFixesAsync(CodeFixContext context)
  {
    var diagnostic = context.Diagnostics.First();

    // Recover the service type's full name from diagnostic properties stashed
    // by the analyzer — saves us re-walking the step's Create() params here.
    if (
      !diagnostic.Properties.TryGetValue(ServiceFullNameProperty, out var serviceFullName)
      || string.IsNullOrEmpty(serviceFullName)
    )
    {
      return;
    }

    // Look for an existing [FUnitStubContainer] anywhere in the project.
    var solution = context.Document.Project.Solution;
    var existingContainer = await FindExistingStubContainerAsync(
      context.Document.Project,
      context.CancellationToken
    ).ConfigureAwait(false);

    if (existingContainer is not null)
    {
      context.RegisterCodeFix(
        CodeAction.Create(
          title: $"Add '{serviceFullName}' registration to existing [FUnitStubContainer]",
          createChangedSolution: ct =>
            AddRegistrationToExistingContainerAsync(
              solution,
              existingContainer,
              serviceFullName!,
              ct
            ),
          equivalenceKey: nameof(Fu100AddStubRegistrationFix) + ":existing"
        ),
        diagnostic
      );
    }
    else
    {
      context.RegisterCodeFix(
        CodeAction.Create(
          title: $"Create [FUnitStubContainer] with '{serviceFullName}' registration",
          createChangedDocument: ct =>
            ScaffoldNewContainerAsync(context.Document, serviceFullName!, ct),
          equivalenceKey: nameof(Fu100AddStubRegistrationFix) + ":new"
        ),
        diagnostic
      );
    }
  }

  // ─────────────────────────────────────────────────────────────────────────
  // Find an existing [FUnitStubContainer] class anywhere in the project
  // ─────────────────────────────────────────────────────────────────────────

  private static async Task<TypeDeclarationSyntax?> FindExistingStubContainerAsync(
    Project project,
    CancellationToken cancellationToken
  )
  {
    foreach (var document in project.Documents)
    {
      var syntaxRoot = await document
        .GetSyntaxRootAsync(cancellationToken)
        .ConfigureAwait(false);
      if (syntaxRoot is null)
      {
        continue;
      }

      var typeDecl = syntaxRoot
        .DescendantNodes()
        .OfType<TypeDeclarationSyntax>()
        .FirstOrDefault(t => HasStubContainerAttribute(t));
      if (typeDecl is not null)
      {
        return typeDecl;
      }
    }
    return null;
  }

  private static bool HasStubContainerAttribute(TypeDeclarationSyntax typeDecl)
  {
    foreach (var attrList in typeDecl.AttributeLists)
    {
      foreach (var attr in attrList.Attributes)
      {
        var name = attr.Name.ToString();
        if (
          name == "FUnitStubContainer"
          || name == "FUnitStubContainerAttribute"
          || name.EndsWith(".FUnitStubContainer")
          || name.EndsWith(".FUnitStubContainerAttribute")
        )
        {
          return true;
        }
      }
    }
    return false;
  }

  // ─────────────────────────────────────────────────────────────────────────
  // Add a registration line to an existing container's Configure method body
  // ─────────────────────────────────────────────────────────────────────────

  private static async Task<Solution> AddRegistrationToExistingContainerAsync(
    Solution solution,
    TypeDeclarationSyntax containerTypeDecl,
    string serviceFullName,
    CancellationToken cancellationToken
  )
  {
    var containerDoc = solution.GetDocument(containerTypeDecl.SyntaxTree);
    if (containerDoc is null)
    {
      return solution;
    }

    var sourceText = await containerDoc.GetTextAsync(cancellationToken).ConfigureAwait(false);

    // Find the Configure(IServiceCollection) method body.
    var configureMethod = containerTypeDecl
      .Members.OfType<MethodDeclarationSyntax>()
      .FirstOrDefault(m =>
        m.Identifier.Text == "Configure"
        && m.ParameterList.Parameters.Count == 1
        && m.Modifiers.Any(SyntaxKind.PublicKeyword)
        && m.Modifiers.Any(SyntaxKind.StaticKeyword)
      );

    if (configureMethod?.Body is null)
    {
      return solution;
    }

    var openBrace = configureMethod.Body.OpenBraceToken;
    var line = sourceText.Lines.GetLineFromPosition(openBrace.SpanStart);
    var lineText = line.ToString();
    var leadingWhitespace = lineText.Length - lineText.TrimStart().Length;
    var bodyIndent = new string(' ', leadingWhitespace + 4);

    // Insert: services.AddSingleton<{service}, TODO_StubImpl>();
    // The user replaces TODO_StubImpl with the actual fake/mock class.
    var insertion =
      $"\n{bodyIndent}// TODO: Replace TODO_StubImpl with your fake/mock implementation.\n"
      + $"{bodyIndent}services.AddSingleton<global::{serviceFullName}, TODO_StubImpl>();\n";

    var insertPos = openBrace.Span.End;
    var newSourceText = sourceText.Replace(new TextSpan(insertPos, 0), insertion);
    var newDoc = containerDoc.WithText(newSourceText);
    return newDoc.Project.Solution;
  }

  // ─────────────────────────────────────────────────────────────────────────
  // Scaffold a new [FUnitStubContainer] class in the test class's namespace
  // ─────────────────────────────────────────────────────────────────────────

  private static async Task<Document> ScaffoldNewContainerAsync(
    Document document,
    string serviceFullName,
    CancellationToken cancellationToken
  )
  {
    var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
    if (root is not CompilationUnitSyntax compilationUnit)
    {
      return document;
    }

    var sourceText = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);

    // Determine the namespace declaration if any.
    var namespaceDecl = compilationUnit
      .Members.OfType<BaseNamespaceDeclarationSyntax>()
      .FirstOrDefault();

    var indent = "";
    int insertPos;

    if (namespaceDecl is FileScopedNamespaceDeclarationSyntax fileScopedNs)
    {
      // File-scoped namespace: append to the end of the file.
      insertPos = fileScopedNs.Span.End;
      indent = "";
    }
    else if (namespaceDecl is NamespaceDeclarationSyntax blockNs)
    {
      // Block namespace: insert just before the close brace.
      insertPos = blockNs.CloseBraceToken.SpanStart;
      indent = "    ";
    }
    else
    {
      // No namespace: append at end of file (top-level).
      insertPos = root.Span.End;
      indent = "";
    }

    var stub =
      $"\n\n{indent}/// <summary>\n"
      + $"{indent}/// Auto-generated stub container by Fu100AddStubRegistrationFix.\n"
      + $"{indent}/// Add additional <c>services.AddSingleton(...)</c> calls here as needed.\n"
      + $"{indent}/// </summary>\n"
      + $"{indent}[global::Flowthru.Step.Testing.FUnitStubContainer]\n"
      + $"{indent}internal static class TestStubs\n"
      + $"{indent}{{\n"
      + $"{indent}    public static void Configure(global::Microsoft.Extensions.DependencyInjection.IServiceCollection services)\n"
      + $"{indent}    {{\n"
      + $"{indent}        // TODO: Replace TODO_StubImpl with your fake/mock implementation.\n"
      + $"{indent}        services.AddSingleton<global::{serviceFullName}, TODO_StubImpl>();\n"
      + $"{indent}    }}\n"
      + $"{indent}}}\n";

    var newSourceText = sourceText.Replace(new TextSpan(insertPos, 0), stub);
    return document.WithText(newSourceText);
  }
}
