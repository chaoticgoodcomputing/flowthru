namespace Flowthru.Validation.PreFlight;

/// <summary>
/// Registration-time validation hook — runs once per process before
/// any flow's pre-flight pipeline. Use this surface for "the entire
/// host is misconfigured" checks that would otherwise propagate into
/// every flow's first run as an opaque failure: bad connection
/// strings, missing DI services, configuration mismatches catchable
/// at host startup.
/// </summary>
/// <remarks>
/// <para>
/// <strong>When hooks run.</strong> The first
/// <c>IFlowthruService.RunAsync</c> call invokes
/// every registered hook before any flow's pre-flight runs. Catalog
/// authors can also call
/// <see cref="Hosting.IFlowthruService.ValidateRegistrationAsync"/>
/// during <c>Main</c> for fail-fast-at-startup behaviour.
/// </para>
/// <para>
/// <strong>Caching.</strong> Registration validation caches the highest
/// <see cref="Flowthru.Flow.ValidationDepth"/> at which a full pass
/// succeeded; a later run at that depth or lower is a no-op, but a deeper
/// run re-runs the hooks that its depth newly admits. Failed hooks re-run
/// on every invocation so transient failures eventually clear without
/// requiring a process restart.
/// </para>
/// <para>
/// <strong>I/O classification.</strong> Hooks self-classify via
/// <see cref="MinimumDepth"/> on the same I/O ladder as the run's
/// <see cref="Flowthru.Flow.ValidationDepth"/>: a pure wiring / DI-presence
/// check declares <c>Hermetic</c> and runs even in an offline smoke test;
/// a hook that probes a live resource keeps the default <c>Shallow</c> and
/// is skipped below it.
/// </para>
/// <para>
/// <strong>Failure shape.</strong> Hook implementations return
/// <see cref="Validated{TError, TValue}"/> so a single hook can
/// report multiple findings in one pass. Failures aggregate into a
/// single <see cref="PreFlightError.RegistrationCheckFailed"/> per
/// finding, all surfaced together.
/// </para>
/// </remarks>
public interface IRegistrationValidationHook
{
  /// <summary>
  /// Stable identifier — used as the <c>HookId</c> field on
  /// <see cref="PreFlightError.RegistrationCheckFailed"/> so the
  /// reader can pinpoint the source of the failure.
  /// </summary>
  string HookId { get; }

  /// <summary>
  /// The lightest <see cref="Flowthru.Flow.ValidationDepth"/> at which this
  /// hook participates. Defaults to <see cref="Flowthru.Flow.ValidationDepth.Shallow"/>
  /// — the historical behaviour, where registration ran only once probing
  /// began. A hook that touches no external resource (a DI-presence or
  /// configuration check) should override this to
  /// <see cref="Flowthru.Flow.ValidationDepth.Hermetic"/> so it still runs
  /// in an offline smoke test. The service skips any hook whose
  /// <c>MinimumDepth</c> exceeds the run's depth.
  /// </summary>
  Flowthru.Flow.ValidationDepth MinimumDepth => Flowthru.Flow.ValidationDepth.Shallow;

  /// <summary>
  /// Run the validation. Receives the configured DI provider so the
  /// hook can resolve services it depends on. Returns an aggregating
  /// <see cref="Validated{TError, TValue}"/> — a single hook can
  /// report multiple findings in one pass.
  /// </summary>
  FlowIO<Validated<PreFlightError, FlowUnit>> Validate(IServiceProvider services);
}
