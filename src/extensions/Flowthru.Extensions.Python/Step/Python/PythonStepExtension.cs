using Flowthru.Step;
using Flowthru.Step.Marshalling;

namespace Flowthru.Step.Python;

/// <summary>
/// Capability descriptor for the Python step extension. Declares the
/// <see cref="StepContainerKind"/> shapes the Python stack can ingest
/// and emit, and witnesses the floor implementation via
/// <see cref="IContainerMarshaller{TExtension}"/>.
/// </summary>
/// <remarks>
/// <para>
/// Per Phase 9 of the smart-caching/extensibility RFCs, the Python
/// extension covers the production floor:
/// <see cref="StepContainerKind.Singleton"/> |
/// <see cref="StepContainerKind.Enumerable"/> on both inputs and
/// outputs. Singletons are marshalled via JSON over stdin/stdout;
/// enumerables are marshalled via Apache Arrow IPC. The
/// <c>SubprocessPythonExecutor.ClassifyType</c> classifier dispatches
/// to the correct wire format automatically — users don't choose,
/// they just pass the catalog item whose container kind matches the
/// Python function's expected input shape.
/// </para>
/// <para>
/// This class is purely a declarative descriptor: it carries no
/// behaviour. The attribute drives <c>FT1301</c> (minimum-coverage
/// floor) and <c>FT1303</c> (capability/marshaller alignment); the
/// marker interface is the implementation evidence that
/// <c>FT1303</c> verifies. <c>IQueryableMarshaller</c> and
/// <c>IAsyncStreamMarshaller</c> are intentionally absent — the
/// Python subprocess executor does not push computation down into
/// the data source nor stream rows lazily, so declaring those kinds
/// would be a false claim.
/// </para>
/// </remarks>
[StepExtensionCapabilities(
  inputs: StepContainerKind.Singleton | StepContainerKind.Enumerable,
  outputs: StepContainerKind.Singleton | StepContainerKind.Enumerable
)]
public sealed class PythonStepExtension :
  IStepExtension,
  IContainerMarshaller<PythonStepExtension>
{
  private PythonStepExtension() { }
}
