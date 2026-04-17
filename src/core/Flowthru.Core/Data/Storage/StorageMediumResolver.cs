using Flowthru.Core.Data.Storage.Medium;

namespace Flowthru.Core.Data.Storage;

/// <summary>
/// Default implementation of <see cref="IStorageMediumResolver"/>.
/// </summary>
/// <remarks>
/// <para>
/// Consults registered <see cref="IStorageMediumProvider"/> instances in order, falling
/// back to <see cref="FileStorageMedium"/> for bare file paths and <c>file://</c> URIs.
/// </para>
/// <para>
/// <strong>Two construction modes:</strong>
/// </para>
/// <list type="bullet">
/// <item>
/// <strong>DI-injected:</strong> The DI container passes all registered
/// <c>IStorageMediumProvider</c> singletons via
/// <see cref="StorageMediumResolver(IEnumerable{IStorageMediumProvider})"/>.
/// Used automatically when <c>services.AddFlowthru(...)</c> registers this type.
/// </item>
/// <item>
/// <strong>Direct construction:</strong> Use the parameterless constructor and chain
/// <see cref="Register"/> calls. Useful in tests or standalone programs that don't
/// use the DI service layer.
/// </item>
/// </list>
/// </remarks>
public sealed class StorageMediumResolver : IStorageMediumResolver
{
  private readonly List<IStorageMediumProvider> _providers;

  /// <summary>
  /// DI constructor — providers are collected from all registered
  /// <see cref="IStorageMediumProvider"/> singletons.
  /// </summary>
  public StorageMediumResolver(IEnumerable<IStorageMediumProvider> providers)
  {
    _providers = providers.ToList();
  }

  /// <summary>
  /// Parameterless constructor for direct construction outside the DI container.
  /// Chain <see cref="Register"/> to add providers.
  /// </summary>
  public StorageMediumResolver()
  {
    _providers = new List<IStorageMediumProvider>();
  }

  /// <summary>
  /// Adds a provider to the resolver's dispatch chain.
  /// </summary>
  /// <returns><c>this</c> for fluent chaining.</returns>
  public StorageMediumResolver Register(IStorageMediumProvider provider)
  {
    _providers.Add(provider);
    return this;
  }

  /// <inheritdoc/>
  public IStorageMedium Resolve(string pathOrUri)
  {
    // Only parse as URI if it looks like an absolute URI with a non-file scheme.
    // Uri.TryCreate will happily parse "C:\path\to\file" as a valid absolute URI
    // on Windows, so we guard against that by checking the scheme explicitly.
    if (
      Uri.TryCreate(pathOrUri, UriKind.Absolute, out var uri)
      && uri.Scheme != "file"
      && uri.Scheme.Length > 1 // exclude Windows drive letters (e.g. "C:")
    )
    {
      foreach (var provider in _providers)
      {
        if (provider.CanHandle(uri))
        {
          return provider.Create(uri);
        }
      }
    }

    return new FileStorageMedium(pathOrUri);
  }
}
