using Flowthru.Core.Cli;
using Flowthru.Core.Services;
using Flowthru.Meta;
using Flowthru.Meta.Providers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Minimal.Data;
using Minimal.Flows.Greetings;

namespace Minimal;

/// <summary>
/// Main application entry point for the minimal greeting pipeline.
/// </summary>
public class Program
{
  /// <summary>
  /// Main entry point for the minimal greeting pipeline CLI application.
  /// </summary>
  /// <param name="args">Command-line arguments</param>
  public static Task<int> Main(string[] args) =>
    FlowthruCli.RunStandaloneAsync(
      args,
      services => ConfigureServices(services, Directory.GetCurrentDirectory())
    );

  /// <summary>
  /// Configures services for the application. Used by test infrastructure.
  /// </summary>
  /// <param name="basePath">Optional base path for data files (defaults to current directory)</param>
  public static IServiceProvider ConfigureServices(string? basePath = null)
  {
    var services = new ServiceCollection();
    ConfigureServices(services, basePath ?? Directory.GetCurrentDirectory());
    return services.BuildServiceProvider();
  }

  /// <summary>
  /// Shared service configuration logic.
  /// </summary>
  private static void ConfigureServices(IServiceCollection services, string basePath)
  {
    services.AddFlowthru(flowthru =>
    {
      flowthru.UseConfiguration(opts => opts.ConfigurationPath = basePath);
      flowthru.RegisterCatalog(_ => new Catalog(basePath));
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

      // Register the greetings pipeline
      flowthru
        .RegisterFlow(label: "Greetings", flow: GreetingsFlow.Create)
        .WithDescription(
          "A minimal pipeline demonstrating name transformation into multiple greeting formats"
        );
    });

    services.AddLogging(logging =>
    {
      logging.AddConsole();
      logging.SetMinimumLevel(LogLevel.Information);
    });
  }
}
