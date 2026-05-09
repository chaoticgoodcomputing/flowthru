using Flowthru.Cli;
using Flowthru.Diagnostics;
using Flowthru.Diagnostics.Json;
using Flowthru.Diagnostics.Mermaid;
using Flowthru.Hosting;
using Flowthru.Validation.Runtime;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SimpleEffectsExample.Data;
using SimpleEffectsExample.Flows.Reporting;
using SimpleEffectsExample.Services;

namespace SimpleEffectsExample;

/// <summary>
/// Entry point for the <c>SimpleEffectsExample</c> starter example. Demonstrates the
/// minimal ceremony needed to wire a service-bearing step into a Flowthru flow
/// with full preflight inspection and metadata emission.
/// </summary>
/// <remarks>
/// <para>
/// The pattern shown here is the recommended starting point for any external-system
/// integration: register the service, attach
/// <c>AddFlowServiceInspector&lt;TService&gt;</c> for preflight reachability, and
/// inject the service into the step's <c>Create(...)</c> factory. No Flowthru-specific
/// extension package is required — the user's own service implementation drops in.
/// </para>
/// </remarks>
public class Program
{
  public static Task<int> Main(string[] args) =>
    FlowthruCli.RunStandaloneAsync(
      args,
      services => ConfigureServices(services, Directory.GetCurrentDirectory())
    );

  /// <summary>
  /// Builds a configured service provider. Used by integration tests in
  /// <c>Flowthru.Tests.Examples</c> to run the example end-to-end without going
  /// through the CLI entry point.
  /// </summary>
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

    // ── External service registration ─────────────────────────────────────
    services.AddSingleton<IRemoteTimeService, TimeApiClient>();

    services.AddFlowthru(flowthru =>
      {
        flowthru.RegisterCatalog(_ => new Catalog(Path.Combine(basePath, "Data")));

        // Pre-flight inspector — function-shape overload. Returns
        // Inspect.Pass()/Fail(...); the framework wraps to the internal
        // Validated<PreFlightError, FlowUnit> at the dispatcher boundary.
        flowthru.AddFlowServiceInspector<IRemoteTimeService>(async (svc, ct) =>
        {
          if (svc is TimeApiClient apiClient && !await apiClient.PingAsync(ct))
            return Inspect.Fail(
              "timeapi.io is unreachable. Check internet access or service status.",
              source: "RemoteTime"
            );
          return Inspect.Pass();
        });

        flowthru.ConfigureMetadata(meta =>
        {
          var metadataPath = Path.Combine(basePath, "Metadata");
          meta.AddJsonMetadata(opt => opt.WithOutputDirectory(metadataPath));
          meta.AddMermaidMetadata(opt => opt.WithOutputDirectory(metadataPath));
        });

        flowthru
          .RegisterFlow<Catalog, IRemoteTimeService>(
            label: "ReportTime",
            factory: ReportTimeFlow.Create
          )
          .WithDescription(
            "Fetches the current UTC time from a public service and writes a "
              + "formatted report. Demonstrates the effect-as-step pattern."
          );
      });

    services.AddLogging(logging =>
    {
      logging.AddConsole();
      logging.SetMinimumLevel(LogLevel.Information);
    });
  }
}
