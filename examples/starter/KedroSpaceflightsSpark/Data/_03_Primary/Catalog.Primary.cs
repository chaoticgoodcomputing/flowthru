using Flowthru.Core.Data;
using Flowthru.Misc.DataFrames;
using KedroSpaceflightsSpark.Data._03_Primary.Schemas;
using SparkFactory = Flowthru.Extensions.Spark.ItemFactory;

namespace KedroSpaceflightsSpark.Data;

public partial class Catalog
{
    /// <summary>
    /// Unified model input table. Held as an in-memory TypedFrame so the DataScience
    /// and Reporting flows can continue to apply Spark operations (filter, window functions)
    /// before materialization. The deferred execution plan from CreateModelInputTableStep
    /// is passed through as-is; no Spark action is triggered at this catalog boundary.
    /// </summary>
    public IItem<TypedFrame<ModelInputTableSchema>> ModelInputTable =>
      CreateItem(() => SparkFactory.Frame.Memory<ModelInputTableSchema>(label: "ModelInputTable"));
}
