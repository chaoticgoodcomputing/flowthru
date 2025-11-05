using Flowthru.Application;
using MagicAtlas.Data;
using MagicAtlas.Pipelines;
using MagicAtlas.Pipelines.CardProcessing;
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
      }
    );

    return await app.RunAsync();
  }
}
