namespace Flowthru.Cli;

/// <summary>
/// Parses the small set of flags <see cref="FlowthruCli"/> understands.
/// Deliberately scoped — the CLI is meant as the bare entry point, not a
/// full subcommand framework. End users with richer needs build their
/// own host on top of <see cref="IFlowthruService"/>.
/// </summary>
/// <remarks>
/// <para>
/// Supported flags:
/// <list type="bullet">
///   <item><c>--flow &lt;label&gt;</c> — name of the flow to run (required if more than one is registered).</item>
///   <item><c>--dry-run</c> — pass <see cref="DryRunOption.On"/>.</item>
///   <item><c>--validation-depth &lt;none|shallow|deep&gt;</c> — pass through.</item>
///   <item><c>--continue-on-error</c> — set <see cref="ExecutionOptions.StopOnFirstError"/> = false.</item>
///   <item><c>--list</c> — print every registered flow label and exit.</item>
///   <item><c>--help</c>, <c>-h</c> — print usage and exit.</item>
/// </list>
/// </para>
/// </remarks>
public sealed record CliArguments(
  string? FlowLabel,
  ExecutionOptions Options,
  bool ListFlows,
  bool ShowHelp
)
{
  public static CliArguments Default { get; } = new(
    FlowLabel: null,
    Options: ExecutionOptions.Default,
    ListFlows: false,
    ShowHelp: false
  );
}

/// <summary>
/// Static parser for <see cref="CliArguments"/>. Throws
/// <see cref="ArgumentException"/> on unrecognized or malformed flags
/// — the CLI catches the exception and renders the help text.
/// </summary>
public static class ArgumentParser
{
  /// <summary>Parse <paramref name="args"/> into a <see cref="CliArguments"/>.</summary>
  public static CliArguments Parse(IReadOnlyList<string> args)
  {
    if (args is null) throw new ArgumentNullException(nameof(args));

    string? flowLabel = null;
    var dryRun = DryRunOption.Off;
    var depth = ValidationDepth.Shallow;
    var stopOnFirstError = true;
    var listFlows = false;
    var showHelp = false;

    for (var i = 0; i < args.Count; i++)
    {
      switch (args[i])
      {
        case "--flow":
          if (i + 1 >= args.Count)
            throw new ArgumentException("--flow requires a value (the flow label).");
          flowLabel = args[++i];
          break;
        case "--dry-run":
          dryRun = DryRunOption.On;
          break;
        case "--validation-depth":
          if (i + 1 >= args.Count)
            throw new ArgumentException("--validation-depth requires a value (none|shallow|deep).");
          depth = args[++i].ToLowerInvariant() switch
          {
            "none" => ValidationDepth.None,
            "shallow" => ValidationDepth.Shallow,
            "deep" => ValidationDepth.Deep,
            var other => throw new ArgumentException(
              $"Unknown validation depth '{other}'. Expected one of: none, shallow, deep."
            ),
          };
          break;
        case "--continue-on-error":
          stopOnFirstError = false;
          break;
        case "--list":
          listFlows = true;
          break;
        case "--help":
        case "-h":
          showHelp = true;
          break;
        default:
          throw new ArgumentException($"Unknown argument '{args[i]}'.");
      }
    }

    return new CliArguments(
      flowLabel,
      new ExecutionOptions
      {
        DryRun = dryRun,
        ValidationDepth = depth,
        StopOnFirstError = stopOnFirstError,
      },
      listFlows,
      showHelp
    );
  }

  /// <summary>The <c>--help</c> output as a single string.</summary>
  public const string HelpText =
    "Usage: <program> [options]\n"
    + "\n"
    + "Options:\n"
    + "  --flow <label>                   Name of the flow to run\n"
    + "  --list                           List every registered flow and exit\n"
    + "  --dry-run                        Skip transform execution; validate only\n"
    + "  --validation-depth <level>       none | shallow (default) | deep\n"
    + "  --continue-on-error              Run independent steps after a failure\n"
    + "  --help, -h                       Show this message\n";
}
