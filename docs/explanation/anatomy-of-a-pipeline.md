# Anatomy of a Flowthru Pipeline

This document walks through the structure of a Flowthru pipeline. What are nodes? What is the Data Catalog? How do pipelines come together? This anatomy serves as an explanation of Flowthru concepts, and how they connect.

We'll be using snippets from the Iris starter example. By the end, you'll understand how schemas, catalogs, nodes, and pipelines fit together.

## Flowthru Project Layout

A Flowthru project follows a predictable structure:

```
KedroIris/
├── Program.cs
├── Data/
│   └── ...
├── Pipelines/
    └── ...
```

These reflect the only two concerns you should have when writing a good data pipeline: What **data** will you need, and what **transformations** will you need to apply?

## Data

In Flowthru pipelines, the pieces of your database are organized into a Catalog. Every file, table, or object used as an input or output for your pipeline is catalogued in this system. When writing catalog entries, you should only have two concerns:

1. What is the shape, or **schema**, of this entry; and
2. How will this entry be stored?

### Schemas

Schemas define the shape of your data.

```csharp
// Data/_01_Raw/Schemas/IrisRawSchema.cs
[FlowthruSchema]
public partial record IrisRawSchema
{
  [SerializedLabel("sepal_length")]
  public required double SepalLength { get; init; }

  [SerializedLabel("sepal_width")]
  public required double SepalWidth { get; init; }

  [SerializedLabel("petal_length")]
  public required double PetalLength { get; init; }

  [SerializedLabel("petal_width")]
  public required double PetalWidth { get; init; }

  [SerializedLabel("species")]
  public required string Species { get; init; }
}
```

Key points:

- **`[FlowthruSchema]`** protects against attempts to store a schema in an incompatible format.
  - For example — if you have a nested schema, using FlowthruSchema will protect you from trying to save it into CSV, a format that doesn't support nested schemas.
- **`required`** enforces that data in that column will never be null.
- **`[SerializedLabel("...")]`** maps C# property names to external field names (e.g., CSV headers). Omit it when names match.


### Catalog Entries

Each catalog entry has three important details:

1. **Schema:** the shape of the data it stores
2. **Name:** the name of the entry in the catalog
3. **Format:** the way the format is saved

In this example:

```csharp
// Data/_01_Raw/Catalog.Raw.cs
public partial class Catalog
{
  public ICatalogEntry<IEnumerable<IrisRawSchema>> IrisRaw =>
    GetOrCreateEntry(() =>
      CatalogEntries.Enumerable.Csv<IrisRawSchema>(
        label: "IrisRaw",
        filePath: $"{_basePath}/_01_Raw/Datasets/iris.csv"
      )
    );
}
```

1. The **schema** is a list (`IEnumerable`) of `IrisRawSchema` entries.
2. The **name** is `IrisRaw`. You'd be able to reference this catalog entry in your pipelines with `catalog.IrisRaw`.
3. The **format** is `csv`, and the data will be stored in the Data directory at `Data/_01_Raw/Datasets/iris.csv`.

In this example:

```csharp
// Data/_04_Feature/Catalog.Feature.cs
public partial class Catalog
{
  public ICatalogEntry<IEnumerable<IrisFeatureSchema>> IrisFeatures =>
    GetOrCreateEntry(() =>
      CatalogEntries.Enumerable.Parquet<IrisFeatureSchema>(
        label: "IrisFeatures",
        filePath: $"{_basePath}/_04_Feature/Datasets/iris_features.parquet"
      )
    );
}
```

1. The **schema** is a list of `IrisFeatureSchema` entries
2. The **name** is `IrisFeatures`, which you could reference in your pipelines as `catalog.IrisFeatures`
3. The **format** is Parquet, located at `Data/_04_Feature/Datasets/iris_features.parquet`

### Data Layers

By default, Flowthru's starters adopt a [layered data engineering convention](https://towardsdatascience.com/the-importance-of-layered-thinking-in-data-engineering-a09f685edc71/). This is purely convention, and flexible to however you'd prefer to organize your own data.

Each layer has a numbered prefix to enforce a clear, linear structure.

| Layer              | Purpose                                         |
| ------------------ | ----------------------------------------------- |
| `(1) Raw`          | Immutable source data — never modified          |
| `(2) Intermediate` | "Refined" data, with stronger typing guarantees |
| `(3) Primary`      | Domain-specific data models                     |
| `(4) Feature`      | Engineered features for ML                      |
| `(5) ModelInput`   | Joined feature tables ("master tables")         |
| `(6) Models`       | Serialized trained models                       |
| `(7) ModelOutput`  | Model predictions and scores                    |
| `(8) Reporting`    | Final reports and visualizations                |

