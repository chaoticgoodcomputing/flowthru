using Flowthru.Cli;
using Flowthru.Diagnostics;
using Flowthru.Hosting;
using Flowthru.Step.Python;
using MnistDistributed.Data;
using MnistDistributed.Flows.Train;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace MnistDistributed;

/// <summary>
/// Entry point for the MNIST-shaped distributed-training example.
/// Wires <see cref="TorchrunLauncher"/> as the
/// <see cref="IPythonLauncher"/> singleton so the training step spawns
/// N python workers via torchrun instead of a single python process.
/// </summary>
/// <remarks>
/// This example is intentionally a tracer bullet for the slice-5
/// rank-aware worker work — see README.md for the catalogue of
/// failure modes it currently reproduces.
/// </remarks>
public class Program
{
  /// <summary>Number of ranks. Single-node, all CPU via gloo.</summary>
  private const int NProcPerNode = 2;

  public static Task<int> Main(string[] args) =>
    FlowthruCli.RunStandaloneAsync(
      args,
      services =>
        ConfigureServices(
          services,
          Directory.GetCurrentDirectory(),
          AppDomain.CurrentDomain.BaseDirectory
        )
    );

  public static IServiceProvider ConfigureServices(
    string? basePath = null,
    string? outputPath = null
  )
  {
    var services = new ServiceCollection();
    ConfigureServices(
      services,
      basePath ?? Directory.GetCurrentDirectory(),
      outputPath ?? AppDomain.CurrentDomain.BaseDirectory
    );
    return services.BuildServiceProvider();
  }

  private static void ConfigureServices(
    IServiceCollection services,
    string basePath,
    string outputPath
  )
  {
    services.AddLogging(logging =>
    {
      logging.AddConsole();
      logging.SetMinimumLevel(LogLevel.Information);
    });

    var configuration = new ConfigurationBuilder()
      .SetBasePath(basePath)
      .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
      .AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: false)
      .Build();
    services.AddSingleton<IConfiguration>(configuration);

    // Register the distributed launcher *before* AddFlowthru / UsePython
    // so the TryAddSingleton<IPythonLauncher> in PythonFlowthruBuilderExtensions
    // sees an existing registration and yields. Launcher selection is a
    // per-executor concern — the executor owns its launcher instance.
    // RedirectsFlag set so non-rank-0 stdout/stderr go to per-rank
    // log files rather than the parent — necessary to avoid those
    // ranks corrupting the JSON protocol stream rank 0 owns. (Doesn't
    // actually rescue this example; rank 1 still loses the stdin race
    // before printing anything. See README's "Reproduced failures".)
    services.AddSingleton<IPythonLauncher>(new TorchrunLauncher
    {
      NProcPerNode = NProcPerNode,
      RedirectsFlag = "1:3",
    });

    services.AddFlowthru(flowthru =>
    {
      flowthru.UseConfiguration(configuration);
      flowthru.RegisterCatalog(sp => new Catalog(
        Path.Combine(basePath, "Data"),
        sp.GetRequiredService<IConfiguration>()));

      flowthru.UsePython(python =>
      {
        python.ModuleSearchPaths.Add(basePath);
        python.ModuleSearchPaths.Add(outputPath);
        python.VenvPath = outputPath;
      });

      flowthru.ConfigureMetadata(meta =>
      {
        var metadataPath = Path.Combine(basePath, "Metadata");
        meta.AddJsonMetadata(opt => opt.WithOutputDirectory(metadataPath));
        meta.AddMermaidMetadata(opt => opt
          .WithOutputDirectory(metadataPath)
          .WithShowFullDag(false));
      });

      flowthru
        .RegisterFlow<Catalog, IPythonExecutor>("Train", TrainFlow.Create)
        .WithDescription("Distributed CNN training via TorchrunLauncher.");
    });
  }
}
