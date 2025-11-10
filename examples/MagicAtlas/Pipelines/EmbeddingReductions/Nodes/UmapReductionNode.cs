using Flowthru.Extensions.ML.UMAP;
using MagicAtlas.Data._07_ModelOutput.Schemas;
using Microsoft.ML;

namespace MagicAtlas.Pipelines.EmbeddingReductions.Nodes;

/// <summary>
/// Performs UMAP dimensionality reduction on oracle text embeddings.
/// </summary>
/// <remarks>
/// <para>
/// Uses UMAP (Uniform Manifold Approximation and Projection) to reduce 384-dimensional
/// sentence embeddings to a lower-dimensional representation while preserving both
/// local and global structure of the data manifold.
/// </para>
/// <para>
/// <strong>Algorithm:</strong> UMAP - A manifold learning technique that constructs
/// a high-dimensional graph representation and optimizes a low-dimensional layout
/// to preserve topological structure.
/// </para>
/// <para>
/// <strong>Advantages over PCA:</strong>
/// </para>
/// <list type="bullet">
/// <item>Preserves local neighborhood structure (semantic similarity)</item>
/// <item>Maintains global topological relationships</item>
/// <item>Captures non-linear patterns in embeddings</item>
/// <item>Produces better visual separation for clusters</item>
/// </list>
/// <para>
/// <strong>Output:</strong> Lower-dimensional embeddings optimized for visualization
/// and cluster analysis.
/// </para>
/// <para>
/// <strong>Citation:</strong> McInnes, L, Healy, J, "UMAP: Uniform Manifold Approximation
/// and Projection for Dimension Reduction", ArXiv e-prints 1802.03426, 2018
/// https://arxiv.org/abs/1802.03426
/// </para>
/// </remarks>
public static class UmapReductionNode
{
  /// <summary>
  /// Configuration options for UMAP dimensionality reduction.
  /// </summary>
  public record Params
  {
    /// <summary>
    /// Number of neighboring points used in local approximations of manifold structure.
    /// </summary>
    /// <remarks>
    /// <para>Default: 15</para>
    /// <para>Range: 2-100</para>
    /// <para>
    /// Larger values result in more global structure being preserved at the loss of
    /// detailed local structure. Smaller values focus on local neighborhoods.
    /// </para>
    /// <para>Common values: 5-50 depending on dataset size and structure.</para>
    /// </remarks>
    public int NumberOfNeighbors { get; init; } = 100;

    /// <summary>
    /// Target dimensionality of the embedding space.
    /// </summary>
    /// <remarks>
    /// <para>Default: 2 (for 2D visualization)</para>
    /// <para>Common values:</para>
    /// <list type="bullet">
    /// <item>2: 2D scatter plots and visualizations</item>
    /// <item>3: 3D interactive visualizations</item>
    /// </list>
    /// </remarks>
    public int NumberOfComponents { get; init; } = 2;

    /// <summary>
    /// Minimum distance between points in the low-dimensional embedding.
    /// </summary>
    /// <remarks>
    /// <para>Default: 0.1</para>
    /// <para>Range: 0.0-0.5</para>
    /// <para>
    /// Controls how tightly the embedding compresses points together. Smaller values
    /// allow more accurate local structure preservation. Larger values produce more
    /// evenly distributed embeddings.
    /// </para>
    /// </remarks>
    public float MinDist { get; init; } = 0.25f;

    /// <summary>
    /// Distance metric for measuring similarity in high-dimensional space.
    /// </summary>
    /// <remarks>
    /// <para>Default: "euclidean"</para>
    /// <para>Supported metrics:</para>
    /// <list type="bullet">
    /// <item>"euclidean": Standard L2 distance</item>
    /// <item>"cosine": Cosine similarity (good for embeddings)</item>
    /// <item>"correlation": Pearson correlation distance</item>
    /// <item>"manhattan": L1 distance</item>
    /// </list>
    /// <para>
    /// For sentence embeddings, "cosine" is often preferred as it focuses on
    /// directional similarity rather than magnitude.
    /// </para>
    /// </remarks>
    public string Metric { get; init; } = "euclidean";

    /// <summary>
    /// Number of training epochs for optimization.
    /// </summary>
    /// <remarks>
    /// <para>Default: null (auto-selected based on dataset size)</para>
    /// <para>
    /// When null, UMAP selects 500 epochs for small datasets (&lt;10k samples)
    /// and 200 epochs for large datasets.
    /// </para>
    /// <para>More epochs result in better optimization but longer training time.</para>
    /// </remarks>
    public int? NumberOfEpochs { get; init; } = null;

    /// <summary>
    /// Random seed for reproducibility.
    /// </summary>
    /// <remarks>
    /// Set to a fixed value for reproducible results across runs.
    /// </remarks>
    public int? Seed { get; init; } = 42;

