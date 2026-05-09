using Flowthru.Cli;
using Flowthru.Hosting;
using KedroIris.Data;
using KedroIris.Flows.DataEngineering;
using KedroIris.Flows.DataScience;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace KedroIris;

/// <summary>
/// Main application entry point for the Iris classification pipeline.
/// </summary>
public class Program
{
  public static Task<int> Main(string[] args) =>
    FlowthruCli.RunStandaloneAsync(
      args,
      services => ConfigureServices(services, basePath: Directory.GetCurrentDirectory())
    );

  public static IServiceProvider ConfigureServices(string? basePath = null)
  {
    var services = new ServiceCollection();
    ConfigureServices(services, basePath ?? Directory.GetCurrentDirectory());
    return services.BuildServiceProvider();
  }

  private static void ConfigureServices(IServiceCollection services, string basePath)
  {
    var configuration = new ConfigurationBuilder()
      .SetBasePath(basePath)
      .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
      .AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: false)
      .Build();
    services.AddSingleton<IConfiguration>(configuration);

    services.AddFlowthru(flowthru =>
    {
      flowthru.RegisterCatalog(_ => new Catalog(Path.Combine(basePath, "Data")));
      flowthru.RegisterCatalog(sp => new FlowConfig(sp.GetRequiredService<IConfiguration>()));

      flowthru
        .RegisterFlow<Catalog, FlowConfig>("DataEngineering", DataEngineeringFlow.Create)
        .WithDescription("Splits iris data into training and test sets with one-hot encoding");

      flowthru
        .RegisterFlow<Catalog, FlowConfig>("DataScience", DataScienceFlow.Create)
        .WithDescription("Trains multi-class logistic regression model for iris classification");
    });

    services.AddLogging(logging =>
    {
      logging.AddConsole();
      logging.SetMinimumLevel(LogLevel.Information);
    });
  }
}
