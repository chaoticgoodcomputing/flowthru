using DroppedNeuralNet.Data;
using DroppedNeuralNet.Data._01_Raw.Schemas;
using DroppedNeuralNet.Data._02_Intermediate.Schemas;
using Flowthru.Core.Flows;
using Flowthru.Extensions.Python.Execution;
using Flowthru.Extensions.Python.Steps;

namespace DroppedNeuralNet.Flows.DataPrep;

/// <summary>
/// Ingests all 97 .pth piece files and classifies each by its tensor dimensions.
///
/// Step 1 (load_pieces):   pieces directory path → IEnumerable&lt;PieceBlob&gt;
///   Reads every piece_*.pth file from disk and emits a blob record per piece.
///   Config is passed via a single-row catalog item so it travels through the DAG
///   rather than being side-loaded via appsettings.
///
/// Step 2 (classify_pieces): IEnumerable&lt;PieceBlob&gt; → IEnumerable&lt;ClassifiedPiece&gt;
///   Deserializes each blob with torch.load(), inspects weight shape, and assigns
///   LayerType (BlockInp / BlockOut / Last) based on the (rows, cols) pattern.
/// </summary>
public static class DataPrepFlow
{
  public static Flow Create(Catalog catalog, IPythonExecutor executor)
  {
    return FlowBuilder.CreateFlow(pipeline =>
    {
      pipeline.AddPythonStep<string, IEnumerable<PieceBlob>>(
        label: "LoadPieces",
        description: "Read all piece_*.pth files from the pieces directory into raw blobs.",
        module: "Flows.DataPrep.Steps.load_pieces",
        function: "load_pieces",
        input: catalog.PiecesDirectory,
        output: catalog.Pieces,
        executor: executor
      );

      pipeline.AddPythonStep<IEnumerable<PieceBlob>, IEnumerable<PieceMetadata>>(
        label: "ClassifyPieces",
        description: "Inspect tensor shapes and assign LayerType to each piece. Emits structural metadata only — no blob data.",
        module: "Flows.DataPrep.Steps.classify_pieces",
        function: "classify_pieces",
        input: catalog.Pieces,
        output: catalog.PieceMetadata,
        executor: executor
      );
    });
  }
}