The file structure of the `Data` directory in the Flowthru starters reflects this structure:

```
KedroIris/
└── Data/
    ├── Catalog.cs                    # Core catalog class
    ├── _01_Raw/
    │   ├── Catalog.Raw.cs            # Catalog entries in this layer
    │   ├── Schemas/                  # Schemas for this layer
    │   └── Datasets/                 # Actual data files
    ├── ...
    └── _08_Reporting/
        ├── Catalog.Reporting.cs      # Catalog entries in this layer
        ├── Schemas/                  # Schemas for this layer
        └── Datasets/                 # Actual data files
```

### Catalog Root

The root catalog file links together all of the layer-specific catalog entry extensions. 

```csharp
// Data/Catalog.cs
public partial class Catalog : DataCatalogBase
{
  private readonly string _basePath;

  public Catalog(string basePath)
  {
    _basePath = basePath;
    InitializeCatalogProperties();  // Required!
  }
}
```

This is a small file that you shouldn't need to update often if you're just adding, removing, or modified catalog entries. It's primary purpose is to set up configuration used across all of your catalog entries.

## Pipelines

Pipelines are made up of **nodes** — transformation functions that are built to move data from one schema to another. The goal is to keep things simple at each level:

1. When writing a node, you don't need to worry about how it's connecting to other nodes — you just need to make sure it inputs the schema, and outputs  the schema

### Nodes

Nodes are simply functions. Easy! The **only** purpose of a node is to take in data that has one schema, and convert it to data that has another schema.

```csharp
// Pipelines/DataEngineering/Nodes/SplitAndEncodeNode.cs
public static class SplitAndEncodeNode
{
  public static Func<
    IEnumerable<IrisRawSchema>, // Input Schema
    IEnumerable<IrisFeatureSchema> // Output Schema
  > Create()
  {
    return (data) =>
    {
      // Transform a list of IrisRawSchema into a list of IrisFeatureSchema...
      return transformedData;
    };
  }
}
```

Key points:

- Nodes are a *contract*: that data for the node will **always** come in as the input schemas, and **always** come out as the output schemas.
- Nodes can have any number of inputs, and any number of outputs — as long as they're defined in the input and output schemas, you're not limited to just one-in, one-out.


### Pipelines

Pipelines define how nodes are connected. When building a pipeline, the task is simple:

1. Add a node by it's `Create()` function
2. Define which catalog entries will be input into the node
3. Define which catalog entries will be output by the node

You'll repeat this process for as many nodes as the pipeline 

```csharp
// Pipelines/DataEngineering/DataEngineeringPipeline.cs
public static class DataEngineeringPipeline
{
  public static Pipeline Create(Catalog catalog, Params parameters)
  {
    return PipelineBuilder.CreatePipeline(pipeline =>
    {
      pipeline.AddNode(
        label: "SplitAndEncode", // Unique label for this node in the pipeline
        transform: SplitAndEncodeNode.Create(),
        input: catalog.IrisRaw,
        output: catalog.IrisFeatures
      );
    });
  }
}
```

Key points:

- **Nodes are never directly connected to each other**: Nodes always take in Catalog entries, and output Catalog entries — *never* directly to each other.
- **Order doesn't matter.** A node in the pipeline is **only** ever concerned about its input data and output data. Flowthru handles the order when you run the pipeline: as long as the data is available, or generated by another node, your pipeline will run.

## Entry Point

`Program.cs` is responsible for wiring everything together. 

```csharp
private static void ConfigureServices(IServiceCollection services, string basePath)
{
    services.AddFlowthru(flowthru =>
    {
        flowthru.UseCatalog(_ => new Catalog(basePath: "Data")));

        flowthru.RegisterPipeline<Catalog>(
            label: "DataEngineering",
            pipeline: DataEngineeringPipeline.Create,
        );

        flowthru.RegisterPipeline<Catalog>(
            label: "DataScience",
            pipeline: DataSciencePipeline.Create,
        );
    });
}
```

And that's it! This finishes the pipeline anatomy. At this point, you have Catalog entries, connected with Nodes in your Pipelines — everything you need to find new and creative ways to organize, analyze, and report on your data!
