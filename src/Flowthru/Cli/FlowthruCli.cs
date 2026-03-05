using System.Reflection;
using Flowthru.Pipelines;
using Flowthru.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Flowthru.Cli;

/// <summary>
/// Command-line interface wrapper for IFlowthruService.
/// </summary>
/// <remarks>
/// <para>
/// FlowthruCli provides a thin CLI layer over the core IFlowthruService.
/// It handles:
/// - Command-line argument parsing
/// - Help/version display
/// - Result formatting
/// - Exit code generation
/// </para>
/// <para>
/// The CLI delegates all business logic to IFlowthruService, making the
/// service layer testable and reusable in non-CLI scenarios.
/// </para>
/// </remarks>
public sealed class FlowthruCli
{
  private readonly IFlowthruService _service;
  private readonly ILogger<FlowthruCli> _logger;
  private readonly TextWriter _output;

  /// <summary>
  /// Initializes a new CLI instance.
  /// </summary>
  /// <param name="service">Flowthru service</param>
  /// <param name="logger">Logger instance</param>
  /// <param name="output">Output writer (defaults to Console.Out)</param>
  public FlowthruCli(
    IFlowthruService service,
    ILogger<FlowthruCli> logger,
    TextWriter? output = null
  )
  {
    _service = service;
    _logger = logger;
    _output = output ?? Console.Out;
  }

  /// <summa standalone Flowthru CLI application with automatic service provider lifecycle management.
  /// </summary>
  /// <remarks>
  /// <para>
  /// This is the recommended entry point for standalone console applications using Flowthru.
  /// It manages the ServiceProvider lifecycle automatically, ensuring proper disposal of
  /// logging providers and other resources so the process exits cleanly after pipeline completion.
  /// </para>
  /// <para>
  /// For applications that integrate Flowthru into an existing DI container (e.g., ASP.NET Core),
  /// use the standard constructor and let the host application manage the ServiceProvider lifecycle.
  /// </para>
  /// </remarks>
  /// <param name="args">Command-line arguments</param>
  /// <param name="configure">Configuration callback to register pipelines and services</param>
  /// <param name="cancellationToken">Cancellation token</param>
  /// <returns>Exit code (0 for success, non-zero for errors)</returns>
  public static async Task<int> RunStandaloneAsync(
    string[] args,
    Action<IServiceCollection> configure,
    CancellationToken cancellationToken = default
  )
  {
    var services = new ServiceCollection();
    configure(services);
    var serviceProvider = services.BuildServiceProvider();

    try
    {
      var service = serviceProvider.GetRequiredService<IFlowthruService>();
      var logger = serviceProvider.GetRequiredService<ILogger<FlowthruCli>>();
      var cli = new FlowthruCli(service, logger);

      return await cli.RunAsync(args, cancellationToken);
    }
    finally
    {
      // Dispose ServiceProvider to release resources (logging providers, etc.)
      // This ensures the process exits cleanly without hanging
      if (serviceProvider is IAsyncDisposable asyncDisposable)
      {
        await asyncDisposable.DisposeAsync();
      }
      else if (serviceProvider is IDisposable disposable)
      {
        disposable.Dispose();
      }
    }
  }

  /// <summary>
  /// Runs the CLI with the specified arguments.
  /// </summary>
  /// <param name="args">Command-line arguments</param>
  /// <param name="cancellationToken">Cancellation token</param>
  /// <returns>Exit code (0 for success, non-zero for errors)</returns>
  public async Task<int> RunAsync(string[] args, CancellationToken cancellationToken = default)
  {
    try
    {
      // Parse arguments
      var parsed = ArgumentParser.Parse(args, _service.PipelineNames);

      // Handle special commands
      if (parsed.ShowHelp)
      {
        ShowHelp();
        return 0;
      }

      if (parsed.ShowVersion)
      {
        ShowVersion();
        return 0;
      }

      if (parsed.Error != null)
      {
        await _output.WriteLineAsync($"Error: {parsed.Error}");
        await _output.WriteLineAsync();
        ShowUsage();
        return 1;
      }

      // Execute unified pipeline (with optional slicing)
      var result = await _service.ExecutePipelineAsync(
        parsed.Options,
        parsed.ExportMetadata,
        parsed.MetadataOutputDirectory,
        cancellationToken
      );

      // Format and display results
      FormatResult(result);

      // Return exit code based on success
      return result.Success ? 0 : 1;
    }
    catch (OperationCanceledException)
    {
      _logger.LogWarning("Operation cancelled by user");
      return 130; // Standard exit code for SIGINT
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Unhandled exception during pipeline execution");
      await _output.WriteLineAsync($"Fatal error: {ex.Message}");
      return 1;
    }
  }

