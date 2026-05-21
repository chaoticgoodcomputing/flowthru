using Flowthru.Flow;
using IrisFUnit.Data;
using IrisFUnit.Data._01_Raw.Schemas;
using IrisFUnit.Data._04_Feature.Schemas;
using IrisFUnit.Data._05_ModelInput.Schemas;
using IrisFUnit.Flows.DataEngineering.Steps;

namespace IrisFUnit.Flows.DataEngineering;

/// <summary>
/// Creates the data engineering pipeline that splits the iris
/// dataset and one-hot-encodes species labels. The single
/// <c>SplitAndEncodeStep</c> has arity 2×5 — its inputs are the raw
/// rows plus the configuration-bound <see cref="SplitAndEncodeStep.Options"/>
/// (sourced from the catalog as a <c>ConfigurationItem&lt;T&gt;</c>) and
/// it produces five outputs: features + train X/Y + test X/Y.
/// </summary>
public static class DataEngineeringFlow
{
  public static BuiltFlow Create(Catalog catalog)
  {
    return FlowBuilder.CreateFlow("DataEngineering", pipeline =>
    {
      pipeline.AddStep<
        IEnumerable<IrisRawSchema>,
        SplitAndEncodeStep.Options,
        IEnumerable<IrisFeatureSchema>,
        IEnumerable<FeatureVectorSchema>,
        IEnumerable<TargetLabelSchema>,
        IEnumerable<FeatureVectorSchema>,
        IEnumerable<TargetLabelSchema>
      >(
        label: "SplitAndEncode",
        transform: SplitAndEncodeStep.Create(),
        inputs: (catalog.IrisRaw, catalog.SplitOptions),
        outputs: (catalog.IrisFeatures, catalog.TrainX, catalog.TrainY, catalog.TestX, catalog.TestY)
      );
    });
  }
}
