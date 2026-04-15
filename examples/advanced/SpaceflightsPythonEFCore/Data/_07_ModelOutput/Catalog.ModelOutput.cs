using Flowthru.Core.Data;
using Flowthru.Extensions.EFCore.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using SpaceflightsPythonEFCore.Data._07_ModelOutput.Schemas;

namespace SpaceflightsPythonEFCore.Data;

/// <summary>
/// Model output data layer.
/// ModelMetrics: Written by Python evaluate_model, stored as JSON.
/// ModelPredictions: Written by Python generate_predictions, stored in EFCore (Python → EFCore handoff).
///   Read back by the Python Reporting pipeline (EFCore → Python handoff).
///   Uses a bulk saveFunc to avoid per-row change-tracker INSERTs.
/// </summary>
public partial class Catalog
{
  public IItem<ModelMetrics> ModelMetrics =>
    CreateItem(
      () =>
        ItemFactory.Single.Json<ModelMetrics>(
          label: "ModelMetrics",
          filePath: $"{_basePath}/_07_ModelOutput/Datasets/model_metrics.json"
        )
    );

  public IItem<IEnumerable<ModelPredictions>> ModelPredictions =>
    CreateItem(
      () =>
        EFCoreItemFactory.Enumerable.EFCore<ModelPredictions, SpaceflightsDbContext>(
          label: "ModelPredictions",
          contextFactory: _contextFactory,
          saveFunc: BulkSavePredictions
        )
    );

  /// <summary>
  /// Replaces all ModelPredictions rows using chunked bulk INSERTs instead of the
  /// default change-tracker path, reducing N round-trips to ceil(rows/500).
  /// SQLite's variable limit is 32766; each row uses 2 parameters, so 500 rows/chunk is safe.
  /// </summary>
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

      // EF1002: suppressed — interpolation only produces parameter name placeholders
      // (@a0, @p0, ...), never data values. All values flow through SqliteParameter.
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
