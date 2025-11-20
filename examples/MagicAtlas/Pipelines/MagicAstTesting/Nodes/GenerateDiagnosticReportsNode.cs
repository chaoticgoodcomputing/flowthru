using MagicAST.Core;
using MagicAST.Core.AST;
using MagicAST.Core.Diagnostics;
using MagicAST.DTOs;
using MagicAtlas.Data._08_Reporting.Schemas;

namespace MagicAtlas.Pipelines.MagicAstTesting.Nodes;

/// <summary>
/// Pipeline node that analyzes all cards and generates diagnostic reports.
/// </summary>
public static class GenerateDiagnosticReportsNode
{
  /// <summary>
  /// Configuration for diagnostic report generation.
  /// </summary>
  public record Params
  {
    /// <summary>
    /// Maximum number of examples to include per diagnostic. Default: 5.
    /// </summary>
    public int MaxExamplesPerDiagnostic { get; init; } = 3;

    /// <summary>
    /// Maximum number of top diagnostics to include in each report. Default: 20.
    /// </summary>
    public int TopDiagnosticsCount { get; init; } = 10;
  }

  /// <summary>
  /// Creates a transform function that parses all cards and generates error/warning reports.
  /// </summary>
  public static Func<
    IEnumerable<CardInputDto>,
    Task<(IEnumerable<DiagnosticReport> Errors, IEnumerable<DiagnosticReport> Warnings)>
  > Create(Params? parameters = null)
  {
    var config = parameters ?? new Params();

    return async (inputs) =>
    {
      var inputList = inputs.ToList();
      var totalCards = inputList.Count;

      // Parse all cards and collect diagnostics
      var diagnosticsByCodeAndMessage =
        new Dictionary<
          (string Code, string Message, DiagnosticSeverity Severity),
          List<(string CardName, string? SourceText)>
        >();

      foreach (var cardInput in inputList)
      {
        var cardNode = CardParser.Parse(cardInput);

        foreach (var diagnostic in cardNode.Diagnostics)
        {
          var key = (diagnostic.Id, diagnostic.GetMessage(), diagnostic.Severity);

          if (!diagnosticsByCodeAndMessage.ContainsKey(key))
          {
            diagnosticsByCodeAndMessage[key] = new List<(string, string?)>();
          }

          diagnosticsByCodeAndMessage[key]
            .Add((cardInput.Name, diagnostic.Location?.GetSourceText()));
        }
      }

      // Generate error report
      var errorReports = diagnosticsByCodeAndMessage
        .Where(kvp => kvp.Key.Severity == DiagnosticSeverity.Error)
        .OrderByDescending(kvp => kvp.Value.Count)
        .Take(config.TopDiagnosticsCount)
        .Select(kvp => new DiagnosticReport
        {
          Code = kvp.Key.Code,
          Message = kvp.Key.Message,
          Count = kvp.Value.Count,
          TotalCards = totalCards,
          PercentageSuccessful =
            totalCards > 0 ? ((totalCards - kvp.Value.Count) * 100.0 / totalCards) : 100.0,
          Examples = kvp
            .Value.Take(config.MaxExamplesPerDiagnostic)
            .Select(example => new DiagnosticExample
            {
              CardName = example.CardName,
              SourceText = example.SourceText,
            })
            .ToList(),
        })
        .ToList();

      // Generate warning report
      var warningReports = diagnosticsByCodeAndMessage
        .Where(kvp => kvp.Key.Severity == DiagnosticSeverity.Warning)
        .OrderByDescending(kvp => kvp.Value.Count)
        .Take(config.TopDiagnosticsCount)
        .Select(kvp => new DiagnosticReport
        {
          Code = kvp.Key.Code,
          Message = kvp.Key.Message,
          Count = kvp.Value.Count,
          TotalCards = totalCards,
          PercentageSuccessful =
            totalCards > 0 ? ((totalCards - kvp.Value.Count) * 100.0 / totalCards) : 100.0,
          Examples = kvp
            .Value.Take(config.MaxExamplesPerDiagnostic)
            .Select(example => new DiagnosticExample
            {
              CardName = example.CardName,
              SourceText = example.SourceText,
            })
            .ToList(),
        })
        .ToList();

      return await Task.FromResult<(IEnumerable<DiagnosticReport>, IEnumerable<DiagnosticReport>)>(
        (errorReports, warningReports)
      );
    };
  }
}
