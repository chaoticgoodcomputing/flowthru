using Flowthru.Core.Steps;
using Flowthru.Misc.DataFrames;
using KedroSpaceflightsSpark.Data._03_Primary.Schemas;
using KedroSpaceflightsSpark.Data._08_Reporting.Schemas;

namespace KedroSpaceflightsSpark.Flows.Reporting.Steps;

/// <summary>
/// Ranks each shuttle by price within its shuttle type using Spark window functions.
///
/// Two window functions run over the same PartitionBy(ShuttleType).OrderBy(Price) spec
/// in a single SelectOver pass:
///   - DenseRank: ordinal price rank within type (ties share a rank; no gaps)
///   - Avg:       the type's average price as a window aggregate
///
/// This demonstrates SelectOver with FrameWindowSpec, WindowContext.DenseRank, and
/// WindowContext.Avg — the full window function surface — without materializing
/// the frame until the downstream catalog item is consumed.
/// </summary>
[FlowthruStep]
public static class RankShuttlesByPriceStep
{
    public static Func<TypedFrame<ModelInputTableSchema>, TypedFrame<ShuttlePriceRankSchema>> Create()
    {
        return (input) =>
        {
            var byTypeByPrice = FrameWindowSpec<ModelInputTableSchema>
          .PartitionBy(r => r.ShuttleType)
          .OrderBy(r => r.Price);

            return input.SelectOver(
          (row, win) =>
            new ShuttlePriceRankSchema
            {
                ShuttleId = row.ShuttleId,
                ShuttleType = row.ShuttleType,
                CompanyId = row.CompanyId,
                Price = row.Price,
                PriceRank = win.DenseRank(byTypeByPrice),
                AvgPriceForType = win.Avg(r => r.Price, byTypeByPrice),
            }
        );
        };
    }
}
