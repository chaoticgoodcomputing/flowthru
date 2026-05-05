using Flowthru.Core.Effects;
using Flowthru.Tests.Kits.Effects;

namespace Flowthru.Core.Tests.Execution;

/// <summary>
/// In-memory backend that satisfies <see cref="IEphemeralResourceBackend{TScope}"/>
/// without touching any I/O. Used to self-test the kit's conformance suite —
/// any failure here points at the kit itself, not at a provider.
/// </summary>
public sealed class InMemoryEphemeralResourceBackend : IEphemeralResourceBackend<int>
{
  private bool _exists;
  private readonly List<bool> _peerExists = new();

  public FlowResource<int> CreateResource(bool preserveOnFailure)
  {
    return FlowResource.Make<int>(
      acquire: FlowIO.Lift(() =>
      {
        // Idempotent: a leftover state is wiped here.
        _exists = true;
        return 1;
      }),
      release: (_, ex) =>
        FlowIO.Lift(() =>
        {
          if (ex is not null && preserveOnFailure)
          {
            return FlowUnit.Default;
          }
          _exists = false;
          return FlowUnit.Default;
        })
    );
  }

  public Task<bool> ResourceExists() => Task.FromResult(_exists);

  public Task SeedLeftoverState()
  {
    _exists = true;
    return Task.CompletedTask;
  }

  public Task<IPeerStateProbe?> CreatePeerState()
  {
    var index = _peerExists.Count;
    _peerExists.Add(true);
    return Task.FromResult<IPeerStateProbe?>(new PeerProbe(_peerExists, index));
  }

  private sealed class PeerProbe : IPeerStateProbe
  {
    private readonly List<bool> _peerExists;
    private readonly int _index;

    public PeerProbe(List<bool> peerExists, int index)
    {
      _peerExists = peerExists;
      _index = index;
    }

    public Task<bool> StillExists() => Task.FromResult(_peerExists[_index]);

    public ValueTask DisposeAsync()
    {
      _peerExists[_index] = false;
      return ValueTask.CompletedTask;
    }
  }
}
