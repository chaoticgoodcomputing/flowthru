using Flowthru.Pipelines;
using Flowthru.Services.Models;

namespace Flowthru.Cli;

/// <summary>
/// Parses command-line arguments into structured execution requests.
/// </summary>
internal static class ArgumentParser
{
  /// <summary>
  /// Parses command-line arguments into a pipeline execution request.
  /// </summary>
  /// <param name="args">Command-line arguments</param>
  /// <param name="availablePipelines">Available pipeline names for validation</param>
  /// <returns>Parsed execution request, or null to execute all pipelines</returns>
  public static ParsedArguments Parse(string[] args, IEnumerable<string> availablePipelines)
  {
    var options = new ExecutionOptions();
    var exportMetadata = true;
    string? metadataOutputDirectory = null;
    string? pipelineName = null;

    // Parse arguments
    for (int i = 0; i < args.Length; i++)
    {
      var arg = args[i];

      switch (arg)
      {
        case "--dry-run":
          options.DryRun = true;
          break;

        case "--no-metadata":
          exportMetadata = false;
          break;

        case "--metadata-output":
          if (i + 1 < args.Length)
          {
            metadataOutputDirectory = args[++i];
          }
          else
          {
            throw new ArgumentException("--metadata-output requires a directory path");
          }
          break;

        case "--help":
        case "-h":
          return new ParsedArguments { ShowHelp = true };

        case "--version":
        case "-v":
          return new ParsedArguments { ShowVersion = true };

        default:
          // First non-flag argument is the pipeline name
          if (!arg.StartsWith("--") && pipelineName == null)
          {
            pipelineName = arg;
          }
          break;
      }
    }

    // If no pipeline specified, run all
    if (pipelineName == null)
    {
      return new ParsedArguments { ExecuteAll = true, Options = options };
    }

    // Validate pipeline name
    if (!availablePipelines.Contains(pipelineName))
    {
      return new ParsedArguments
      {
        Error =
          $"Pipeline '{pipelineName}' not found. Available: {string.Join(", ", availablePipelines)}",
      };
    }

    // Create execution request
    return new ParsedArguments
    {
      Request = new PipelineExecutionRequest
      {
        PipelineName = pipelineName,
        Options = options,
        ExportMetadata = exportMetadata,
        MetadataOutputDirectory = metadataOutputDirectory,
      },
    };
  }
}

/// <summary>
/// Result of parsing command-line arguments.
/// </summary>
internal sealed class ParsedArguments
{
  /// <summary>
  /// Pipeline execution request (if a specific pipeline was specified).
  /// </summary>
  public PipelineExecutionRequest? Request { get; init; }

  /// <summary>
  /// Whether to execute all pipelines.
  /// </summary>
  public bool ExecuteAll { get; init; }

  /// <summary>
  /// Execution options (when executing all pipelines).
  /// </summary>
  public ExecutionOptions? Options { get; init; }

  /// <summary>
  /// Whether to show help message.
  /// </summary>
  public bool ShowHelp { get; init; }

  /// <summary>
  /// Whether to show version information.
  /// </summary>
  public bool ShowVersion { get; init; }

  /// <summary>
  /// Parse error message (if any).
  /// </summary>
  public string? Error { get; init; }
}
