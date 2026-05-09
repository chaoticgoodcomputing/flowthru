using Flowthru.Data.Catalog;
using KedroSpaceflightsGQL.Infra.GqlClient;

namespace KedroSpaceflightsGQL.Data;

/// <summary>
/// Data catalog for the Spaceflights GQL pipeline, providing access to datasets across all data layers.
/// </summary>
public partial class Catalog : CatalogAbstract
{
  private readonly string _basePath;
  private readonly ISpaceflightsClient _client;

  /// <summary>
  /// Initializes a new instance of the <see cref="Catalog"/> class.
  /// </summary>
  /// <param name="basePath">The base path for data storage.</param>
  /// <param name="client">
  /// StrawberryShake-generated GraphQL client. Swap this for a real endpoint by configuring
  /// the named <c>HttpClient</c> in <c>Program.ConfigureServices</c> to point at your GQL server.
  /// </param>
  public Catalog(string basePath, ISpaceflightsClient client)
  {
    _basePath = basePath;
    _client = client;
  }
}
