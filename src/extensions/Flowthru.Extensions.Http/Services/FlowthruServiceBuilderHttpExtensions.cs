using Flowthru.Core.Data.Storage;
using Flowthru.Core.Services;
using Flowthru.Extensions.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Flowthru.Extensions.Http.Services;

/// <summary>
/// Extension methods for registering HTTP storage support with <see cref="FlowthruServiceBuilder"/>.
/// </summary>
public static class FlowthruServiceBuilderHttpExtensions
{
  /// <summary>
  /// Enables HTTP(S) remote file access for catalog entries that use
  /// <c>http://</c> or <c>https://</c> URIs as file paths.
  /// </summary>
  /// <param name="builder">The Flowthru service builder.</param>
  /// <returns>The builder for method chaining.</returns>
  /// <remarks>
  /// <para>
  /// Once registered, any file-backed catalog factory method
  /// (<c>ItemFactory.Enumerable.Csv</c>, <c>Parquet</c>, <c>Json</c>, etc.) that
  /// receives an <see cref="Flowthru.Core.Data.Storage.IStorageMediumResolver"/> will
  /// automatically route <c>http://</c> and <c>https://</c> paths through
  /// <see cref="Flowthru.Core.Data.Storage.Medium.HttpStorageMedium"/>.
  /// </para>
  /// <para>
  /// <strong>Example:</strong>
  /// <code>
  /// services.AddFlowthru(flowthru =>
  /// {
  ///     flowthru.UseHttp();
  ///     flowthru.RegisterCatalog(sp => new MyCatalog(
  ///         dataPath,
  ///         sp.GetRequiredService&lt;IStorageMediumResolver&gt;()
  ///     ));
  /// });
  /// </code>
  /// </para>
  /// <para>
  /// Catalog entries with local file paths are unaffected — they continue to resolve
  /// to <see cref="Flowthru.Core.Data.Storage.Medium.FileStorageMedium"/>.
  /// </para>
  /// </remarks>
  public static FlowthruServiceBuilder UseHttp(this FlowthruServiceBuilder builder) =>
    builder.UseHttp(_ => { });

  /// <summary>
  /// Enables HTTP(S) remote file access with custom configuration.
  /// </summary>
  /// <param name="builder">The Flowthru service builder.</param>
  /// <param name="configure">Action to configure HTTP options (timeout, user-agent, etc.).</param>
  /// <returns>The builder for method chaining.</returns>
  /// <remarks>
  /// <para>
  /// <strong>Example (custom timeout for large remote files):</strong>
  /// <code>
  /// services.AddFlowthru(flowthru =>
  /// {
  ///     flowthru.UseHttp(http =>
  ///     {
  ///         http.Timeout = TimeSpan.FromMinutes(15);
  ///         http.UserAgent = "MyOrg-DataPipeline/2.0";
  ///     });
  /// });
  /// </code>
  /// </para>
  /// </remarks>
  public static FlowthruServiceBuilder UseHttp(
    this FlowthruServiceBuilder builder,
    Action<HttpOptions> configure
  )
  {
    var options = new HttpOptions();
    configure(options);

    builder.ConfigureServices(services =>
    {
      services.AddSingleton<IStorageMediumProvider>(_ => new HttpStorageMediumProvider(
        options.CreateClient(),
        options.Cache
      ));
    });

    return builder;
  }
}
