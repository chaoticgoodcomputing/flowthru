using DroppedNeuralNet.Data._01_Raw.Schemas;
using Flowthru.Core.Data;
using Flowthru.Core.Data.Storage;

namespace DroppedNeuralNet.Data;

public partial class Catalog
{
    /// <summary>
    /// Historical sensor measurements produced by the original network.
    /// 48 float features plus the network's prediction and the ground-truth label.
    /// Used by the solver to validate candidate permutations against recorded outputs.
    /// </summary>
    public IItem<IEnumerable<MeasurementSchema>> HistoricalData =>
      CreateItem(
        () =>
          ItemFactory.Enumerable.Csv<MeasurementSchema>(
            label: "HistoricalData",
            filePath: $"{_basePath}/_01_Raw/Datasets/historical_data.csv"
          )
      );

    /// <summary>
    /// The file system path to the directory containing all piece_*.pth files.
    /// Pre-seeded from the constructor so it acts as a DAG root — load_pieces reads it,
    /// keeping the ingestion boundary explicit rather than side-loading via appsettings.
    /// </summary>
    public IItem<string> PiecesDirectory =>
      CreateItem(
        () =>
          new Item<string>(
            "PiecesDirectory",
            new MemoryStorageAdapter<string>($"{_basePath}/_01_Raw/Datasets/Pieces")
          )
      );

    /// <summary>
    /// Raw layer blobs loaded from the pieces directory.
    /// Produced by the load_pieces Python step; persisted as JSON (base64-encoded Data field)
    /// so downstream flows (Exploration, Validation, Solver) can run independently without
    /// re-executing DataPrep from scratch.
    /// </summary>
    public IItem<IEnumerable<PieceBlob>> Pieces =>
      CreateItem(
        () =>
          ItemFactory.Enumerable.Json<PieceBlob>(
            label: "Pieces",
            filePath: $"{_basePath}/_01_Raw/Datasets/pieces.json"
          )
      );
}
