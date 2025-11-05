using Flowthru.Pipelines;
using MagicAtlas.Data;
using MagicAtlas.Pipelines.Nodes;

namespace MagicAtlas.Pipelines;

public static class MtgRulesPipeline
{
  public static Pipeline Create(Catalog catalog)
  {
    return PipelineBuilder.CreatePipeline(pipeline =>
    {
      // Node 1: Split raw text into 5 sections
      pipeline.AddNode(
        label: "SplitSections",
        transform: SplitSectionsNode.Create(),
        input: catalog.RawRules,
        output: (
          catalog.Intro,
          catalog.TableOfContents,
          catalog.RulesText,
          catalog.GlossaryText,
          catalog.Credits
        )
      );

      // Node 2: Parse rules into hierarchical structure
      pipeline.AddNode(
        label: "ParseRules",
        transform: ParseRulesNode.Create(),
        input: catalog.RulesText,
        output: catalog.ParsedRules
      );

      // Node 3: Parse glossary into term-definition pairs
      pipeline.AddNode(
        label: "ParseGlossary",
        transform: ParseGlossaryNode.Create(),
        input: catalog.GlossaryText,
        output: catalog.ParsedGlossary
      );
    });
  }
}
