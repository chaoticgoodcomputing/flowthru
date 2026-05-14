using Flowthru.Cli;
using Flowthru.Diagnostics;
using Flowthru.Diagnostics.Json;
using Flowthru.Diagnostics.Mermaid;
using Flowthru.Hosting;
using Flowthru.Step.Python;
using KedroIrisPython.Data;
using KedroIrisPython.Flows.DataEngineering;
using KedroIrisPython.Flows.DataScience;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace KedroIrisPython;

/// <summary>
/// Main application entry point for the Iris pipeline with Python nodes.
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
    services.AddSingleton<IConfiguration>(configuration);

    services.AddFlowthru(flowthru =>
    {
      flowthru.UseConfiguration(configuration);
      flowthru.RegisterCatalog(sp => new Catalog(
        Path.Combine(basePath, "Data"),
        sp.GetRequiredService<IConfiguration>()));

      flowthru.ConfigureMetadata(meta =>
      {
        var metadataPath = Path.Combine(basePath, "Metadata");
        meta.AddJsonMetadata(opt => opt.WithOutputDirectory(metadataPath));
        meta.AddMermaidMetadata(opt => opt.WithOutputDirectory(metadataPath));
      });

      flowthru.UsePython(python =>
      {
        python.ModuleSearchPaths.Add(basePath);
        python.ModuleSearchPaths.Add(outputPath);
        python.VenvPath = outputPath;
      });

      flowthru
        .RegisterFlow<Catalog, IPythonExecutor>("DataEngineering", DataEngineeringFlow.Create)
        .WithDescription("Splits iris data into training and test sets using Python");

      flowthru
        .RegisterFlow<Catalog, IPythonExecutor>("DataScience", DataScienceFlow.Create)
        .WithDescription("Trains multi-class logistic regression model and evaluates predictions using Python");
    });
  }
}
