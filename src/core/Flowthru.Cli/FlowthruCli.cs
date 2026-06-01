using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Flowthru.Cli;

/// <summary>
/// Standalone entry point for running a Flowthru program from
/// <c>Main</c>. Hosts the DI container, parses flags, dispatches to
/// the requested flow, and renders <see cref="FlowResult"/> outcomes
/// to <see cref="Console.Out"/> / <see cref="Console.Error"/>.
/// </summary>
/// <remarks>
/// <para>
/// Per §1.4, the canonical end-user authoring shape is:
/// <code>
/// public static Task&lt;int&gt; Main(string[] args) =&gt;
///   FlowthruCli.RunStandaloneAsync(args, services =&gt;
///   {
///     services.AddFlowthru(b =&gt;
///     {
///       b.RegisterCatalog&lt;MyCatalog&gt;(_ =&gt; new MyCatalog());
///       b.RegisterFlow&lt;MyCatalog&gt;(catalog =&gt; FlowBuilder.CreateFlow("main", p =&gt; …));
///     });
///   });
/// </code>
/// </para>
/// <para>
/// Exit code semantics: <c>0</c> on flow success, <c>1</c> on a flow
/// failure, <c>2</c> on a usage error (unknown flag, missing flow
/// label, etc.).
/// </para>
/// </remarks>
public static class FlowthruCli
{
  /// <summary>
  /// Build a <see cref="ServiceCollection"/>, run
  /// <paramref name="configureServices"/> against it, parse
  /// <paramref name="args"/>, dispatch to the matching flow, and
  /// return a process exit code.
  /// </summary>
  public static async Task<int> RunStandaloneAsync(
    string[] args,
    Action<IServiceCollection> configureServices,
    CancellationToken cancellationToken = default
  )
  {
    if (args is null) throw new ArgumentNullException(nameof(args));
    if (configureServices is null) throw new ArgumentNullException(nameof(configureServices));

    CliArguments parsed;
    try
    {
      parsed = ArgumentParser.Parse(args);
    }
    catch (ArgumentException ex)
    {
      await Console.Error.WriteLineAsync(ex.Message).ConfigureAwait(false);
      await Console.Error.WriteAsync(ArgumentParser.HelpText).ConfigureAwait(false);
      return 2;
    }

    if (parsed.ShowHelp)
    {
      await Console.Out.WriteAsync(ArgumentParser.HelpText).ConfigureAwait(false);
      return 0;
    }

    var services = new ServiceCollection();
    configureServices(services);

    await using var provider = services.BuildServiceProvider();
    var flowthru = provider.GetService<IFlowthruService>()
      ?? throw new InvalidOperationException(
        "IFlowthruService is not registered. "
        + "Call services.AddFlowthru(b => …) inside the configureServices callback."
      );

    if (parsed.ListFlows)
    {
      foreach (var label in flowthru.RegisteredFlowLabels)
      {
        await Console.Out.WriteLineAsync(label).ConfigureAwait(false);
      }
      return 0;
    }

    // Per §2.4, all flows registered with the same FlowthruService
    // merge into a single DAG. Three dispatch paths, mutually exclusive
    // (the parser rejects ambiguous combinations):
    //   • --from/--to/--only → service.RunAsync(strategy, …) — the new
    //     FlowSliceStrategy algebra with composition and glob wildcards.
    //   • --flow <label>     → service.RunAsync(flowLabel, …) — the
    //     legacy "slice to flow's declared outputs" path.
    //   • neither            → service.RunAsync((string?)null, …) —
    //     full merged DAG.
    // The parser has no --parallelism flag, so parsed.Options always
    // carries the record default (1). Seed it from the host's
    // ExecutionDefaults so a ConfigureExecution(...) / appsettings value
    // takes effect on the CLI path; the other flags (dry-run, depth, …)
    // pass through untouched. Resolving IOptions here also triggers the
    // Parallelism validator before the run starts.
    var defaults = provider.GetService<IOptions<ExecutionDefaults>>()?.Value;
    var options = defaults is null
      ? parsed.Options
      : parsed.Options with { Parallelism = defaults.Parallelism };

    var result = parsed.Slice is not null
      ? await flowthru.RunAsync(parsed.Slice, options, cancellationToken).ConfigureAwait(false)
      : await flowthru.RunAsync(parsed.FlowLabel, options, cancellationToken).ConfigureAwait(false);

    return await RenderResult(result).ConfigureAwait(false);
  }

  private static async Task<int> RenderResult(FlowResult result)
  {
    foreach (var stepResult in result.StepResults)
    {
      switch (stepResult)
      {
        case StepResult.Succeeded s:
          await Console.Out.WriteLineAsync($"✓ {s.StepLabel}").ConfigureAwait(false);
          break;
        case StepResult.Skipped sk:
          await Console.Out
            .WriteLineAsync($"- {sk.StepLabel} (skipped: {sk.Reason})")
            .ConfigureAwait(false);
          break;
        case StepResult.Failed f:
          var report = RuntimeErrorClassifier.Classify(f.Error);
          await Console.Error
            .WriteLineAsync($"✗ {f.StepLabel}: {ConsoleErrorFormatter.Format(report)}")
            .ConfigureAwait(false);
          break;
      }
    }
    return result.IsSuccess ? 0 : 1;
  }
}
