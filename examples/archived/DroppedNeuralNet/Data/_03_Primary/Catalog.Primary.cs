using DroppedNeuralNet.Data._03_Primary.Schemas;
using Flowthru.Core.Data;

namespace DroppedNeuralNet.Data;

public partial class Catalog
{
    /// <summary>
    /// All (inp, out) piece pairings that satisfy the Block dimension constraints.
    /// Produced by the C# FindLegalPairings step; held in memory.
    /// Each candidate represents a structurally valid Block assembling two classified pieces.
    /// </summary>
    public IItem<IEnumerable<BlockCandidate>> LegalPairings =>
      CreateItem(() => ItemFactory.Enumerable.Memory<BlockCandidate>(label: "LegalPairings"));
}
