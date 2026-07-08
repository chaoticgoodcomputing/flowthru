using Flowthru.Flow;
using Flowthru.Prelude;
using Flowthru.Step.DuckDb.Internal;
using Flowthru.Validation.PreFlight;

namespace Flowthru.Validation.PreFlight.DuckDb;

/// <summary>
/// Pre-flight validation hook that walks every DuckDB transform step in
/// the built flow and runs the hermetic SQL schema check for each:
/// empty in-memory tables built from the <em>declared</em> input record
/// schemas (named per the step's relation bindings), the SQL
/// <c>DESCRIBE</c>d against them — binding without executing — and the
/// described result schema verified against the declared output schema.
/// All failures accumulate into a single
/// <see cref="Validated{TError, TValue}"/> result, so a flow with three
/// broken transforms reports all three at once.
/// </summary>
/// <remarks>
/// <para>
/// Registered by <c>UseDuckDb()</c>; runs inside the standard pre-flight
/// pipeline, aggregating applicatively with every other pre-flight
/// error. Classified <see cref="ValidationDepth.Hermetic"/>: the check
/// reaches nothing outside the process — no socket, no data file, no
/// external database; the embedded engine is instantiated in-memory
/// purely as a type-checker over declared metadata (see the hermetic
/// carve-out documented on
/// <see cref="ValidationDepth.Hermetic"/>) — so a schema-breaking SQL
/// edit fails even an offline smoke test
/// (<c>DryRunOption.On + ValidationDepth.Hermetic</c>).
/// </para>
/// <para>
/// The same check backs the design-time surface:
/// <c>DuckDbTransformStep&lt;TOut&gt;.Validate()</c> /
/// <c>BuiltFlow.ValidateDuckDbTransforms()</c> run it from unit tests
/// with identical diagnostics.
/// </para>
/// </remarks>
public sealed class DuckDbTransformValidationHook : IFlowValidationHook
{
  /// <inheritdoc/>
  public string HookId => "duckdb.sql-schema";

  /// <inheritdoc/>
  /// <remarks>
  /// Hermetic — see the class remarks for why an embedded in-memory
  /// engine over declared metadata honours the hermetic promise.
  /// </remarks>
  public ValidationDepth MinimumDepth => ValidationDepth.Hermetic;

  /// <inheritdoc/>
  public FlowIO<Validated<PreFlightError, FlowUnit>> Validate(BuiltFlow flow)
  {
    if (flow is null) throw new ArgumentNullException(nameof(flow));

    return FlowIO.LiftAsync<Validated<PreFlightError, FlowUnit>>(
      async ct =>
      {
        var perStep = new List<Validated<PreFlightError, FlowUnit>>();
        foreach (var step in flow.Steps)
        {
          if (step is not IDuckDbTransformDescriptor transform) continue;

          var failures = await DuckDbSqlSchemaCheck.RunAsync(transform, ct)
            .ConfigureAwait(false);
          perStep.Add(failures.Count == 0
            ? Validated<PreFlightError, FlowUnit>.Pure(FlowUnit.Default)
            : Validated<PreFlightError, FlowUnit>.Fail(
                failures.Select(f => (PreFlightError)new PreFlightError.External(f)).ToList()
              ));
        }

        // Accumulate every step's outcome into a single result. Empty
        // input list is the identity (Pure(FlowUnit)); ZipAll accumulates
        // failures across siblings without short-circuiting.
        return Validated.ZipAll<PreFlightError, FlowUnit>(perStep)
          .Map(_ => FlowUnit.Default);
      },
      source: HookId
    );
  }
}
