using Flowthru.Data.Storage;
using Flowthru.Data.Storage.S3;
using Flowthru.Data.Storage.S3.Local;
using Flowthru.Validation.Runtime;
using Flowthru.Validation.Runtime.S3;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Flowthru.Hosting;

/// <summary>
/// Extension methods that register the S3 storage-medium provider with
/// <see cref="IFlowthruBuilder"/>. Once one of these is called, any catalog item
/// declared with an <c>s3://bucket/key</c> path resolves through the shared
/// <see cref="IStorageMediumResolver"/> to an <see cref="S3StorageMedium"/>; the
/// format extension's smart constructor sees a normal <see cref="IStorageMedium"/>
/// regardless of whether the source is local or on S3.
/// </summary>
/// <remarks>
/// <para>
/// <strong>The gateway is the swap point.</strong> <see cref="UseS3(IFlowthruBuilder)"/>
/// wires the AWS-backed gateway (credentials via the standard SDK chain);
/// <see cref="UseLocalS3"/> wires the shipped file-backed stub for offline
/// development; <see cref="UseS3(IFlowthruBuilder, IS3Gateway)"/> wires any
/// gateway you supply. The provider, the medium, and every catalog item are
/// identical across all three.
/// </para>
/// </remarks>
public static class S3FlowthruBuilderExtensions
{
  /// <summary>
  /// Enable <c>s3://</c> storage-medium dispatch backed by AWS S3. Registers the
  /// AWS-backed <see cref="IS3Gateway"/> and the <see cref="S3StorageMediumProvider"/>;
  /// the resolver picks the provider up automatically. Configuration is bound from
  /// the <c>Flowthru:S3</c> section; credentials resolve via the standard SDK
  /// chain (environment, profile, instance role).
  /// </summary>
  /// <example>
  /// <code>
  /// services.AddFlowthru(b =>
  /// {
  ///   b.UseS3();
  ///   b.RegisterCatalog(sp => new Catalog(
  ///     basePath, sp.GetRequiredService&lt;IStorageMediumResolver&gt;()));
  /// });
  /// </code>
  /// </example>
  public static IFlowthruBuilder UseS3(this IFlowthruBuilder builder)
  {
    if (builder is null) throw new ArgumentNullException(nameof(builder));

    builder.Services
      .AddOptions<S3Options>()
      .Configure<IConfiguration>((opts, cfg) => cfg.GetSection("Flowthru:S3").Bind(opts))
      .ValidateOnStart();

    builder.Services.AddSingleton<IS3Gateway, AmazonS3Gateway>();
    return RegisterProvider(builder);
  }

  /// <summary>
  /// Enable <c>s3://</c> dispatch with code-first option overrides. The callback
  /// runs after configuration-section binding, so it can selectively override
  /// individual values (region, endpoint, path-style).
  /// </summary>
  /// <example>
  /// <code>
  /// b.UseS3(s3 =>
  /// {
  ///   s3.Region = "us-west-2";
  ///   s3.ServiceUrl = "http://localhost:9000"; // MinIO / LocalStack
  ///   s3.ForcePathStyle = true;
  /// });
  /// </code>
  /// </example>
  public static IFlowthruBuilder UseS3(
    this IFlowthruBuilder builder,
    Action<S3Options> configure
  )
  {
    if (builder is null) throw new ArgumentNullException(nameof(builder));
    if (configure is null) throw new ArgumentNullException(nameof(configure));

    builder.UseS3();
    builder.Services.PostConfigure(configure);
    return builder;
  }

  /// <summary>
  /// Enable <c>s3://</c> dispatch over an explicit <see cref="IS3Gateway"/> — the
  /// swap point. Pass the offline <see cref="LocalFileS3Gateway"/>, a gateway
  /// wired to LocalStack/MinIO, or any custom gateway, with no change to the
  /// catalog or the flow.
  /// </summary>
  /// <param name="builder">The Flowthru builder.</param>
  /// <param name="gateway">The gateway every <c>s3://</c> item routes through.</param>
  public static IFlowthruBuilder UseS3(
    this IFlowthruBuilder builder,
    IS3Gateway gateway
  )
  {
    if (builder is null) throw new ArgumentNullException(nameof(builder));
    if (gateway is null) throw new ArgumentNullException(nameof(gateway));

    builder.Services.AddSingleton(gateway);
    return RegisterProvider(builder);
  }

  /// <summary>
  /// Enable <c>s3://</c> dispatch backed by the shipped file-based stub rooted at
  /// <paramref name="rootDirectory"/> — a fully offline stand-in for S3 with no
  /// AWS account, credentials, or network. Each object lands at
  /// <c>{rootDirectory}/{bucket}/{key}</c>. For local development, demos, and
  /// tests; not for shared or production storage.
  /// </summary>
  public static IFlowthruBuilder UseLocalS3(
    this IFlowthruBuilder builder,
    string rootDirectory
  )
  {
    if (builder is null) throw new ArgumentNullException(nameof(builder));
    return builder.UseS3(new LocalFileS3Gateway(rootDirectory));
  }

  private static IFlowthruBuilder RegisterProvider(IFlowthruBuilder builder)
  {
    // Resolve the read profile for the medium's shared memory-domain dependency
    // (ADR-0019, #111). Registered on every UseS3 path; a no-op until a finite
    // MaxConcurrentReads is declared, so default behaviour is unchanged.
    builder.Services.TryAddEnumerable(
      ServiceDescriptor.Singleton<IServiceProfileContributor, S3ReadProfileContributor>());

    builder.Services.AddSingleton<IStorageMediumProvider>(sp =>
      new S3StorageMediumProvider(
        sp.GetRequiredService<IS3Gateway>(),
        sp.GetService<IOptions<S3Options>>()?.Value.MaxConcurrentReads ?? int.MaxValue));
    return builder;
  }
}
