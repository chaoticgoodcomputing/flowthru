using Flowthru.Data;
using Flowthru.Integrations.MLNet;
using MagicAtlas.Data._04_Embeddings.Schemas;

namespace MagicAtlas.Data;

public partial class Catalog
{
  /// <summary>
  /// ONNX model file for all-MiniLM-L6-v2 sentence-transformers model (Layer 0 seed data).
  ///
  /// Retrieved from https://huggingface.co/sentence-transformers/all-MiniLM-L6-v2/tree/main/onnx
  /// </summary>
  public ICatalogEntry<byte[]> MiniLmOnnxModel =>
    GetOrCreateEntry(
      () =>
        CatalogEntriesMLNet.MLNet.OnnxModel(
          label: "MiniLmOnnxModel",
          filePath: $"{_basePath}/_04_Embeddings/Models/all-MiniLM-L6-v2/model.onnx"
        )
    );

  /// <summary>
  /// Vocabulary file for all-MiniLM-L6-v2 model (Layer 0 seed data).
  ///
  /// Retrieved from https://huggingface.co/sentence-transformers/all-MiniLM-L6-v2/tree/main
  /// </summary>
  public ICatalogEntry<string> MiniLmVocabFile =>
    GetOrCreateEntry(
      () =>
        CatalogEntries.Single.Text(
          label: "MiniLmVocabFile",
          filePath: $"{_basePath}/_04_Embeddings/Models/all-MiniLM-L6-v2/vocab.txt"
        )
    );

  /// <summary>
  /// Oracle text embeddings generated from sentence-transformers model.
  /// </summary>
  public ICatalogEntry<IEnumerable<OracleTextEmbedding>> OracleTextEmbeddings =>
    GetOrCreateEntry(
      () =>
        CatalogEntries.Enumerable.Parquet<OracleTextEmbedding>(
          label: "OracleTextEmbeddings",
          filePath: $"{_basePath}/_04_Embeddings/Datasets/oracle_text_embeddings.parquet"
        )
    );
}
