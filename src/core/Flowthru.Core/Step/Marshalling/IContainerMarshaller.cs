namespace Flowthru.Step.Marshalling;

/// <summary>
/// Marker interface declaring that <typeparamref name="TExtension"/>
/// can marshal both <see cref="StepContainerKind.Singleton"/> and
/// <see cref="StepContainerKind.Enumerable"/> catalog items into and
/// out of its step environment. This is the minimum floor enforced
/// by <c>FT1301</c> for production extensions.
/// </summary>
/// <remarks>
/// <para>
/// The interface carries no methods — it is a capability witness, not
/// a marshalling contract. The actual conversion between
/// <c>IItem&lt;T&gt;</c> values and the extension's runtime
/// representation lives inside the extension itself
/// (e.g. <c>SubprocessPythonExecutor</c> for the Python extension,
/// EF Core's query translator, etc.). This separation keeps the
/// abstraction minimal while letting <c>FT1303</c> verify that the
/// extension's declared capabilities line up with the marker
/// interfaces it implements.
/// </para>
/// <para>
/// Extensions declaring <see cref="StepContainerKind.Queryable"/>
/// additionally implement
/// <see cref="IQueryableMarshaller{TExtension}"/> — an opt-in marker;
/// the floor's <see cref="IContainerMarshaller{TExtension}"/> is the
/// only one strictly required. Streaming
/// (<see cref="StepContainerKind.Source"/>) has no marshaller marker:
/// per ADR-0023 a <c>FlowSource&lt;T&gt;</c> is consumed by compiling
/// back into <c>FlowIO</c>, not shuttled across a marker seam.
/// </para>
/// </remarks>
/// <typeparam name="TExtension">
/// The step extension's descriptor class (must implement
/// <see cref="IStepExtension"/>). Self-referencing typically — e.g.
/// <c>PythonStepExtension : IContainerMarshaller&lt;PythonStepExtension&gt;</c>.
/// </typeparam>
public interface IContainerMarshaller<TExtension> where TExtension : IStepExtension
{
}

/// <summary>
/// Marker interface declaring that <typeparamref name="TExtension"/>
/// can marshal <see cref="StepContainerKind.Queryable"/> catalog items
/// (i.e. <c>IItem&lt;IQueryable&lt;TRow&gt;&gt;</c>) — typically by
/// pushing computation down into the data source. Opt-in; not part of
/// the minimum-coverage floor.
/// </summary>
/// <typeparam name="TExtension">
/// The step extension's descriptor class.
/// </typeparam>
public interface IQueryableMarshaller<TExtension> where TExtension : IStepExtension
{
}
