---
title: Anatomy of a Flowthru Flow
description: A walkthrough of the pieces that make up a Flowthru flow — schemas, catalogs, steps, and flows — and how they connect, using the Iris starter as the running example.
---

This document walks through the structure of a Flowthru flow. What are steps? What is the Catalog? How do flows come together? This anatomy serves as an explanation of Flowthru concepts, and how they connect.

We'll be using snippets from the Iris starter example. By the end, you'll understand how schemas, catalogs, steps, and flows fit together.

## Flowthru Project Layout

A Flowthru project follows a predictable structure:

```
Iris/
├── Program.cs
├── Data/
│   └── ...
├── Flows/
    └── ...
```

These reflect the only two concerns you should have when writing a good data flow: What **data** will you need, and what **transformations** will you need to apply?

## Data

In Flowthru flows, the pieces of your database are organized into a Catalog. Every file, table, or object used as an input or output for your flow is catalogued in this system. When writing catalog items, you should only have two concerns:

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
  - The generator inspects every property type and classifies the schema as flat (CSV-compatible) or nested (JSON/Parquet only). Primitives, enums, `Guid`, `DateTime`, and similar single-value types are flat. Collections and nested objects make a schema nested.
  - For user-defined types such as NewTypes or strong-typed identifiers, implement `IScalar` to declare that the type serializes to a single value. See [Customizing Schema Property Types](../guides/customizing-schema-property-types.md) for the full classification rules and examples.
- **`required`** enforces that data in that column will never be null.
- **`[SerializedLabel("...")]`** maps C# property names to external field names (e.g., CSV headers). Omit it when names match.


### Catalog Items

Each catalog item has three important details:

1. **Schema:** the shape of the data it stores
2. **Name:** the name of the entry in the catalog
3. **Format:** the way the format is saved

In this example:

```csharp
// Data/_01_Raw/Catalog.Raw.cs
public partial class Catalog
{
  public IItem<IEnumerable<IrisRawSchema>> IrisRaw =>
    GetOrCreateEntry(() =>
      ItemFactory.Enumerable.Csv<IrisRawSchema>(
        label: "IrisRaw",
        filePath: $"{_basePath}/_01_Raw/Datasets/iris.csv"
      )
    );
}
```

1. The **schema** is a list (`IEnumerable`) of `IrisRawSchema` entries.
2. The **name** is `IrisRaw`. You'd be able to reference this catalog item in your flows with `catalog.IrisRaw`.
3. The **format** is `csv`, and the data will be stored in the Data directory at `Data/_01_Raw/Datasets/iris.csv`.

In this example:

```csharp
// Data/_04_Feature/Catalog.Feature.cs
public partial class Catalog
{
  public IItem<IEnumerable<IrisFeatureSchema>> IrisFeatures =>
    GetOrCreateEntry(() =>
      ItemFactory.Enumerable.Parquet<IrisFeatureSchema>(
        label: "IrisFeatures",
        filePath: $"{_basePath}/_04_Feature/Datasets/iris_features.parquet"
      )
    );
}
```

1. The **schema** is a list of `IrisFeatureSchema` entries
2. The **name** is `IrisFeatures`, which you could reference in your flows as `catalog.IrisFeatures`
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
Iris/
└── Data/
    ├── Catalog.cs                    # Core catalog class
    ├── _01_Raw/
    │   ├── Catalog.Raw.cs            # Catalog items in this layer
    │   ├── Schemas/                  # Schemas for this layer
    │   └── Datasets/                 # Actual data files
    ├── ...
    └── _08_Reporting/
        ├── Catalog.Reporting.cs      # Catalog items in this layer
        ├── Schemas/                  # Schemas for this layer
        └── Datasets/                 # Actual data files
```

### Catalog Root

The root catalog file links together all of the layer-specific catalog item extensions. 

```csharp
// Data/Catalog.cs
public partial class Catalog : CatalogAbstract
  {
    private readonly string _basePath;

    public Catalog(string basePath)
    {
      _basePath = basePath;
      InitializeCatalogProperties();  // Required!
    }
  }
```

This is a small file that you shouldn't need to update often if you're just adding, removing, or modified catalog items. It's primary purpose is to set up configuration used across all of your catalog items.

## Flows

Flows are made up of **steps** — transformation functions that are built to move data from one schema to another. The goal is to keep things simple at each level:

1. When writing a step, you don't need to worry about how it's connecting to other steps — you just need to make sure it inputs the schema, and outputs  the schema

### Steps

Steps are simply functions. Easy! The **only** purpose of a step is to take in data that has one schema, and convert it to data that has another schema.

```csharp
// Flows/DataEngineering/Steps/SplitAndEncodeStep.cs
public static class SplitAndEncodeStep
{
  public static Func<
    IEnumerable<IrisRawSchema>, // Input Schema
    IEnumerable<IrisFeatureSchema> // Output Schema
  > Create()
  {
    return (data) => // Input some data as IEnumerable<IrisRawSchema>
    {
      // Transformations...

      return transformedData; // Output some data as IEnumerable<IrisFeatureSchema>
    };
  }
}
```

Key points:

- Steps are a *contract*: that data for the step will **always** come in as the input schemas, and **always** come out as the output schemas.
- Steps can have any number of inputs, and any number of outputs — as long as they're defined in the input and output schemas, you're not limited to just one-in, one-out.


### Flows

Flows define how steps are connected. When building a flow, the task is simple:

1. Add a step by its `Create()` function
2. Define which catalog items will be input into the step
3. Define which catalog items will be output by the step

You'll repeat this process for as many steps as the flow 

```csharp
// Flows/DataEngineering/DataEngineeringFlow.cs
public static class DataEngineeringFlow
{
  public static Flow Create(Catalog catalog, Params parameters)
  {
    return FlowBuilder.CreateFlow(flow =>
    {
      flow.AddStep(
        label: "SplitAndEncode", // Unique label for this step in the flow
        transform: SplitAndEncodeStep.Create(),
        input: catalog.IrisRaw,
        output: catalog.IrisFeatures
      );
    });
  }
}
```

Key points:

- **Steps are never directly connected to each other**: Steps always take in catalog items, and output catalog items — *never* directly to each other.
- **Order doesn't matter.** A step in the flow is **only** ever concerned about its input data and output data. Flowthru handles the order when you run the flow: as long as the data is available, or generated by another step, your flow will run.

## Entry Point

`Program.cs` is responsible for wiring everything together. 

```csharp
private static void ConfigureServices(IServiceCollection services, string basePath)
{
    services.AddFlowthru(flowthru =>
    {
        flowthru.RegisterCatalog(_ => new Catalog(basePath: "Data"));

        flowthru.RegisterFlow(
            label: "DataEngineering",
            flow: DataEngineeringFlow.Create
        );

        flowthru.RegisterFlow(
            label: "DataScience",
            flow: DataScienceFlow.Create
        );
    });
}
```

And that's it! This finishes the flow anatomy. At this point, you have catalog items, connected with Steps in your Flows — everything you need to find new and creative ways to organize, analyze, and report on your data!
