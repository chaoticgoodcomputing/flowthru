using System.Text;
using Flowthru.Data.Storage;
using Flowthru.Tests.Kits.Storage;

namespace Flowthru.Core.Tests.Storage;

/// <summary>
/// Runs the <see cref="IInspectShallowLaws{TContainer}"/> kit against
/// <see cref="SingletonJsonAdapter{T}"/>. The projection is identity —
/// canonical JSON seed payloads are written verbatim to disk and the
/// adapter inspects them directly. This is the reference shape every
/// other adapter's laws subclass compares against.
/// </summary>
public sealed class SingletonJsonInspectShallowLaws : IInspectShallowLaws<InspectShallowKitRow>
{
  /// <inheritdoc/>
  protected override Task<byte[]> ProjectJsonPayloadAsync(string jsonPayload) =>
    Task.FromResult(Encoding.UTF8.GetBytes(jsonPayload));

  /// <inheritdoc/>
  protected override IStorageAdapter<InspectShallowKitRow> CreateAdapter(string filePath) =>
    new SingletonJsonAdapter<InspectShallowKitRow>(filePath);

  /// <inheritdoc/>
  protected override string FileExtension => ".json";
}
