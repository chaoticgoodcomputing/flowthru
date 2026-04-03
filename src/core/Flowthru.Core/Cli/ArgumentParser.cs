using Flowthru.Flows;
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

    // Slicing options
    HashSet<string>? pipelines = null;
    HashSet<string>? fromNodes = null;
    HashSet<string>? toNodes = null;
    HashSet<string>? fromData = null;
    HashSet<string>? toData = null;
    HashSet<string>? onlyNodes = null;

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

        case "--pipelines":
        case "--pipeline":
          if (i + 1 < args.Length)
          {
            pipelines ??= new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            pipelines.UnionWith(args[++i].Split(',', StringSplitOptions.RemoveEmptyEntries));
          }
          else
          {
            throw new ArgumentException(
              "--pipelines requires a comma-separated list of pipeline names"
            );
          }
          break;

        case "--from-nodes":
          if (i + 1 < args.Length)
          {
            fromNodes ??= new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            fromNodes.UnionWith(args[++i].Split(',', StringSplitOptions.RemoveEmptyEntries));
          }
          else
          {
            throw new ArgumentException(
              "--from-nodes requires a comma-separated list of node names"
            );
          }
          break;

        case "--to-nodes":
          if (i + 1 < args.Length)
          {
            toNodes ??= new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            toNodes.UnionWith(args[++i].Split(',', StringSplitOptions.RemoveEmptyEntries));
          }
          else
          {
            throw new ArgumentException("--to-nodes requires a comma-separated list of node names");
          }
          break;

        case "--from-data":
          if (i + 1 < args.Length)
          {
            fromData ??= new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            fromData.UnionWith(args[++i].Split(',', StringSplitOptions.RemoveEmptyEntries));
          }
          else
          {
            throw new ArgumentException(
              "--from-data requires a comma-separated list of catalog entry names"
            );
          }
          break;

        case "--to-data":
          if (i + 1 < args.Length)
          {
            toData ??= new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            toData.UnionWith(args[++i].Split(',', StringSplitOptions.RemoveEmptyEntries));
          }
          else
          {
            throw new ArgumentException(
              "--to-data requires a comma-separated list of catalog entry names"
            );
          }
          break;

        case "--only-nodes":
          if (i + 1 < args.Length)
          {
            onlyNodes ??= new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            onlyNodes.UnionWith(args[++i].Split(',', StringSplitOptions.RemoveEmptyEntries));
          }
          else
          {
            throw new ArgumentException(
              "--only-nodes requires a comma-separated list of node names"
            );
          }
          break;

        case "--help":
        case "-h":
          return new ParsedArguments { ShowHelp = true };

        case "--version":
        case "-v":
          return new ParsedArguments { ShowVersion = true };

        default:
          throw new ArgumentException($"Unknown argument: {arg}");
      }
    }

    // Build slice strategy if any slicing flags were provided
    FlowSliceStrategy? sliceStrategy = null;
    if (
      pipelines != null
      || fromNodes != null
      || toNodes != null
      || fromData != null
      || toData != null
      || onlyNodes != null
    )
    {
      sliceStrategy = new FlowSliceStrategy
      {
        Flows = pipelines,
        FromNodes = fromNodes,
        ToNodes = toNodes,
        FromData = fromData,
        ToData = toData,
        OnlyNodes = onlyNodes,
      };
    }

    // Attach slice strategy to options when executing
    if (sliceStrategy != null)
    {
      options.SliceStrategy = sliceStrategy;
    }

    return new ParsedArguments
    {
      ExecuteAll = true,
      Options = options,
      ExportMetadata = exportMetadata,
      MetadataOutputDirectory = metadataOutputDirectory,
    };
  }
}

/// <summary>
/// Result of parsing command-line arguments.
/// </summary>
internal sealed class ParsedArguments
{
  /// <summary>
  /// Whether to execute all pipelines (now always true, with optional slicing).
  /// </summary>
  public bool ExecuteAll { get; init; }

  /// <summary>
  /// Execution options.
  /// </summary>
  public ExecutionOptions? Options { get; init; }

  /// <summary>
  /// Whether to export metadata.
  /// </summary>
  public bool ExportMetadata { get; init; } = true;

  /// <summary>
  /// Metadata output directory override.
  /// </summary>
  public string? MetadataOutputDirectory { get; init; }

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
