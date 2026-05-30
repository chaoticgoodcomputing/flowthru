using Flowthru.Data.Storage.Sheets;
using Flowthru.Prelude;
using Google.Apis.Sheets.v4;
using Microsoft.Extensions.DependencyInjection;

namespace Flowthru.Hosting;

/// <summary>
/// Extension methods that register a Google Sheets gateway with
/// <see cref="IFlowthruBuilder"/>. After calling one of these, an
/// <see cref="ISheetsGateway"/> is resolvable from the host's
/// <see cref="IServiceProvider"/>; inject it into your <c>Catalog</c> and pass it
/// to <c>ItemFactory.Enumerable.GoogleSheets&lt;TRow&gt;(...)</c> so Sheets catalog
/// items reach the spreadsheet through it.
/// </summary>
/// <remarks>
/// <para>
/// <strong>The extension owns no credentials.</strong> You supply an
/// authenticated <see cref="SheetsService"/> — built from a service account,
/// OAuth user credentials, or Application Default Credentials, whichever your
/// deployment uses. Flowthru never loads, stores, or sees a secret.
/// </para>
/// <para>
/// <strong>The gateway is swappable.</strong> The production overloads register
/// the live <see cref="SheetsService"/>-backed gateway; tests and examples call
/// <see cref="AddGoogleSheets(IFlowthruBuilder, ISheetsGateway, SheetsRetryOptions)"/> with an offline
/// in-memory gateway instead, with no change to the catalog or the flow.
/// </para>
/// </remarks>
public static class GoogleSheetsFlowthruBuilderExtensions
{
  /// <summary>
  /// Register a Google Sheets gateway backed by a container-owned
  /// <see cref="SheetsService"/>. The container owns the client's lifetime; the
  /// gateway never disposes it. Use this when one authenticated client is shared
  /// for the host's lifetime.
  /// </summary>
  /// <param name="builder">The Flowthru builder.</param>
  /// <param name="service">
  /// An authenticated client. Build it however your deployment authenticates —
  /// service account, OAuth, or Application Default Credentials.
  /// </param>
  public static IFlowthruBuilder AddGoogleSheets(
    this IFlowthruBuilder builder,
    SheetsService service
  )
  {
    if (builder is null) throw new ArgumentNullException(nameof(builder));
    if (service is null) throw new ArgumentNullException(nameof(service));

    return builder.AddGoogleSheets(new SheetsServiceGateway(service));
  }

  /// <summary>
  /// Register a Google Sheets gateway backed by a <see cref="SheetsService"/>
  /// factory. One client is acquired per flow run and disposed when the run
  /// completes, so the client's lifetime is scoped to a single execution. Use
  /// this when a fresh client per run is preferable to a shared one (e.g. to
  /// avoid a long-lived token).
  /// </summary>
  /// <param name="builder">The Flowthru builder.</param>
  /// <param name="serviceFactory">
  /// Builds a fresh authenticated client. Invoked once at the start of each flow
  /// run; the returned client is disposed when the run ends.
  /// </param>
  public static IFlowthruBuilder AddGoogleSheets(
    this IFlowthruBuilder builder,
    Func<SheetsService> serviceFactory
  )
  {
    if (builder is null) throw new ArgumentNullException(nameof(builder));
    if (serviceFactory is null) throw new ArgumentNullException(nameof(serviceFactory));

    return builder.AddGoogleSheets(new SheetsServiceGateway(serviceFactory));
  }

