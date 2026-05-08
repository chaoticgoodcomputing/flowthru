using Flowthru.Cli;
using Flowthru.Hosting;
using KedroIrisFUnit.Data;
using KedroIrisFUnit.Flows.DataEngineering;
using KedroIrisFUnit.Flows.DataScience;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace KedroIrisFUnit;

/// <summary>
/// Main entry point for the Iris classification pipeline. Phase 7's
/// done-criterion: this program runs end-to-end against
/// <c>Flowthru.Core</c> + <c>FUnit</c> only — no extension packages —
/// exercising schemas, catalog with attribute-driven items,
/// canonical <c>Create() => Func</c> step shapes,
/// <c>FlowBuilder</c>, and the hosting + CLI surface.
/// </summary>
public class Program
{
  public static Task<int> Main(string[] args) =>
    FlowthruCli.RunStandaloneAsync(
      args,
      services => ConfigureServices(services, basePath: Directory.GetCurrentDirectory())
    );

  /// <summary>
  /// Build a service provider for tests / external host adapters.
  /// </summary>
  public static IServiceProvider ConfigureServices(string? basePath = null)
  {
    var services = new ServiceCollection();
    ConfigureServices(services, basePath ?? Directory.GetCurrentDirectory());
    return services.BuildServiceProvider();
  }

  /// <summary>
  /// Shared service-configuration logic. Per Phase 4: catalogs are
  /// DI-resolvable values; flows declare which ones they need by
  /// parameter list and the framework resolves each from DI before
  /// invoking the factory.
  /// </summary>
  public static void ConfigureServices(IServiceCollection services, string basePath)
  {
    var configuration = new ConfigurationBuilder()
      .SetBasePath(basePath)
      .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
      .AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: false)
      .Build();
    services.AddSingleton<IConfiguration>(configuration);

    services.AddFlowthru(b =>
    {
      b.RegisterCatalog(_ => new Catalog(Path.Combine(basePath, "Data")));
      b.RegisterCatalog(sp => new FlowConfig(sp.GetRequiredService<IConfiguration>()));

      b.RegisterFlow<Catalog, FlowConfig>("DataEngineering", DataEngineeringFlow.Create)
        .WithDescription("Splits iris data into training and test sets with one-hot encoding");

      b.RegisterFlow<Catalog, FlowConfig>("DataScience", DataScienceFlow.Create)
        .WithDescription("Trains multi-class logistic regression model for iris classification");
    });

    services.AddLogging(logging =>
    {
      logging.AddConsole();
      logging.SetMinimumLevel(LogLevel.Information);
    });
  }
}
