using Flowthru.Pipelines;
using Microsoft.Extensions.Logging;

namespace Flowthru.Results;

/// <summary>
/// Interface for formatting pipeline execution results.
/// </summary>
/// <remarks>
/// <para>
/// Result formatters transform a PipelineResult into human-readable or
/// machine-readable output via logging.
/// </para>
/// <para>
/// Built-in formatters:
/// - <see cref="ConsoleResultFormatter"/> - Human-readable console output (default)
/// </para>
/// <para>
/// Future formatters: JSON, Markdown, compact CI/CD format.
/// </para>
/// </remarks>
public interface IPipelineResultFormatter {
  /// <summary>
  /// Formats and outputs the pipeline result.
  /// </summary>
  /// <param name="result">The pipeline execution result</param>
  /// <param name="logger">The logger to write output to</param>
  void Format(PipelineResult result, ILogger logger);
}
