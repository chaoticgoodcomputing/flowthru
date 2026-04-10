using Flowthru.Core.Flows;
using Microsoft.Extensions.Logging;

namespace Flowthru.Core.Results;

/// <summary>
/// Interface for formatting Flow execution results.
/// </summary>
/// <remarks>
/// <para>
/// Result formatters transform a FlowResult into human-readable or
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
public interface IFlowResultFormatter
{
  /// <summary>
  /// Formats and outputs the Flow result.
  /// </summary>
  /// <param name="result">The Flow execution result</param>
  /// <param name="logger">The logger to write output to</param>
  void Format(FlowResult result, ILogger logger);
}
