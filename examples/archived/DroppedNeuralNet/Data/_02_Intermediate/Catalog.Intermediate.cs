using DroppedNeuralNet.Data._02_Intermediate.Schemas;
using Flowthru.Core.Data;

namespace DroppedNeuralNet.Data;

public partial class Catalog
{
    /// <summary>
    /// Structural metadata for all 97 pieces: index, input/output dims, and layer type.
    /// Produced by the classify_pieces Python step; persisted as JSON.
    /// Contains no weight data — steps needing tensors join this against <see cref="Pieces"/> by PieceIndex.
    /// </summary>
    public IItem<IEnumerable<PieceMetadata>> PieceMetadata =>
      CreateItem(
        () =>
          ItemFactory.Enumerable.Json<PieceMetadata>(
            label: "PieceMetadata",
            filePath: $"{_basePath}/_02_Intermediate/Datasets/piece_metadata.json"
          )
      );
}
