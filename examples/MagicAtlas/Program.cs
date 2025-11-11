using Flowthru.Application;
using MagicAtlas.Data;
using MagicAtlas.Pipelines;
using MagicAtlas.Pipelines.CardProcessing;
using MagicAtlas.Pipelines.EmbeddingAnalytics;
using MagicAtlas.Pipelines.EmbeddingReductions;
using MagicAtlas.Pipelines.EmbeddingViz;
using MagicAtlas.Pipelines.OracleTextEmebdding;
using MagicAtlas.Pipelines.RulesProcessing;

namespace MagicAtlas;

public class Program
{
  public static async Task<int> Main(string[] args)
  {
    var app = FlowthruApplication.Create(
      args,
      builder =>
      {
        builder.UseConfiguration();

        builder
          .RegisterPipeline<Catalog>(label: "RulesProcessing", pipeline: RulesProcessing.Create)
          .WithDescription("Processes MTG comprehensive rules into structured JSON");

        builder
          .RegisterPipelineWithConfiguration<Catalog, CardProcessing.Params>(
            label: "CardProcessing",
            pipeline: CardProcessing.Create,
            configurationSection: "Flowthru:Pipelines:CardProcessing"
          )
          .WithDescription("Processes Scryfall card data and preps for analysis");

        builder
          .RegisterPipeline<Catalog>(
            label: "OracleTextEmbedding",
            pipeline: OracleTextEmebdding.Create
          )
          .WithDescription("Generates BERT embeddings for oracle text analysis");

        builder
          .RegisterPipelineWithConfiguration<Catalog, EmbeddingAnalytics.Params>(
            label: "EmbeddingAnalytics",
            pipeline: EmbeddingAnalytics.Create,
            configurationSection: "Flowthru:Pipelines:EmbeddingAnalytics"
          )
          .WithDescription("Analyzes card embeddings through nearest neighbor search");

        builder
          .RegisterPipelineWithConfiguration<Catalog, EmbeddingReductions.Params>(
            label: "EmbeddingReductions",
            pipeline: EmbeddingReductions.Create,
            configurationSection: "Flowthru:Pipelines:EmbeddingReductions"
          )
          .WithDescription("Performs PCA dimensionality reduction on oracle text embeddings");

        builder
          .RegisterPipelineWithConfiguration<Catalog, EmbeddingViz.Params>(
            label: "EmbeddingViz",
            pipeline: EmbeddingViz.Create,
            configurationSection: "Flowthru:Pipelines:EmbeddingViz"
          )
          .WithDescription("Creates enhanced UMAP visualizations with card metadata (color, CMC)");
      }
    );

    return await app.RunAsync();
  }
}
