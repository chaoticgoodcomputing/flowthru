namespace Flowthru.Step;

/// <summary>
/// Declares which <see cref="StepContainerKind"/> shapes a step
/// extension's stack can ingest as inputs and emit as outputs.
/// Applied to the extension's <see cref="IStepExtension"/>-implementing
/// descriptor class.
/// </summary>
/// <remarks>
/// <para>
/// The attribute is purely declarative — the corresponding runtime
/// evidence is the set of marshaller marker interfaces the descriptor
/// implements (see <c>Flowthru.Step.Marshalling</c>). The
/// <c>FT1303</c> analyzer enforces that declared capabilities match
/// implemented marshaller interfaces; <c>FT1301</c> enforces the
/// minimum floor of <see cref="StepContainerKind.Singleton"/> |
/// <see cref="StepContainerKind.Enumerable"/> for
/// <see cref="ExtensionStatus.Production"/> extensions.
/// </para>
/// <para>
/// Extension authors set <see cref="Status"/> to
/// <see cref="ExtensionStatus.InDevelopment"/> while iterating on the
/// extension's algebra; the minimum-coverage diagnostic downgrades to
/// a warning. The default (<see cref="ExtensionStatus.Production"/>)
/// is the strict path — shipped extensions must support the floor.
/// </para>
/// </remarks>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class StepExtensionCapabilitiesAttribute : Attribute
{
  /// <summary>
  /// Container kinds the extension accepts as step inputs. Must
  /// include at minimum <see cref="StepContainerKind.Singleton"/> |
  /// <see cref="StepContainerKind.Enumerable"/> when
  /// <see cref="Status"/> is <see cref="ExtensionStatus.Production"/>.
  /// </summary>
  public StepContainerKind Inputs { get; }

  /// <summary>
  /// Container kinds the extension can emit as step outputs. Same
  /// minimum-floor rules as <see cref="Inputs"/>.
  /// </summary>
  public StepContainerKind Outputs { get; }

  /// <summary>
  /// Production-readiness state. Defaults to
  /// <see cref="ExtensionStatus.Production"/>; set to
  /// <see cref="ExtensionStatus.InDevelopment"/> while iterating on
  /// the extension's algebra to downgrade <c>FT1301</c> from error to
  /// warning.
  /// </summary>
  public ExtensionStatus Status { get; set; } = ExtensionStatus.Production;

  /// <summary>
  /// Declares a step extension's container capabilities.
  /// </summary>
  /// <param name="inputs">Container kinds accepted as inputs.</param>
  /// <param name="outputs">Container kinds emitted as outputs.</param>
  public StepExtensionCapabilitiesAttribute(
    StepContainerKind inputs,
    StepContainerKind outputs
  )
  {
    Inputs = inputs;
    Outputs = outputs;
  }
}
