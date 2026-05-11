using Flowthru.Data.Storage;
using Flowthru.Data.Storage.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Flowthru.Hosting;

/// <summary>
/// Extension methods that register the HTTP storage-medium provider
/// with <see cref="IFlowthruBuilder"/>. Once <see cref="UseHttp(IFlowthruBuilder)"/>
/// is called, any catalog item declared with an <c>http://</c> or
/// <c>https://</c> URI in its path argument resolves through the
/// shared <see cref="IStorageMediumResolver"/> to an
/// <see cref="HttpStorageMedium"/>; the format extension's smart
/// constructor sees a normal <see cref="IStorageMedium"/> regardless
/// of whether the source is local or remote.
/// </summary>
public static class HttpFlowthruBuilderExtensions
{
  /// <summary>
  /// Enable HTTP(S) storage-medium dispatch. Registers
  /// <see cref="HttpStorageMediumProvider"/> as a singleton
  /// <see cref="IStorageMediumProvider"/>; the resolver picks it up
  /// automatically. Configuration is bound from the
  /// <c>Flowthru:Http</c> section; properties not present in
  /// configuration retain their defaults.
  /// </summary>
  /// <example>
  /// <code>
  /// services.AddFlowthru(b =>
  /// {
  ///   b.UseHttp();
  ///   b.RegisterCatalog(sp => new Catalog(
  ///     basePath, sp.GetRequiredService&lt;IStorageMediumResolver&gt;()));
  /// });
  /// </code>
  /// </example>
  public static IFlowthruBuilder UseHttp(this IFlowthruBuilder builder)
  {
    if (builder is null) throw new ArgumentNullException(nameof(builder));

    builder.Services
      .AddOptions<HttpOptions>()
      .Configure<IConfiguration>((opts, cfg) => cfg.GetSection("Flowthru:Http").Bind(opts))
      .ValidateOnStart();

    builder.Services.AddSingleton<IStorageMediumProvider, HttpStorageMediumProvider>();

    return builder;
  }

  /// <summary>
  /// Enable HTTP(S) dispatch with code-first option overrides. The
  /// callback runs after configuration-section binding, so it can
  /// selectively override individual values.
  /// </summary>
  /// <example>
  /// <code>
  /// b.UseHttp(http =>
  /// {
  ///   http.Timeout = TimeSpan.FromMinutes(15);
  ///   http.UserAgent = "MyOrg-Pipeline/2.0";
  ///   http.Cache = new HttpCacheOptions { Directory = "/var/cache/flowthru" };
  /// });
  /// </code>
  /// </example>
  public static IFlowthruBuilder UseHttp(
    this IFlowthruBuilder builder,
    Action<HttpOptions> configure
  )
  {
    if (builder is null) throw new ArgumentNullException(nameof(builder));
    if (configure is null) throw new ArgumentNullException(nameof(configure));

    builder.UseHttp();
    builder.Services.PostConfigure(configure);
    return builder;
  }
}