    /// <summary>
    /// Verbosity level for progress reporting.
    /// </summary>
    /// <remarks>
    /// <para>0 = Silent (no progress output)</para>
    /// <para>1 = Minimal (major phases only)</para>
    /// <para>2 = Detailed (phase progress percentages)</para>
    /// <para>Default is 2 for pipeline visibility.</para>
    /// </remarks>
    public int Verbosity { get; init; } = 2;

    /// <summary>
    /// Whether to use approximate nearest neighbors for k-NN computation.
    /// </summary>
    /// <remarks>
    /// <para>Default: null (auto-detect based on dataset size and dimensionality)</para>
    /// <para>
    /// When null, ANN is automatically enabled for datasets with >10,000 samples
    /// AND >50 dimensions. Manually set to true/false to override auto-detection.
    /// </para>
    /// <para>
    /// ANN provides 10-100x speedup for large high-dimensional datasets with minimal
    /// accuracy loss (~98-99% recall). Only applicable to Euclidean metric.
    /// </para>
    /// </remarks>
    public bool? UseApproximateNearestNeighbors { get; init; } = null;

    /// <summary>
    /// Number of random projection trees in ANN forest.
    /// </summary>
    /// <remarks>
    /// <para>Default: 10</para>
    /// <para>Range: 5-50</para>
    /// <para>
    /// More trees increase accuracy and query time. Each tree adds roughly O(log n)
    /// to query complexity. 10 trees provides good balance for most datasets.
    /// </para>
    /// </remarks>
    public int AnnNumTrees { get; init; } = 10;

    /// <summary>
    /// Maximum number of points per leaf in ANN trees.
    /// </summary>
    /// <remarks>
    /// <para>Default: 10</para>
    /// <para>Range: 5-50</para>
    /// <para>
    /// Smaller values create deeper trees with faster queries but longer build time.
    /// Larger values create shallower trees with more brute-force at leaves.
    /// </para>
    /// </remarks>
    public int AnnLeafSize { get; init; } = 10;

    /// <summary>
    /// Number of candidate nodes to search in ANN forest.
    /// </summary>
    /// <remarks>
    /// <para>Default: null (auto-set to k * numTrees)</para>
    /// <para>
    /// Controls accuracy/speed tradeoff. More candidates = better accuracy but slower.
    /// Default formula (k * numTrees) provides ~98% recall.
    /// </para>
    /// </remarks>
    public int? AnnSearchK { get; init; } = null;
  }

  /// <summary>
  /// Creates a UMAP reduction node with specified options.
  /// </summary>
  /// <param name="options">Configuration options for UMAP</param>
  /// <returns>
  /// Transform function that takes embeddings and returns UMAP-reduced embeddings
  /// </returns>
  public static Func<
    IEnumerable<OracleTextEmbedding>,
    Task<IEnumerable<OracleUmapEmbedding>>
  > Create(Params? options = null)
  {
    var opts = options ?? new Params();

    return async (embeddings) =>
    {
      var embeddingsList = embeddings.ToList();

      // Create ML.NET context
      var mlContext = new MLContext(seed: opts.Seed);

      // Extract embedding vectors as float[][]
      var data = embeddingsList.Select(e => e.Embedding).ToArray();

      // Configure UMAP options
      var umapOptions = new UmapOptions
      {
        NumberOfNeighbors = opts.NumberOfNeighbors,
        NumberOfComponents = opts.NumberOfComponents,
        MinDist = opts.MinDist,
        Metric = opts.Metric,
        NumberOfEpochs = opts.NumberOfEpochs,
        RandomState = opts.Seed,
        Verbosity = opts.Verbosity,
        // Approximate nearest neighbors configuration
        // UseApproximateNearestNeighbors = opts.UseApproximateNearestNeighbors,
        // AnnNumTrees = opts.AnnNumTrees,
        // AnnLeafSize = opts.AnnLeafSize,
        // AnnSearchK = opts.AnnSearchK,
        // Use defaults for other parameters
        Spread = 1.0f,
        LearningRate = 1.0f,
        LocalConnectivity = 1.0f,
        RepulsionStrength = 1.0f,
        NegativeSampleRate = 5,
        SetOpMixRatio = 1.0f,
      };

      // Create UMAP trainer
      var trainer = mlContext.CreateUmapTrainer(umapOptions);

      // Fit UMAP model and transform
      var (model, embedding) = trainer.FitTransform(data);

      // Generate UMAP embeddings
      var umapEmbeddings = embeddingsList
        .Select(
          (e, i) =>
            new OracleUmapEmbedding
            {
              TextEntryId = e.TextEntryId,
              CardId = e.CardId,
              TextType = e.TextType,
              Text = e.Text,
              Components = embedding[i],
              ComponentDimension = opts.NumberOfComponents,
            }
        )
        .ToList();

      return await Task.FromResult(umapEmbeddings.AsEnumerable());
    };
  }
}
