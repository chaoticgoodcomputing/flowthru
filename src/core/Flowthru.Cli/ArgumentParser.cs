using Flowthru.Flow;

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
///   <item><c>--flow &lt;label&gt;</c> — name of the flow to run (slice to its declared outputs).</item>
///   <item><c>--from &lt;labels&gt;</c> — slice downstream from the given step/item labels (comma-separated; repeatable).</item>
///   <item><c>--to &lt;labels&gt;</c> — slice upstream to the given step/item labels (comma-separated; repeatable).</item>
///   <item><c>--only &lt;labels&gt;</c> — exactly the named steps / item producers (comma-separated; repeatable).</item>
///   <item><c>--exclude &lt;labels&gt;</c> — drop matching steps from the slice
///     (comma-separated; repeatable; supports the <c>flows:</c> prefix).</item>
///   <item><c>--dry-run</c> — pass <see cref="DryRunOption.On"/>.</item>
///   <item><c>--validation-depth &lt;none|shallow|deep&gt;</c> — pass through.</item>
///   <item><c>--continue-on-error</c> — set <see cref="ExecutionOptions.StopOnFirstError"/> = false.</item>
///   <item><c>--no-cache</c> — set <see cref="ExecutionOptions.BypassCacheReads"/> = true; skip the
///     pre-flight cache plan (every cacheable step runs) but still record post-run composites
///     for the next invocation.</item>
///   <item><c>--list</c> — print every registered flow label and exit.</item>
///   <item><c>--help</c>, <c>-h</c> — print usage and exit.</item>
/// </list>
/// </para>
/// <para>
/// <strong>Slice composition.</strong> Multiple slicing flags compose
/// via <see cref="FlowSliceStrategy.And"/> (intersection). A repeated
/// flag composes via <see cref="FlowSliceStrategy.Or"/> (union) within
/// its primitive — e.g.
/// <c>--from A --from B</c> ≡ <c>From(A, B)</c>;
/// <c>--from A --to Z</c> ≡ <c>And(From(A), To(Z))</c>.
/// Labels in any slicing flag may be step labels OR item labels and
/// may use glob wildcards (<c>*</c>, <c>?</c>).
/// </para>
/// <para>
/// <strong>Exclusions.</strong> <c>--exclude</c> drops matching steps
/// from the rest of the slice. It composes as
/// <c>And(rest, Not(Or(...)))</c>: every <c>--exclude</c> pattern
/// becomes one disjunct inside a single <see cref="FlowSliceStrategy.Not"/>.
/// With no other slice flag the implicit <em>rest</em> is
/// <see cref="FlowSliceStrategy.All"/>. Patterns can use the
/// <c>flows:Label</c> prefix to match by flow rather than by step/item
/// label — for example <c>--exclude flows:Ingest</c> drops every step
/// attributed to the <c>Ingest</c> flow.
/// </para>
/// <para>
/// <strong>Flag mixing.</strong> <c>--flow</c> may not be combined with
/// <c>--from</c>/<c>--to</c>/<c>--only</c>/<c>--exclude</c> — the two
/// paths have different slicing semantics. The parser rejects the
/// combination at the boundary.
/// </para>
/// </remarks>
public sealed record CliArguments(
  string? FlowLabel,
  FlowSliceStrategy? Slice,
  ExecutionOptions Options,
  bool ListFlows,
  bool ShowHelp
)
{
  public static CliArguments Default { get; } = new(
    FlowLabel: null,
    Slice: null,
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
    var fromLabels = new List<string>();
    var toLabels = new List<string>();
    var onlyLabels = new List<string>();
    var excludePatterns = new List<string>();
    var dryRun = DryRunOption.Off;
    var depth = ValidationDepth.Shallow;
    var stopOnFirstError = true;
    var bypassCacheReads = false;
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
        case "--from":
          if (i + 1 >= args.Count)
            throw new ArgumentException("--from requires a value (comma-separated label list).");
          fromLabels.AddRange(SplitLabels(args[++i]));
          break;
        case "--to":
          if (i + 1 >= args.Count)
            throw new ArgumentException("--to requires a value (comma-separated label list).");
          toLabels.AddRange(SplitLabels(args[++i]));
          break;
        case "--only":
          if (i + 1 >= args.Count)
            throw new ArgumentException("--only requires a value (comma-separated label list).");
          onlyLabels.AddRange(SplitLabels(args[++i]));
          break;
        case "--exclude":
          if (i + 1 >= args.Count)
            throw new ArgumentException(
              "--exclude requires a value (comma-separated label list). "
              + "Use the flows: prefix to match by flow label (e.g. --exclude flows:Ingest)."
            );
          excludePatterns.AddRange(SplitLabels(args[++i]));
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
        case "--no-cache":
          bypassCacheReads = true;
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

    var slice = BuildSliceStrategy(fromLabels, toLabels, onlyLabels, excludePatterns);
    if (flowLabel is not null && slice is not null)
    {
      throw new ArgumentException(
        "--flow cannot be combined with --from / --to / --only / --exclude. "
        + "Use one or the other."
      );
    }

    return new CliArguments(
      flowLabel,
      slice,
      new ExecutionOptions
      {
        DryRun = dryRun,
        ValidationDepth = depth,
        StopOnFirstError = stopOnFirstError,
        BypassCacheReads = bypassCacheReads,
      },
      listFlows,
      showHelp
    );
  }

  /// <summary>
  /// Build a slice strategy from the collected From/To/Only/Exclude
  /// labels. Each primitive (when populated) becomes a
  /// <see cref="FlowSliceStrategy.From"/> /
  /// <see cref="FlowSliceStrategy.To"/> /
  /// <see cref="FlowSliceStrategy.Only"/>; when more than one primitive
  /// is populated they compose via <see cref="FlowSliceStrategy.And"/>
  /// (intersection). Exclusions wrap the rest in
  /// <c>And(rest, Not(Or(...)))</c>: every <c>--exclude</c> pattern is
  /// a sub-strategy folded into a single <see cref="FlowSliceStrategy.Not"/>.
  /// With no other slice flags, the implicit <em>rest</em> is
  /// <see cref="FlowSliceStrategy.All"/>. All-empty returns <c>null</c>
  /// — the caller treats that as "no slice".
  /// </summary>
  private static FlowSliceStrategy? BuildSliceStrategy(
    IReadOnlyList<string> from,
    IReadOnlyList<string> to,
    IReadOnlyList<string> only,
    IReadOnlyList<string> excludes
  )
  {
    var primitives = new List<FlowSliceStrategy>();
    if (from.Count > 0)
    {
      primitives.Add(new FlowSliceStrategy.From(from.ToHashSet(StringComparer.Ordinal)));
    }
    if (to.Count > 0)
    {
      primitives.Add(new FlowSliceStrategy.To(to.ToHashSet(StringComparer.Ordinal)));
    }
    if (only.Count > 0)
    {
      primitives.Add(new FlowSliceStrategy.Only(only.ToHashSet(StringComparer.Ordinal)));
    }
    var rest = primitives.Count switch
    {
      0 => null,
      1 => primitives[0],
      _ => primitives.Aggregate((acc, next) => new FlowSliceStrategy.And(acc, next)),
    };

    if (excludes.Count == 0)
    {
      return rest;
    }

    // Build a sub-strategy per exclude pattern, then fold into a single
    // Not via Or composition. The rest-side defaults to All when no
    // other slice flag is present so the user can write
    // `--exclude flows:Ingest` without --from/--to/--only.
    var excludeStrategies = excludes
      .Select(ResolveExcludePattern)
      .ToList();
    var excludeUnion = excludeStrategies.Count == 1
      ? excludeStrategies[0]
      : excludeStrategies.Aggregate((acc, next) => new FlowSliceStrategy.Or(acc, next));
    var negation = new FlowSliceStrategy.Not(excludeUnion);
    return new FlowSliceStrategy.And(rest ?? new FlowSliceStrategy.All(), negation);
  }

  /// <summary>
  /// Map a single <c>--exclude</c> pattern to the matching
  /// <see cref="FlowSliceStrategy"/> sub-strategy. A bare pattern
  /// resolves to <see cref="FlowSliceStrategy.Only"/> (label glob).
  /// The <c>flows:</c> prefix dispatches to
  /// <see cref="FlowSliceStrategy.Flows"/> instead, matching by flow
  /// label rather than step/item label.
  /// </summary>
  private static FlowSliceStrategy ResolveExcludePattern(string pattern)
  {
    if (pattern.StartsWith(FlowsPrefix, StringComparison.Ordinal))
    {
      var stripped = pattern.Substring(FlowsPrefix.Length);
      return new FlowSliceStrategy.Flows(
        new HashSet<string>(new[] { stripped }, StringComparer.Ordinal)
      );
    }
    return new FlowSliceStrategy.Only(
      new HashSet<string>(new[] { pattern }, StringComparer.Ordinal)
    );
  }

  private const string FlowsPrefix = "flows:";

  private static IEnumerable<string> SplitLabels(string raw) =>
    raw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

  /// <summary>The <c>--help</c> output as a single string.</summary>
  public const string HelpText =
    "Usage: <program> [options]\n"
    + "\n"
    + "Options:\n"
    + "  --flow <label>                   Name of the flow to run (slice to its outputs)\n"
    + "  --from <labels>                  Slice downstream from steps/items (comma-separated)\n"
    + "  --to <labels>                    Slice upstream to steps/items (comma-separated)\n"
    + "  --only <labels>                  Exactly these steps / item producers (comma-separated)\n"
    + "  --exclude <labels>               Drop matching steps from the slice (comma-separated;\n"
    + "                                   repeatable). Use the flows: prefix to match by flow\n"
    + "                                   label, e.g. --exclude flows:Ingest\n"
    + "  --list                           List every registered flow and exit\n"
    + "  --dry-run                        Skip transform execution; validate only\n"
    + "  --validation-depth <level>       none | shallow (default) | deep\n"
    + "  --continue-on-error              Run independent steps after a failure\n"
    + "  --no-cache                       Skip the cache plan (every cacheable step runs);\n"
    + "                                   the manifest is still updated post-run\n"
    + "  --help, -h                       Show this message\n"
    + "\n"
    + "Slice flags accept step labels OR item labels and may use glob wildcards (*, ?).\n"
    + "Multiple slice flags compose via intersection; repeated flags via union.\n"
    + "--exclude composes against the rest of the slice as And(rest, Not(union-of-excludes));\n"
    + "with no other slice flag, the implicit rest is the full DAG.\n"
    + "--flow cannot be combined with --from / --to / --only / --exclude.\n";
}
