using Flowthru.Cli;
using Flowthru.Diagnostics;
using Flowthru.Diagnostics.Json;
using Flowthru.Diagnostics.Mermaid;
using Flowthru.Hosting;
using Flowthru.Validation.Runtime;
using KedroSpaceflightsGQL.Data;
using KedroSpaceflightsGQL.Flows.DataProcessing;
using KedroSpaceflightsGQL.Flows.DataScience;
using KedroSpaceflightsGQL.Flows.Ingest;
using KedroSpaceflightsGQL.Flows.Reporting;
using KedroSpaceflightsGQL.Infra.GqlClient;
using KedroSpaceflightsGQL.Infra.GqlServer;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace KedroSpaceflightsGQL;

/// <summary>
/// Main application entry point for the Spaceflights GQL pipeline.
/// </summary>
/// <remarks>
/// This example hosts a HotChocolate GraphQL server in-process using
/// <see cref="TestServer"/> (ASP.NET Core test infrastructure). To point at a real
/// GQL endpoint instead, remove the <c>BuildTestServer()</c> call and replace
/// <c>UseTestServerHandler</c> with a normal <c>BaseAddress</c> in the named
/// <c>HttpClient</c> configuration below.
/// </remarks>
public class Program
{
  /// <summary>
  /// Main entry point for the Spaceflights GQL pipeline CLI.
  /// </summary>
  public static async Task<int> Main(string[] args)
  {
    using var gqlServer = BuildTestServer();
    var gqlHandler = gqlServer.CreateHandler();

    return await FlowthruCli.RunStandaloneAsync(
      args,
      services => ConfigureServices(services, Directory.GetCurrentDirectory(), gqlHandler)
    );
  }

  // ── Test infrastructure (remove for production) ───────────────────────────

  private static TestServer BuildTestServer()
  {
    var host = new HostBuilder()
      .ConfigureWebHost(webBuilder =>
      {
        webBuilder
          .UseTestServer()
          .ConfigureServices(SpaceflightsGqlServer.ConfigureServices)
          .Configure(SpaceflightsGqlServer.Configure);
      })
      .Build();
    host.Start();
    return host.GetTestServer();
  }

  // ── Service configuration (shared with test infrastructure) ───────────────

  public static IServiceProvider ConfigureServices(string? basePath = null)
  {
    var gqlServer = BuildTestServer();
    var gqlHandler = gqlServer.CreateHandler();

    var services = new ServiceCollection();
    services.AddSingleton(gqlServer);
    ConfigureServices(services, basePath ?? Directory.GetCurrentDirectory(), gqlHandler);
    return services.BuildServiceProvider();
  }

  private static void ConfigureServices(
    IServiceCollection services,
    string basePath,
    HttpMessageHandler? gqlHandler
  )
  {
    services
      .AddSpaceflightsClient()
      .ConfigureHttpClient(
        c => c.BaseAddress = new Uri("http://localhost/graphql"),
        b =>
        {
          if (gqlHandler is not null)
          {
            b.ConfigurePrimaryHttpMessageHandler(() => gqlHandler);
          }
        }
      );

    var configuration = new ConfigurationBuilder()
      .SetBasePath(basePath)
      .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
      .AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: false)
      .Build();
    services.AddSingleton<IConfiguration>(configuration);

    services.AddFlowthru(flowthru =>
    {
      // UseConfiguration registers the IConfiguration so the Catalog's
      // ConfigurationItem<T> bindings can resolve their sections. Option
      // records are exposed on the catalog as ordinary inputs — flow
      // factories no longer take a second FlowConfig parameter
      // (Phase 5/8 of the smart-caching RFC).
      flowthru.UseConfiguration(configuration);
      flowthru.RegisterCatalog(sp => new Catalog(
        basePath: System.IO.Path.Combine(basePath, "Data"),
        client: sp.GetRequiredService<ISpaceflightsClient>(),
        configuration: sp.GetRequiredService<IConfiguration>()
      ));

      // Pre-flight inspector for the StrawberryShake client. The probe here is a
      // lightweight no-op success since the in-process GQL server is fully under
      // our control; a production-bound configuration would issue a small healthcheck query.
      flowthru.AddFlowServiceInspector<ISpaceflightsClient>((_, _) =>
        Task.FromResult(Inspect.Pass())
      );

      flowthru.ConfigureMetadata(meta =>
      {
        var metadataPath = System.IO.Path.Combine(basePath, "Metadata");
        meta.AddJsonMetadata(opt => opt.WithOutputDirectory(metadataPath));
        meta.AddMermaidMetadata(opt => opt
          .WithOutputDirectory(metadataPath)
          .WithShowFullDag(false));
      });

      // Ingest: seeds the GQL server from CSV/Excel before DataProcessing runs.
      flowthru
        .RegisterFlow<Catalog, ISpaceflightsClient>("Ingest", IngestFlow.Create)
        .WithDescription("Seeds the GraphQL server with raw company, shuttle, and review data");

      // DataProcessing: reads from GQL server; depends on Ingest having run first
      flowthru
        .RegisterFlow<Catalog>("DataProcessing", DataProcessingFlow.Create)
        .WithDescription("Preprocesses companies and shuttles data");

      flowthru
        .RegisterFlow<Catalog>("DataScience", DataScienceFlow.Create)
        .WithDescription("Trains linear regression model for price prediction");

      flowthru
        .RegisterFlow<Catalog>("Reporting", ReportingFlow.Create)
        .WithDescription("Generates passenger capacity reports and visualizations");
    });

    services.AddLogging(logging =>
    {
      logging.AddConsole();
      logging.SetMinimumLevel(LogLevel.Information);
    });
  }
}