  /// <summary>
  /// Register an explicit <see cref="ISheetsGateway"/> — the swap point. Pass the
  /// offline in-memory gateway in tests and examples, or any custom gateway, with
  /// no change to the catalog. The other overloads delegate here after building
  /// the production gateway.
  /// </summary>
  /// <remarks>
  /// <para>
  /// <strong>Retry is on by default.</strong> The supplied gateway is wrapped in
  /// a <see cref="RetryingSheetsGateway"/> so a transient <c>429</c> is ridden out
  /// with capped exponential backoff in production without any caller wiring. The
  /// decorator forwards the inner gateway's <see cref="IFlowResourceProvider"/>,
  /// so factory-mode per-run client lifecycle is preserved. Pass a configured
  /// <see cref="SheetsRetryOptions"/> to tune the policy, or
  /// <see cref="AddGoogleSheetsWithoutRetry"/> to opt out (e.g. in a test that
  /// asserts on the raw gateway type).
  /// </para>
  /// </remarks>
  /// <param name="builder">The Flowthru builder.</param>
  /// <param name="gateway">The gateway every Sheets catalog item routes through.</param>
  /// <param name="retryOptions">
  /// The backoff policy. <see langword="null"/> uses
  /// <see cref="SheetsRetryOptions"/> defaults (tuned to the ~60-writes/min quota).
  /// </param>
  public static IFlowthruBuilder AddGoogleSheets(
    this IFlowthruBuilder builder,
    ISheetsGateway gateway,
    SheetsRetryOptions? retryOptions = null
  )
  {
    if (builder is null) throw new ArgumentNullException(nameof(builder));
    if (gateway is null) throw new ArgumentNullException(nameof(gateway));

    // Wrap by default so production gets backoff automatically. The decorator
    // forwards the inner gateway's FlowResource, so registering it (rather than
    // the inner) keeps factory-mode lifecycle intact.
    var wrapped = new RetryingSheetsGateway(gateway, retryOptions);
    return RegisterGateway(builder, wrapped);
  }

  /// <summary>
  /// Register an explicit <see cref="ISheetsGateway"/> <strong>without</strong>
  /// the default retry decorator. The escape hatch for tests that assert on the
  /// raw gateway type, or for a caller supplying its own resilience layer.
  /// </summary>
  /// <param name="builder">The Flowthru builder.</param>
  /// <param name="gateway">The gateway every Sheets catalog item routes through.</param>
  public static IFlowthruBuilder AddGoogleSheetsWithoutRetry(
    this IFlowthruBuilder builder,
    ISheetsGateway gateway
  )
  {
    if (builder is null) throw new ArgumentNullException(nameof(builder));
    if (gateway is null) throw new ArgumentNullException(nameof(gateway));

    return RegisterGateway(builder, gateway);
  }

  // One gateway instance, registered once and surfaced under every interface
  // the engine and the catalog resolve it by. Registering it as
  // IFlowResourceProvider is the whole wiring: the engine discovers every
  // provider and brackets its FlowResource around the run, so the factory-mode
  // client is acquired before pre-flight and disposed after post-run. An
  // injected-mode or in-memory gateway returns a null FlowResource and is a
  // no-op in that loop.
  private static IFlowthruBuilder RegisterGateway(IFlowthruBuilder builder, ISheetsGateway gateway)
  {
    builder.Services.AddSingleton(gateway);

    if (gateway is IFlowResourceProvider provider)
    {
      builder.Services.AddSingleton(provider);
    }

    return builder;
  }

  /// <summary>
  /// Alias for <see cref="AddGoogleSheets(IFlowthruBuilder, SheetsService)"/>,
  /// matching the <c>Use*</c> spelling used elsewhere in the hosting surface.
  /// </summary>
  public static IFlowthruBuilder UseGoogleSheets(
    this IFlowthruBuilder builder,
    SheetsService service
  ) => builder.AddGoogleSheets(service);

  /// <summary>
  /// Alias for <see cref="AddGoogleSheets(IFlowthruBuilder, Func{SheetsService})"/>,
  /// matching the <c>Use*</c> spelling used elsewhere in the hosting surface.
  /// </summary>
  public static IFlowthruBuilder UseGoogleSheets(
    this IFlowthruBuilder builder,
    Func<SheetsService> serviceFactory
  ) => builder.AddGoogleSheets(serviceFactory);

  /// <summary>
  /// Alias for <see cref="AddGoogleSheets(IFlowthruBuilder, ISheetsGateway, SheetsRetryOptions)"/>,
  /// matching the <c>Use*</c> spelling used elsewhere in the hosting surface.
  /// </summary>
  public static IFlowthruBuilder UseGoogleSheets(
    this IFlowthruBuilder builder,
    ISheetsGateway gateway
  ) => builder.AddGoogleSheets(gateway);
}
