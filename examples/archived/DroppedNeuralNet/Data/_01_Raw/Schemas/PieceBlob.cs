using Flowthru.Core.Abstractions;

namespace DroppedNeuralNet.Data._01_Raw.Schemas;

/// <summary>
/// A single neural network layer piece as a raw binary blob.
/// The <see cref="Data"/> field holds the raw bytes of a PyTorch state dict (.pth),
/// carrying weight and bias tensors opaquely through C# layers.
/// Python steps deserialize the blob back to tensors using torch.load().
/// </summary>
[FlowthruSchema]
public partial record PieceBlob
{
    public int PieceIndex { get; init; }

    /// <summary>
    /// Serialized PyTorch state dict bytes.
    /// Treated as an opaque binary payload by C# steps — only Python steps inspect tensor values.
    /// </summary>
    public byte[] Data { get; init; } = Array.Empty<byte>();
}
