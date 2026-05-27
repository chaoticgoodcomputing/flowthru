using Flowthru.Data.Catalog;
using Microsoft.Extensions.Configuration;

namespace MnistDistributed.Data;

/// <summary>
/// Catalog for the MnistDistributed example. Two items only — the
/// training config (singleton, configuration-bound) and the trained
/// model (binary blob, on-disk file).
/// </summary>
public partial class Catalog : CatalogAbstract
{
  private readonly string _basePath;
  private readonly IConfiguration _configuration;

  public Catalog(string basePath, IConfiguration configuration)
  {
    _basePath = basePath;
    _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
  }
}
