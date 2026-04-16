# KedroIris Starter

A Flowthru starter project modeled off of the [Kedro Iris Starter](https://github.com/kedro-org/kedro-starters/tree/main/astro-airflow-iris). 

## Getting Started

In order to execute this pipeline, move into this directory and run:

```bash
dotnet run
```

This will run both the Data Engineering and Data Science flows in sequence, generating the final [model metrics output.](./Data/_08_Reporting/Datasets/metrics.json)

Once you've confirmed your flow runs successfully, you can begin:

1. Adding new data, steps, and flows to your project; and
2. Using the [Flowthru service](./Program.cs) to run your Flowthru flows from other .NET projects.

## Project Structure

### Flow Structure

```mermaid
flowchart TB

    %% External Data Inputs
    Catalog_IrisRaw[("Catalog.IrisRaw")]

    subgraph DataEngineering["DataEngineering"]
        DataEngineering_SplitAndEncode["DataEngineering.SplitAndEncode"]
        Catalog_IrisFeatures[("Catalog.IrisFeatures")]
        Catalog_TrainX[("Catalog.TrainX")]
        Catalog_TrainY[("Catalog.TrainY")]
        Catalog_TestX[("Catalog.TestX")]
        Catalog_TestY[("Catalog.TestY")]

        DataEngineering_SplitAndEncode --> Catalog_IrisFeatures
        DataEngineering_SplitAndEncode --> Catalog_TrainX
        DataEngineering_SplitAndEncode --> Catalog_TrainY
        DataEngineering_SplitAndEncode --> Catalog_TestX
        DataEngineering_SplitAndEncode --> Catalog_TestY
    end

    subgraph DataScience["DataScience"]
        DataScience_TrainModel["DataScience.TrainModel"]
        DataScience_Predict["DataScience.Predict"]
        DataScience_Evaluate["DataScience.Evaluate"]
        Catalog_IrisModel[("Catalog.IrisModel")]
        Catalog_Predictions[("Catalog.Predictions")]
        Catalog_Metrics[("Catalog.Metrics")]

        DataScience_TrainModel --> Catalog_IrisModel
        Catalog_IrisModel --> DataScience_Predict
        DataScience_Predict --> Catalog_Predictions
        Catalog_Predictions --> DataScience_Evaluate
        DataScience_Evaluate --> Catalog_Metrics
    end

    %% External Data to Flow Edges
    Catalog_IrisRaw --> DataEngineering_SplitAndEncode

    %% Cross-Flow Data Flow
    Catalog_TrainX -.-> DataScience_TrainModel
    Catalog_TrainY -.-> DataScience_TrainModel
    Catalog_TestX -.-> DataScience_Predict
    Catalog_TestY -.-> DataScience_Evaluate
```
