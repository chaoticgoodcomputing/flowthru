using System.Net;
using Flowthru.Core.Data.Storage;
using Flowthru.Core.Data.Storage.Medium;
using Flowthru.Tests.Kits.Medium;

namespace Flowthru.Extensions.Http.Tests.Conformance;

/// <summary>
/// Conformance for <see cref="HttpStorageMedium"/>.
/// </summary>
/// <remarks>
/// HTTP is read-only by design (<c>Traits.CanWrite = false</c>) and remote, so the kit's
/// write-round-trip and InspectTarget scenarios pass via the read-only skip path. Read,
/// Exists-on-readable, and Exists-on-nonexistent are exercised against fake handlers.
/// </remarks>
[TestFixtureSource(nameof(Fixtures))]
public class HttpStorageMediumConformance : StorageMediumConformance
{
  public static IEnumerable<string> Fixtures => new[] { "Synthetic/http-bytes" };

  public HttpStorageMediumConformance(string scenarioName) : base(scenarioName) { }

  protected override IStorageMedium CreateReadable(byte[] data)
  {
    var body = System.Text.Encoding.UTF8.GetString(data);
    var handler = new FakeHandler(HttpStatusCode.OK, body);
    var client = new HttpClient(handler);
    return new HttpStorageMedium(new Uri("https://example.com/data"), client);
  }

  protected override IStorageMedium CreateNonexistent()
  {
    var handler = new FakeHandler(HttpStatusCode.NotFound, body: string.Empty);
    var client = new HttpClient(handler);
    return new HttpStorageMedium(new Uri("https://example.com/missing"), client);
  }

  protected override IStorageMedium CreateWritable()
  {
    // HttpStorageMedium is structurally read-only; the kit's round-trip test will skip
    // because Traits.CanWrite = false. Returning a readable medium satisfies the contract;
    // the writable scenarios short-circuit before any write attempt.
    return CreateReadable(FixtureBytes);
  }
}
