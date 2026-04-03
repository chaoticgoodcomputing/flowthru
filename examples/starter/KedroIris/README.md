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
    IrisRaw[("Iris Raw")]

    subgraph DataEngineering["DataEngineering"]
        DataEngineering_SplitAndEncode["Data Engineering.Split And Encode"]
        IrisFeatures[("Iris Features")]
        TrainX[("Train X")]
        TrainY[("Train Y")]
        TestX[("Test X")]
        TestY[("Test Y")]

        DataEngineering_SplitAndEncode --> IrisFeatures
        DataEngineering_SplitAndEncode --> TrainX
        DataEngineering_SplitAndEncode --> TrainY
        DataEngineering_SplitAndEncode --> TestX
        DataEngineering_SplitAndEncode --> TestY
    end

    subgraph DataScience["DataScience"]
        DataScience_TrainModel["Data Science.Train Model"]
        DataScience_Predict["Data Science.Predict"]
        DataScience_Evaluate["Data Science.Evaluate"]
        IrisModel[("Iris Model")]
        Predictions[("Predictions")]
        Metrics[("Metrics")]

        DataScience_TrainModel --> IrisModel
        IrisModel --> DataScience_Predict
        DataScience_Predict --> Predictions
        Predictions --> DataScience_Evaluate
        DataScience_Evaluate --> Metrics
    end

    %% External Data to Flow Edges
    IrisRaw --> DataEngineering_SplitAndEncode

    %% Cross-Flow Data Flow
    TrainX -.-> DataScience_TrainModel
    TrainY -.-> DataScience_TrainModel
    TestX -.-> DataScience_Predict
    TestY -.-> DataScience_Evaluate
```


### File Structure

```
KedroIris/
├── Program.cs                      # Program entry point
├── Data/                           # 8-layer data organization
│   ├── _01_Raw/                   # Immutable source data
│   │   └── Datasets/iris.csv
│   ├── ...
│   └── _08_Reporting/             # Metrics and visualizations
├── Flows/
│   ├── DataEngineering/           # Data splitting and encoding
│   └── DataScience/               # Model training and evaluation
```
