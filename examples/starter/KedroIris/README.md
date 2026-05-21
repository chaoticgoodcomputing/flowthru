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

<!-- flowthru:mermaid:start -->
```mermaid
flowchart TB

    %% External Data Inputs
    IrisRaw[("IrisRaw")]
    SplitOptions[("SplitOptions")]
    TrainModelOptions[("TrainModelOptions")]

    subgraph DataEngineering["DataEngineering"]
        SplitAndEncode["SplitAndEncode"]
        IrisFeatures[("IrisFeatures")]
        TrainX[("TrainX")]
        TrainY[("TrainY")]
        TestX[("TestX")]
        TestY[("TestY")]
    end

    subgraph DataScience["DataScience"]
        TrainModel["TrainModel"]
        IrisModel[("IrisModel")]
        Predict["Predict"]
        Predictions[("Predictions")]
        Evaluate["Evaluate"]
        Metrics[("Metrics")]
    end

    %% Edges
    IrisRaw --> SplitAndEncode
    SplitOptions --> SplitAndEncode
    SplitAndEncode --> IrisFeatures
    SplitAndEncode --> TrainX
    SplitAndEncode --> TrainY
    SplitAndEncode --> TestX
    SplitAndEncode --> TestY
    TrainX --> TrainModel
    TrainY --> TrainModel
    TrainModelOptions --> TrainModel
    TrainModel --> IrisModel
    IrisModel --> Predict
    TestX --> Predict
    Predict --> Predictions
    Predictions --> Evaluate
    TestY --> Evaluate
    Evaluate --> Metrics

```
<!-- flowthru:mermaid:end -->

<!-- flowthru:filetree:start -->
```
KedroIris/
├── Program.cs  # entry point
├── Data/
│   ├── _01_Raw/
│   │   ├── Datasets/
│   │   │   └── iris.json
│   │   └── Schemas/
│   │       └── IrisRawSchema.cs
│   ├── ...
│   └── _08_Reporting/
│       ├── Datasets/
│       │   └── metrics.json
│       └── Schemas/
│           └── MetricsSchema.cs
└── Flows/
    ├── DataEngineering/
    │   └── Steps/
    │       └── SplitAndEncodeStep.cs
    └── DataScience/
        └── Steps/
            ├── EvaluateModelStep.cs
            ├── PredictStep.cs
            └── TrainModelStep.cs
```
<!-- flowthru:filetree:end -->
