using DroppedNeuralNet.Data._02_Intermediate.Schemas;
using DroppedNeuralNet.Data._03_Primary.Schemas;
using Flowthru.Core.Steps;

namespace DroppedNeuralNet.Flows.Exploration.Steps;

/// <summary>
/// Enumerates all structurally legal (inp, out) Block pairings from piece metadata.
///
/// A pairing is legal when:
///   - inp piece has LayerType == BlockInp  (weight shape 96×48)
///   - out piece has LayerType == BlockOut  (weight shape 48×96)
///
/// Operates entirely on <see cref="PieceMetadata"/> — no blob data is accessed.
/// The residual connection constraint (in_dim == out_dim == 48) is already encoded in the
/// LayerType classification, so no additional dimension arithmetic is needed here.
/// </summary>
[FlowthruStep]
public static class FindLegalPairingsStep
{
  public static Func<IEnumerable<PieceMetadata>, IEnumerable<BlockCandidate>> Create()
  {
    return (pieces) =>
    {
      var materializedPieces = pieces.ToList();

      var inpPieces = materializedPieces.Where(p => p.LayerType == LayerType.BlockInp).ToList();

      var outPieces = materializedPieces.Where(p => p.LayerType == LayerType.BlockOut).ToList();

      return inpPieces.SelectMany(
        inp => outPieces,
        (inp, @out) =>
          new BlockCandidate { InpPieceIndex = inp.PieceIndex, OutPieceIndex = @out.PieceIndex }
      );
    };
  }
}
