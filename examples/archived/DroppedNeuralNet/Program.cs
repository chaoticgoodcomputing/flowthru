using DroppedNeuralNet.Data;
using DroppedNeuralNet.Flows.DataPrep;
using DroppedNeuralNet.Flows.Exploration;
using DroppedNeuralNet.Flows.Solver;
using DroppedNeuralNet.Flows.Validation;
using Flowthru.Core.Cli;
using Flowthru.Core.Services;
using Flowthru.Extensions.Python;
using Flowthru.Extensions.Python.Services;
using Flowthru.Meta;
using Flowthru.Meta.Providers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DroppedNeuralNet;

/// <summary>
/// Entry point for the DroppedNeuralNet example.
///
/// Puzzle: 97 scrambled PyTorch linear-layer pieces must be arranged into the
/// permutation that reproduces a recorded set of predictions.
///
/// Flows:
///   DataPrep    — Python: ingest .pth blobs and classify each piece by tensor dimensions.
///   Exploration — C# + Python: enumerate legal pairings, score via Frobenius norms,
///                 run Hungarian assignment, rank orderings via activation chaining.
///   Validation  — Python: diagnostic probes for pairing and ordering quality.
///   Solver      — Python: forward-pass validate ranked candidates; emit the solution.
///
/// Usage:
///   dotnet run                                       # run all four flows in order
///   dotnet run -- --flows DataPrep                   # ingest + classify only
///   dotnet run -- --flows Exploration                # pairing analysis (requires DataPrep)
///   dotnet run -- --flows Validation                 # diagnostics (requires Exploration)
///   dotnet run -- --flows Solver                     # validate (requires Exploration)
///
/// The pieces directory defaults to ~/Downloads/dropped-a-neural-net/pieces.
/// Override with the PIECES_DIR environment variable.
/// </summary>
public class Program
{
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

    services.AddFlowthru(
      configuration,
      flowthru =>
      {
        flowthru.RegisterCatalog(_ => new Catalog(basePath: Path.Combine(basePath, "Data")));

        flowthru.ConfigureMetadata(meta =>
        {
          var metadataPath = Path.Combine(basePath, "Metadata");
          meta.AddProvider<JsonMetadataProvider, JsonMetadataProviderBuilder>(json =>
              json.WithOutputDirectory(metadataPath)
            )
            .AddProvider<MermaidMetadataProvider, MermaidMetadataProviderBuilder>(mermaid =>
              mermaid.WithOutputDirectory(metadataPath)
            );
        });

        flowthru.UsePython(python =>
        {
          python.ModuleSearchPaths.Add(basePath);
          python.ModuleSearchPaths.Add(outputPath);
          python.VenvPath = outputPath;
        });

        // Eagerly initialize the Python runtime before pipeline registration.
        // NOTE: Do not dispose — singleton instances must stay alive.
        var tempProvider = flowthru.Services.BuildServiceProvider();
        tempProvider.GetRequiredService<Flowthru.Extensions.Python.Execution.IPythonExecutor>();

        flowthru
          .RegisterFlow(label: "DataPrep", flow: DataPrepFlow.Create)
          .WithDescription(
            "Ingest .pth blobs and classify each piece by tensor dimensions (Python)"
          );

        flowthru
          .RegisterFlow(label: "Exploration", flow: ExplorationFlow.Create)
          .WithDescription(
            "Enumerate legal pairings (C#), score via Frobenius norms, run Hungarian assignment, rank orderings via activation chaining (C# + Python)"
          );

        flowthru
          .RegisterFlow(label: "Validation", flow: ValidationFlow.Create)
          .WithDescription(
            "Diagnostic probes: fixed-order baseline, ProductNorm signal stats, per-candidate errors (Python)"
          );

        flowthru
          .RegisterFlow(label: "Solver", flow: SolverFlow.Create)
          .WithDescription(
            "Forward-pass validate ranked candidate permutations; emit the solution (Python)"
          );
      }
    );
  }
}
