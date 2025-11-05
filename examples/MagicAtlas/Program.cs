using Flowthru.Application;
using MagicAtlas.Data;
using MagicAtlas.Pipelines;

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
          .RegisterPipeline<Catalog>(
            label: "RulesProcessingPipeline",
            pipeline: MtgRulesPipeline.Create
          )
          .WithDescription("Processes MTG comprehensive rules into structured JSON");
      }
    );

    return await app.RunAsync();
  }
}
