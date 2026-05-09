using Flowthru.Data.Catalog;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using SpaceflightsPythonEFCore.Data._07_ModelOutput.Schemas;

namespace SpaceflightsPythonEFCore.Data;

/// <summary>
/// Model output data layer.
/// </summary>
public partial class Catalog
{
  public IItem<ModelMetrics> ModelMetrics =>
    CreateItem(() => Item.Of<ModelMetrics>("ModelMetrics")
      .Json()
      .AtPath($"{_basePath}/_07_ModelOutput/Datasets/model_metrics.json")
      .Build());

  public IItem<IEnumerable<ModelPredictions>> ModelPredictions =>
    CreateItem(() => Item.Of<IEnumerable<ModelPredictions>>("ModelPredictions")
      .EFCoreTable<ModelPredictions, SpaceflightsDbContext>()
      .WithContextFactory(_contextFactory)
      .WithSave(BulkSavePredictions)
      .Build());

  private static async Task BulkSavePredictions(
    SpaceflightsDbContext ctx,
    IEnumerable<ModelPredictions> data,
    CancellationToken ct
  )
  {
    await ctx.Database.ExecuteSqlRawAsync("DELETE FROM \"ModelPredictions\"", ct);

    foreach (var chunk in data.Chunk(500))
    {
      var valueClauses = string.Join(", ", chunk.Select((_, i) => $"(@a{i}, @p{i})"));
      var parameters = chunk
        .SelectMany(
          (row, i) =>
            new SqliteParameter[] { new($"@a{i}", row.Actual), new($"@p{i}", row.Predicted) }
        )
        .ToArray<object>();

#pragma warning disable EF1002
      await ctx.Database.ExecuteSqlRawAsync(
        $"INSERT INTO \"ModelPredictions\" (\"Actual\", \"Predicted\") VALUES {valueClauses}",
        parameters,
        ct
      );
#pragma warning restore EF1002
    }
  }
}
