using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Flowthru.Tests.Helpers;

/// <summary>
/// Manual codefix harness. Drives an analyzer + codefix against a single
/// source string via <see cref="AdhocWorkspace"/>, returning the registered
/// code actions and the resulting changed documents. Use this when the
/// standard <c>Microsoft.CodeAnalysis.CSharp.Testing.CSharpCodeFixTest</c>
/// can't run — e.g. for codefixes that fix diagnostics tagged
/// <c>WellKnownDiagnosticTags.CompilationEnd</c>, which the standard harness
/// refuses with a "non-local analyzer diagnostic" error.
/// </summary>
public static class CodeFixTestHelper
{
  /// <summary>
  /// Compile <paramref name="sourceCode"/>, run <paramref name="analyzer"/> against it,
  /// and apply the first <paramref name="codeFix"/> action whose
  /// <see cref="CodeAction.EquivalenceKey"/> matches <paramref name="equivalenceKey"/>
  /// (or the first action of any kind if <paramref name="equivalenceKey"/> is null).
  /// </summary>
  /// <returns>
  /// An <see cref="CodeFixApplicationResult"/> with the diagnostics observed, every
  /// registered code-action title, and the post-fix text of every document the action
  /// changed (including documents other than the diagnostic's source — the FU100
  /// codefix, for instance, mutates a stub-container doc that isn't the test method's
  /// document).
  /// </returns>
  public static async Task<CodeFixApplicationResult> ApplyCodeFixAsync(
    DiagnosticAnalyzer analyzer,
    CodeFixProvider codeFix,
    string sourceCode,
    string? equivalenceKey = null,
    params Assembly[] extraReferences
  )
  {
    using var workspace = new AdhocWorkspace();
    var project = workspace
      .AddProject("TestProject", LanguageNames.CSharp)
      .AddMetadataReferences(StandardReferences(extraReferences));

    var document = project.AddDocument("Source.cs", sourceCode);
    project = document.Project;

    var compilation = await project
      .GetCompilationAsync()
      .ConfigureAwait(false);
    if (compilation is null)
    {
      throw new InvalidOperationException("Failed to obtain compilation from project.");
    }

    var compilationWithAnalyzers = compilation.WithAnalyzers(
      ImmutableArray.Create(analyzer)
    );
    var diagnostics = await compilationWithAnalyzers
      .GetAnalyzerDiagnosticsAsync()
      .ConfigureAwait(false);

    var fixable = diagnostics
      .Where(d => codeFix.FixableDiagnosticIds.Contains(d.Id))
      .ToImmutableArray();

    var registeredActions = new ConcurrentBag<CodeAction>();
    if (fixable.Length > 0)
    {
      var firstFixable = fixable[0];
      var context = new CodeFixContext(
        document,
        firstFixable,
        (action, _) => registeredActions.Add(action),
        CancellationToken.None
      );
      await codeFix.RegisterCodeFixesAsync(context).ConfigureAwait(false);
    }

    var titles = registeredActions.Select(a => a.Title).ToImmutableArray();

    var selected = equivalenceKey is null
      ? registeredActions.FirstOrDefault()
      : registeredActions.FirstOrDefault(a => a.EquivalenceKey == equivalenceKey);

    var changedDocuments = new Dictionary<string, string>(StringComparer.Ordinal);
    if (selected is not null)
    {
      var operations = await selected
        .GetOperationsAsync(CancellationToken.None)
        .ConfigureAwait(false);

      foreach (var op in operations.OfType<ApplyChangesOperation>())
      {
        var newSolution = op.ChangedSolution;
        var changes = newSolution.GetChanges(workspace.CurrentSolution);

        foreach (var projectChange in changes.GetProjectChanges())
        {
          foreach (var changedDocId in projectChange.GetChangedDocuments())
          {
            var changedDoc = newSolution.GetDocument(changedDocId)!;
            var text = await changedDoc.GetTextAsync().ConfigureAwait(false);
            changedDocuments[changedDoc.Name] = text.ToString();
          }
          foreach (var addedDocId in projectChange.GetAddedDocuments())
          {
            var addedDoc = newSolution.GetDocument(addedDocId)!;
            var text = await addedDoc.GetTextAsync().ConfigureAwait(false);
            changedDocuments[addedDoc.Name] = text.ToString();
          }
        }
      }
    }

    return new CodeFixApplicationResult(diagnostics, titles, changedDocuments);
  }

  private static IEnumerable<MetadataReference> StandardReferences(Assembly[] extra)
  {
    var tpa = ((string?)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES"))?.Split(
      Path.PathSeparator
    ) ?? Array.Empty<string>();
    foreach (var path in tpa)
    {
      if (!string.IsNullOrEmpty(path))
      {
        yield return MetadataReference.CreateFromFile(path);
      }
    }
    foreach (var asm in extra)
    {
      yield return MetadataReference.CreateFromFile(asm.Location);
    }
  }
}

/// <summary>
/// Outcome of <see cref="CodeFixTestHelper.ApplyCodeFixAsync"/>.
/// </summary>
public sealed record CodeFixApplicationResult(
  ImmutableArray<Diagnostic> InitialDiagnostics,
  ImmutableArray<string> RegisteredCodeFixTitles,
  IReadOnlyDictionary<string, string> ChangedDocuments
);
