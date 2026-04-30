namespace SimpleEffectsExample.Services;

/// <summary>
/// Demonstrates the canonical "service injected into a step" pattern. Steps that
/// need to talk to an external system depend on an interface like this one;
/// implementations are registered in DI by <c>Program.cs</c>, and an
/// <c>AddFlowthruInspect&lt;IRemoteTimeService&gt;(...)</c> sidecar attaches
/// preflight reachability validation without modifying this contract.
/// </summary>
public interface IRemoteTimeService
{
  /// <summary>
  /// Fetches the current UTC time from the upstream provider.
  /// </summary>
  Task<DateTimeOffset> GetCurrentUtcAsync(CancellationToken cancellationToken = default);
}
