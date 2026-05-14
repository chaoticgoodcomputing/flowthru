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
  /// Shared service-configuration logic. Per Phase 5/8: option
  /// records are exposed on the catalog via
  /// <c>ConfigurationItem&lt;T&gt;</c> and wire into steps as
  /// ordinary inputs — flow factories no longer take a second
  /// <c>FlowConfig</c> parameter.
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
      // UseConfiguration registers the IConfiguration so the Catalog's
      // ConfigurationItem<T> bindings can resolve their sections.
      b.UseConfiguration(configuration);
      b.RegisterCatalog(sp => new Catalog(
        Path.Combine(basePath, "Data"),
        sp.GetRequiredService<IConfiguration>()));

      b.RegisterFlow<Catalog>("DataEngineering", DataEngineeringFlow.Create)
        .WithDescription("Splits iris data into training and test sets with one-hot encoding");

      b.RegisterFlow<Catalog>("DataScience", DataScienceFlow.Create)
        .WithDescription("Trains multi-class logistic regression model for iris classification");
    });

    services.AddLogging(logging =>
    {
      logging.AddConsole();
      logging.SetMinimumLevel(LogLevel.Information);
    });
  }
}
