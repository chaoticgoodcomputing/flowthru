using Flowthru.Core.Data.Storage;
using Flowthru.Tests.Kits.Medium;

namespace Flowthru.Extensions.Http.Tests.Conformance;

/// <summary>
/// Conformance for <see cref="HttpStorageMediumProvider"/>.
/// </summary>
[TestFixture]
public class HttpStorageMediumProviderConformance : StorageMediumProviderConformance
{
  protected override IStorageMediumProvider CreateProvider() => new HttpStorageMediumProvider();

  protected override IEnumerable<Uri> AcceptedUris =>
    new[]
    {
      new Uri("http://example.com/data.csv"),
      new Uri("https://example.com/data.json"),
      new Uri("https://api.example.com/v1/items?page=1"),
    };
}
