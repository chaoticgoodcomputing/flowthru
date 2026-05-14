namespace Flowthru.Step;

/// <summary>
/// Marker interface implemented by the descriptor class of a step
/// extension (Python, EF Core, GraphQL, etc.). Carrying this marker
/// alongside <see cref="StepExtensionCapabilitiesAttribute"/> is what
/// the Phase 9 analyzers key on to verify the extension's declared
/// container support.
/// </summary>
/// <remarks>
/// <para>
/// The marker has no methods — capability disclosure happens via the
/// attribute, and implementation evidence happens via the marshaller
/// marker interfaces in <c>Flowthru.Step.Marshalling</c>. The
/// <c>FT1303</c> analyzer enforces that the two stay in sync.
/// </para>
/// <para>
/// End users never name <see cref="IStepExtension"/> directly. It
/// exists for analyzers (capability discovery), source generators
/// (overload emission), and extension authors who must declare a
/// single class that anchors their extension's capability surface.
/// </para>
/// </remarks>
public interface IStepExtension
{
}
