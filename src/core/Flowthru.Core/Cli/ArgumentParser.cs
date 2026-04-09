using Flowthru.Core.Flows;
using Flowthru.Core.Services.Models;
using Flowthru.Core.Graph;

namespace Flowthru.Core.Cli;

/// <summary>
/// Parses command-line arguments into structured execution requests.
/// </summary>
internal static class ArgumentParser
{
  /// <summary>
  /// Parses command-line arguments into a flow execution request.
  /// </summary>
  /// <param name="args">Command-line arguments</param>
  /// <param name="availableFlows">Available flow names for validation</param>
  /// <returns>Parsed execution request</returns>
  public static ParsedArguments Parse(string[] args, IEnumerable<string> availableFlows)
  {
    var options = new ExecutionOptions();
    var exportMetadata = true;
    string? metadataOutputDirectory = null;

    // Slicing options
    HashSet<string>? flows = null;
    HashSet<string>? from = null;
    HashSet<string>? to = null;
    HashSet<string>? only = null;

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

        case "--flows":
        case "--flow":
          if (i + 1 < args.Length)
          {
            flows ??= new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            flows.UnionWith(args[++i].Split(',', StringSplitOptions.RemoveEmptyEntries));
          }
          else
          {
            throw new ArgumentException("--flows requires a comma-separated list of flow names");
          }
          break;

        case "--from":
          if (i + 1 < args.Length)
          {
            from ??= new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            from.UnionWith(args[++i].Split(',', StringSplitOptions.RemoveEmptyEntries));
          }
          else
          {
            throw new ArgumentException(
              "--from requires a comma-separated list of step or catalog item labels"
            );
          }
          break;

        case "--to":
          if (i + 1 < args.Length)
          {
            to ??= new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            to.UnionWith(args[++i].Split(',', StringSplitOptions.RemoveEmptyEntries));
          }
          else
          {
            throw new ArgumentException(
              "--to requires a comma-separated list of step or catalog item labels"
            );
          }
          break;

        case "--only":
          if (i + 1 < args.Length)
          {
            only ??= new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            only.UnionWith(args[++i].Split(',', StringSplitOptions.RemoveEmptyEntries));
          }
          else
          {
            throw new ArgumentException(
              "--only requires a comma-separated list of step or catalog item labels"
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
    if (flows != null || from != null || to != null || only != null)
    {
      sliceStrategy = new FlowSliceStrategy
      {
        Flows = flows,
        From = from,
        To = to,
        Only = only,
      };
    }

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
  /// Whether to execute all flows (always true, with optional slicing).
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
