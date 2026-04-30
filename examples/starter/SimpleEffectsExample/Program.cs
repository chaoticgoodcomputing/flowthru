using Flowthru.Core.Cli;
using Flowthru.Core.Data.Validation;
using Flowthru.Core.Effects;
using Flowthru.Core.Services;
using Flowthru.Meta;
using Flowthru.Meta.Providers;
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
/// integration: register the service, attach <c>AddFlowthruInspect&lt;TService&gt;</c>
/// for preflight reachability, and inject the service into the step's
/// <c>Create(...)</c> factory. No Flowthru-specific extension package is required —
/// the user's own service implementation drops in.
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

    // ── External service registration ─────────────────────────────────────
    // TimeApiClient owns its own HttpClient internally — see Services/TimeApiClient.cs.
    // Production projects can swap this for the typed-client pattern via
    // Microsoft.Extensions.Http (services.AddHttpClient<IRemoteTimeService, …>())
    // when they want IHttpClientFactory's connection-pool semantics. The example
    // uses the simpler pattern to avoid an extra DI package dependency.
    services.AddSingleton<IRemoteTimeService, TimeApiClient>();

    // ── Pre-flight inspection sidecar ─────────────────────────────────────
    // Attaches reachability validation to IRemoteTimeService without modifying
    // the service contract. Flowthru's preflight loop runs this before any step
    // executes and fails fast if the upstream is unreachable. The probe is
    // wrapped in FlowIO.LiftAsync — the standard "lift an async operation into
    // FlowIO" helper.
    services.AddFlowthruInspect<IRemoteTimeService>((svc, ct) =>
      FlowIO.LiftAsync<ValidationResult>(async cancel =>
      {
        // TimeApiClient exposes a PingAsync helper; in general a sidecar can
        // call any method on the service that's cheap and safe to retry.
        if (svc is TimeApiClient apiClient)
        {
          return await apiClient.PingAsync(cancel)
            ? ValidationResult.Success()
            : ValidationResult.Failure(
              "RemoteTime",
              ValidationErrorType.NotFound,
              "timeapi.io is unreachable. Check internet access or service status."
            );
        }
        // Fake/test implementations registered via [FUnitStubContainer] are
        // always considered reachable; tests don't exercise the sidecar.
        return ValidationResult.Success();
      })
    );

    services.AddFlowthru(
      configuration,
      flowthru =>
      {
        flowthru.RegisterCatalog(_ => new Catalog(Path.Combine(basePath, "Data")));

        flowthru.ConfigureMetadata(meta =>
        {
          var metadataPath = Path.Combine(basePath, "Metadata");
          meta
            .AddProvider<JsonMetadataProvider, JsonMetadataProviderBuilder>(json =>
              json.WithOutputDirectory(metadataPath)
            )
            .AddProvider<MermaidMetadataProvider, MermaidMetadataProviderBuilder>(mermaid =>
              mermaid.WithOutputDirectory(metadataPath)
            );
        });

        // RegisterFlow(label, Delegate) inspects the delegate's parameter types and
        // resolves each one from DI: 'Catalog' (subclass of CatalogAbstract) is
        // resolved as the registered catalog; 'IRemoteTimeService' is resolved as
        // the typed HttpClient binding above.
        flowthru
          .RegisterFlow(label: "ReportTime", flow: ReportTimeFlow.Create)
          .WithDescription(
            "Fetches the current UTC time from a public service and writes a "
              + "formatted report. Demonstrates the effect-as-step pattern."
          );
      }
    );

    services.AddLogging(logging =>
    {
      logging.AddConsole();
      logging.SetMinimumLevel(LogLevel.Information);
    });
  }
}
