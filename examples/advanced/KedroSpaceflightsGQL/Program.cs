using Flowthru.Core.Cli;
using Flowthru.Core.Services;
using Flowthru.Meta;
using Flowthru.Meta.Providers;
using KedroSpaceflightsGQL.Data;
using KedroSpaceflightsGQL.Flows.DataProcessing;
using KedroSpaceflightsGQL.Flows.DataScience;
using KedroSpaceflightsGQL.Flows.Ingest;
using KedroSpaceflightsGQL.Flows.Reporting;
using KedroSpaceflightsGQL.Infra.GqlClient;
using KedroSpaceflightsGQL.Infra.GqlServer;
using Microsoft.AspNetCore.TestHost;
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
        // --- Swap out this block to point at a real GQL endpoint ----------------
        using var gqlServer = BuildTestServer();
        var gqlHandler = gqlServer.CreateHandler();
        // ------------------------------------------------------------------------

        return await FlowthruCli.RunStandaloneAsync(
          args,
          services => ConfigureServices(services, Directory.GetCurrentDirectory(), gqlHandler)
        );
    }

    // ── Test infrastructure (remove for production) ───────────────────────────

    /// <summary>
    /// Builds an in-process HotChocolate server using ASP.NET Core's <see cref="TestServer"/>.
    /// </summary>
    private static TestServer BuildTestServer()
    {
        // UseTestServer() must be registered at host construction time —
        // it replaces Kestrel as the server transport before the host is built.
        // GetTestServer() on a WebApplication that used CreateSlimBuilder() fails
        // because Kestrel, not TestServer, is already bound.
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

    /// <summary>
    /// Configures services and returns an <see cref="IServiceProvider"/>.
    /// Called by the example test runner; builds an in-process GQL server automatically.
    /// </summary>
    /// <remarks>
    /// Signature must be <c>IServiceProvider ConfigureServices(string? basePath)</c> to
    /// match the reflection-based invocation in <c>ExampleTestRunner</c>.
    /// </remarks>
    public static IServiceProvider ConfigureServices(string? basePath = null)
    {
        // Build an in-process server for the test context. Its lifetime is tied to
        // the service provider — acceptable for a test process.
        var gqlServer = BuildTestServer();
        var gqlHandler = gqlServer.CreateHandler();

        var services = new ServiceCollection();
        // Keep the server alive for the duration of the service provider by registering
        // it as a singleton. ServiceProvider disposes IDisposable singletons on Dispose().
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
        // Register the StrawberryShake client.
        // The named HttpClient "SpaceflightsClient" is created by AddSpaceflightsClient().
        // We configure its primary handler so requests go to our in-process server.
        //
        // ▶ To use a real endpoint, replace the ConfigurePrimaryHttpMessageHandler call with:
        //     .ConfigureHttpClient(c => c.BaseAddress = new Uri("https://your-api/graphql"))
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

        services.AddFlowthru(flowthru =>
        {
            flowthru.UseConfiguration(opts => opts.ConfigurationPath = basePath);
            flowthru.RegisterCatalog(sp => new Catalog(
          basePath: System.IO.Path.Combine(basePath, "Data"),
          client: sp.GetRequiredService<ISpaceflightsClient>()
        ));

            // Output pipeline metadata
            flowthru.ConfigureMetadata(meta =>
        {
              var metadataPath = System.IO.Path.Combine(basePath, "Metadata");
              meta.AddProvider<JsonMetadataProvider, JsonMetadataProviderBuilder>(json =>
              json.WithOutputDirectory(metadataPath)
            )
            .AddProvider<MermaidMetadataProvider, MermaidMetadataProviderBuilder>(mermaid =>
              mermaid.WithOutputDirectory(metadataPath)
            );
          });

            // Ingest: seeds the GQL server from CSV/Excel before DataProcessing runs.
            // Flowthru resolves Catalog + ISpaceflightsClient from DI via delegate parameter inspection.
            flowthru
          .RegisterFlow(label: "Ingest", flow: IngestFlow.Create)
          .WithDescription("Seeds the GraphQL server with raw company, shuttle, and review data");

            // DataProcessing: reads from GQL server; depends on Ingest having run first
            flowthru
          .RegisterFlow(label: "DataProcessing", flow: DataProcessingFlow.Create)
          .WithDescription("Preprocesses companies and shuttles data");

            // DataScience and Reporting are unchanged from the base Spaceflights example
            flowthru
          .RegisterFlow(
            label: "DataScience",
            flow: DataScienceFlow.Create,
            configurationSection: "Flowthru:Flows:DataScience"
          )
          .WithDescription("Trains linear regression model for price prediction");

            flowthru
          .RegisterFlow(
            label: "Reporting",
            flow: ReportingFlow.Create,
            configurationSection: "Flowthru:Flows:Reporting"
          )
          .WithDescription("Generates passenger capacity reports and visualizations");
        });

        services.AddLogging(logging =>
        {
            logging.AddConsole();
            logging.SetMinimumLevel(LogLevel.Information);
        });
    }
}
