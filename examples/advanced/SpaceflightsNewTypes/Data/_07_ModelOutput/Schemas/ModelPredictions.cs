using Flowthru.Data.Schema;

namespace SpaceflightsNewTypes.Data._07_ModelOutput.Schemas;

/// <summary>
/// Model prediction results containing actual and predicted values.
/// Used for generating confusion matrices and prediction accuracy visualizations.
/// </summary>
/// <remarks>
/// <para>
/// This schema demonstrates the <c>[FlowthruColumn]</c> source generator for the
/// <em>layer-local</em> case: <c>ActualReviewScore</c> and <c>PredictedReviewScore</c> are
/// only used inside <c>_07_ModelOutput.Schemas</c>, so no cross-namespace coordination is
/// required and the generator can spin up the NewTypes directly.
/// </para>
/// <para>
/// The two NewTypes share the same backing type (<see cref="double"/>) but are distinct at
/// the type level — the compiler refuses to swap actuals for predictions in a confusion
/// matrix or scatter chart. The downstream reporting steps demonstrate the explicit
/// <c>.Value</c> downcast for cases where the underlying primitive is genuinely needed.
/// </para>
/// </remarks>
[FlowthruSchema]
public partial record ModelPredictions
{
  /// <summary>
  /// Actual review score from the test set. Distinct from <see cref="PredictedReviewScore"/>
  /// at the type level even though both wrap <see cref="double"/>.
  /// </summary>
  [FlowthruColumn(typeof(double))]
  public ActualReviewScore Actual { get; init; }

  /// <summary>
  /// Predicted review score from the trained model.
  /// </summary>
  [FlowthruColumn(typeof(double))]
  public PredictedReviewScore Predicted { get; init; }
}