  /// <summary>
  /// Displays help message.
  /// </summary>
  private void ShowHelp()
  {
    _output.WriteLine("Flowthru - Type-safe data engineering pipelines for .NET");
    _output.WriteLine();
    ShowUsage();
    _output.WriteLine();
    _output.WriteLine("Options:");
    _output.WriteLine("  --dry-run              Validate without executing nodes");
    _output.WriteLine("  --no-metadata          Disable metadata export");
    _output.WriteLine("  --metadata-output DIR  Specify metadata output directory");
    _output.WriteLine("  -h, --help             Show this help message");
    _output.WriteLine("  -v, --version          Show version information");
    _output.WriteLine();
    _output.WriteLine("Pipeline Slicing:");
    _output.WriteLine("  --pipelines NAMES      Filter to specific pipelines by name");
    _output.WriteLine(
      "  --from-nodes NODES     Start from nodes, include all downstream dependents"
    );
    _output.WriteLine("  --to-nodes NODES       End at nodes, include all upstream dependencies");
    _output.WriteLine("  --from-data ENTRIES    Start from data consumers, include all downstream");
    _output.WriteLine("  --to-data ENTRIES      End at data producers, include all upstream");
    _output.WriteLine(
      "  --only-nodes NODES     Execute only these nodes (auto-include dependencies)"
    );
    _output.WriteLine(
      "  --tags TAGS            Filter to nodes with ALL specified tags (AND logic)"
    );
    _output.WriteLine();
    _output.WriteLine("  Multiple slicing options compose via intersection.");
    _output.WriteLine(
      "  Use comma-separated values: --pipelines DataScience --tags feature,training"
    );
    _output.WriteLine();
    _output.WriteLine("Available Pipelines:");
    foreach (var name in _service.PipelineNames.OrderBy(n => n))
    {
      var metadata = _service.GetPipelineMetadata(name);
      _output.WriteLine($"  {name, -20} {metadata.Description ?? "(no description)"}");
    }
  }

  /// <summary>
  /// Displays usage message.
  /// </summary>
  private void ShowUsage()
  {
    _output.WriteLine("Usage: flowthru [options]");
    _output.WriteLine("       flowthru --help");
    _output.WriteLine("       flowthru --version");
  }

  /// <summary>
  /// Displays version information.
  /// </summary>
  private void ShowVersion()
  {
    var assembly = Assembly.GetExecutingAssembly();
    var version =
      assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
      ?? assembly.GetName().Version?.ToString()
      ?? "unknown";

    _output.WriteLine($"Flowthru v{version}");
  }

  /// <summary>
  /// Formats pipeline execution results.
  /// </summary>
  /// <param name="result">Pipeline execution result</param>
  private void FormatResult(PipelineResult result)
  {
    _output.WriteLine();
    _output.WriteLine("═══════════════════════════════════════════════════════════");
    _output.WriteLine($"Pipeline: {result.PipelineName ?? "(merged)"}");
    _output.WriteLine($"Status: {(result.Success ? "✓ SUCCESS" : "✗ FAILED")}");
    _output.WriteLine($"Duration: {result.ExecutionTime:hh\\:mm\\:ss\\.fff}");
    _output.WriteLine($"Nodes: {result.NodeResults.Count} executed");

    if (!result.Success)
    {
      _output.WriteLine();
      _output.WriteLine("Failed Nodes:");
      foreach (var (label, nodeResult) in result.NodeResults.Where(n => !n.Value.Success))
      {
        _output.WriteLine($"  - {label}: {nodeResult.Exception?.Message ?? "Unknown error"}");
      }
    }

    _output.WriteLine("═══════════════════════════════════════════════════════════");
  }
}
