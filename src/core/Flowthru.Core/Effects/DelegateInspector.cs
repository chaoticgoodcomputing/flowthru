using Flowthru.Core.Data.Validation;

namespace Flowthru.Core.Effects;

/// <summary>
/// Internal <see cref="IFlowthruInspector{TService}"/> implementation that wraps a probe
/// delegate. Created by
/// <see cref="Services.FlowthruInspectionExtensions.AddFlowthruInspect{TService}(
/// Microsoft.Extensions.DependencyInjection.IServiceCollection,
/// System.Func{TService, CancellationToken, FlowIO{ValidationResult}})"/>.
/// </summary>
internal sealed class DelegateInspector<TService> : IFlowthruInspector<TService>
{
  private readonly Func<TService, CancellationToken, FlowIO<ValidationResult>> _probe;

  public DelegateInspector(Func<TService, CancellationToken, FlowIO<ValidationResult>> probe)
  {
    _probe = probe ?? throw new ArgumentNullException(nameof(probe));
  }

  public FlowIO<ValidationResult> InspectAsync(TService service, CancellationToken ct = default)
    => _probe(service, ct);
}
