using Flowthru.Core.Data.Storage;
using Flowthru.Core.Services;
using Flowthru.Extensions.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Flowthru.Extensions.Http.Services;

/// <summary>
/// Extension methods for registering HTTP storage support with <see cref="IFlowthruBuilder"/>.
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
  /// Configuration is bound from the <c>Flowthru:Http</c> section. Properties not
  /// present in configuration retain their default values.
  /// </para>
  /// <para>
  /// <strong>Example:</strong>
  /// <code>
  /// services.AddFlowthru(configuration, flowthru =>
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
  public static IFlowthruBuilder UseHttp(this IFlowthruBuilder builder)
  {
    builder
      .Services.AddOptions<HttpOptions>()
      .Configure<IConfiguration>((opts, cfg) => cfg.GetSection("Flowthru:Http").Bind(opts))
      .ValidateOnStart();

    builder.Services.AddSingleton<IStorageMediumProvider, HttpStorageMediumProvider>();

    return builder;
  }

  /// <summary>
  /// Enables HTTP(S) remote file access with code-first configuration overrides.
  /// </summary>
  /// <param name="builder">The Flowthru service builder.</param>
  /// <param name="configure">Action to override HTTP options after config-file binding.</param>
  /// <returns>The builder for method chaining.</returns>
  /// <remarks>
  /// <para>
  /// The <paramref name="configure"/> callback runs after <c>Flowthru:Http</c> section
  /// binding, so it can selectively override specific values.
  /// </para>
  /// <para>
  /// <strong>Example (custom timeout for large remote files):</strong>
  /// <code>
  /// services.AddFlowthru(configuration, flowthru =>
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
  public static IFlowthruBuilder UseHttp(
    this IFlowthruBuilder builder,
    Action<HttpOptions> configure
  )
  {
    builder.UseHttp();
    builder.Services.PostConfigure(configure);
    return builder;
  }
}
