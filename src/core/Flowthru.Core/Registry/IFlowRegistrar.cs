using Flowthru.Core.Data;
using Flowthru.Core.Flows;
using Flowthru.Core.Graph.Validation;

namespace Flowthru.Core.Registry;

/// <summary>
/// Fluent interface for registering flows in a type-safe manner.
/// </summary>
/// <typeparam name="TCatalog">The catalog type that flows will use</typeparam>
/// <remarks>
/// <para>
/// This interface provides compile-time type safety by tying Flow factories
/// to a specific catalog type. The registrar validates that all registered
/// flows accept the correct catalog type.
/// </para>
/// <para>
/// <strong>Usage:</strong>
/// <code>
/// protected override void RegisterFlows(IFlowRegistrar&lt;MyCatalog&gt; registrar)
/// {
///     // Flow without parameters
///     registrar.Register("processing", ProcessingFlow.Create);
///
///     // Flow with parameters
///     registrar.Register("training", TrainFlow.Create, new TrainOptions());
///
///     // Add metadata
///     registrar.WithDescription("processing", "Cleans and transforms raw data");
/// }
/// </code>
/// </para>
/// </remarks>
public interface IFlowRegistrar<TCatalog>
  where TCatalog : CatalogAbstract
{
  /// <summary>
  /// Registers a Flow with a parameterless factory function.
  /// </summary>
  /// <param name="name">Unique Flow name</param>
  /// <param name="flowFactory">Factory function that creates the Flow from catalog</param>
  /// <returns>This registrar for method chaining</returns>
  /// <remarks>
  /// Use this overload when the Flow doesn't require parameters.
  /// </remarks>
  IFlowRegistrar<TCatalog> Register(string name, Func<TCatalog, Flow> flowFactory);

  /// <summary>
  /// Registers a Flow with a parameterized factory function.
  /// </summary>
  /// <typeparam name="TParams">The type of parameters the Flow requires</typeparam>
  /// <param name="name">Unique Flow name</param>
  /// <param name="flowFactory">Factory function that creates the Flow from catalog and parameters</param>
  /// <param name="parameters">Parameter instance to pass to the flow</param>
  /// <returns>This registrar for method chaining</returns>
  /// <remarks>
  /// <para>
  /// Use this overload when the Flow requires configuration parameters.
  /// Parameters are strongly typed and checked at compile time.
  /// </para>
  /// <para>
  /// The factory signature must match: <c>Func&lt;TCatalog, TParams, Flow&gt;</c>
  /// </para>
  /// </remarks>
  IFlowRegistrar<TCatalog> Register<TParams>(
    string name,
    Func<TCatalog, TParams, Flow> flowFactory,
    TParams parameters
  );

  /// <summary>
  /// Adds a description to the most recently registered flow.
  /// </summary>
  /// <param name="description">Human-readable description of what the Flow does</param>
  /// <returns>This registrar for method chaining</returns>
  /// <remarks>
  /// Use this overload when fluently chaining after Register().
  /// </remarks>
  IFlowRegistrar<TCatalog> WithDescription(string description);

  /// <summary>
  /// Configures validation options for the most recently registered flow.
  /// </summary>
  /// <param name="configure">Action to configure validation behavior</param>
  /// <returns>This registrar for method chaining</returns>
  /// <remarks>
  /// <para>
  /// Use this to opt into deep inspection for critical external data sources
  /// or to explicitly disable inspection for specific inputs.
  /// </para>
  /// <para>
  /// <strong>Example:</strong>
  /// </para>
  /// <code>
  /// registrar.Register("data_processing", ProcessingFlow.Create)
  ///   .WithValidation(validation => {
  ///     validation.Inspect(catalog.Companies, InspectionLevel.Deep);
  ///     validation.Inspect(catalog.Shuttles, InspectionLevel.Deep);
  ///   });
  /// </code>
  /// </remarks>
  IFlowRegistrar<TCatalog> WithValidation(Action<ValidationOptions> configure);
}
