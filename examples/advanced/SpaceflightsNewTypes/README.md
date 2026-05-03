# KedroIris Starter

A Flowthru starter project modeled off of the [Kedro Spaceflights Starter](https://github.com/kedro-org/kedro-starters/tree/main/spaceflights-pandas). 

## Getting Started

In order to execute this pipeline, move into this directory and run:

```bash
dotnet run
```

This will run both the Data Engineering and Data Science pipelines in sequence, generating the final [model outputs and visualizations.](./Data/_08_Reporting/Datasets)

Once you've confirmed your pipeline runs successfully, you can begin:

1. Adding new data, nodes, and pipelines to your project; and
2. Using the [Flowthru service](./Program.cs) to run your Flowthru pipelines from other .NET projects.

## Project Structure

### Flow Structure

```mermaid
flowchart TB

    %% External Data Inputs
    Companies[("Companies")]
    Shuttles[("Shuttles")]
    Reviews[("Reviews")]

    subgraph DataProcessing["DataProcessing"]
        DataProcessing_PreprocessCompanies["Data Processing.Preprocess Companies"]
        DataProcessing_PreprocessShuttles["Data Processing.Preprocess Shuttles"]
        DataProcessing_CreateModelInputTable["Data Processing.Create Model Input Table"]
        PreprocessedCompanies[("Preprocessed Companies")]
        PreprocessedShuttles[("Preprocessed Shuttles")]
        ModelInputTable[("Model Input Table")]

        DataProcessing_PreprocessCompanies --> PreprocessedCompanies
        DataProcessing_PreprocessShuttles --> PreprocessedShuttles
        PreprocessedShuttles --> DataProcessing_CreateModelInputTable
        PreprocessedCompanies --> DataProcessing_CreateModelInputTable
        DataProcessing_CreateModelInputTable --> ModelInputTable
    end

    subgraph DataScience["DataScience"]
        DataScience_SplitData["Data Science.Split Data"]
        DataScience_TrainModel["Data Science.Train Model"]
        DataScience_EvaluateModel["Data Science.Evaluate Model"]
        XTrain[("X Train")]
        XTest[("X Test")]
        Regressor[("Regressor")]
        ModelMetrics[("Model Metrics")]
        ModelPredictions[("Model Predictions")]

        DataScience_SplitData --> XTrain
        DataScience_SplitData --> XTest
        XTrain --> DataScience_TrainModel
        DataScience_TrainModel --> Regressor
        Regressor --> DataScience_EvaluateModel
        XTest --> DataScience_EvaluateModel
        DataScience_EvaluateModel --> ModelMetrics
        DataScience_EvaluateModel --> ModelPredictions
    end

    subgraph Reporting["Reporting"]
        Reporting_ComparePassengerCapacity["Reporting.Compare Passenger Capacity"]
        Reporting_GeneratePassengerCapacityChart["Reporting.Generate Passenger Capacity Chart"]
        Reporting_ExportPassengerCapacityPng["Reporting.Export Passenger Capacity Png"]
        Reporting_GenerateConfusionMatrixChart["Reporting.Generate Confusion Matrix Chart"]
        Reporting_ExportConfusionMatrixPng["Reporting.Export Confusion Matrix Png"]
        ShuttleCapacityReport[("Shuttle Capacity Report")]
        ShuttlePassengerCapacityChart[("Shuttle Passenger Capacity Chart")]
        ShuttlePassengerCapacityPlotPng[("Shuttle Passenger Capacity Plot Png")]
        ConfusionMatrixChart[("Confusion Matrix Chart")]
        ConfusionMatrixPlotPng[("Confusion Matrix Plot Png")]

        Reporting_ComparePassengerCapacity --> ShuttleCapacityReport
        Reporting_GeneratePassengerCapacityChart --> ShuttlePassengerCapacityChart
        ShuttlePassengerCapacityChart --> Reporting_ExportPassengerCapacityPng
        Reporting_ExportPassengerCapacityPng --> ShuttlePassengerCapacityPlotPng
        Reporting_GenerateConfusionMatrixChart --> ConfusionMatrixChart
        ConfusionMatrixChart --> Reporting_ExportConfusionMatrixPng
        Reporting_ExportConfusionMatrixPng --> ConfusionMatrixPlotPng
    end

    %% External Data to Flow Edges
    Companies --> DataProcessing_PreprocessCompanies
    Shuttles --> DataProcessing_PreprocessShuttles
    Reviews --> DataProcessing_CreateModelInputTable

    %% Cross-Flow Data Flow
    PreprocessedShuttles -.-> Reporting_ComparePassengerCapacity
    PreprocessedShuttles -.-> Reporting_GeneratePassengerCapacityChart
    ModelInputTable -.-> DataScience_SplitData
    ModelPredictions -.-> Reporting_GenerateConfusionMatrixChart
```

### File Structure

```
KedroSpaceflights/
├── Program.cs                      # Program entry point
├── Data/                           # Organized data catalog
│   ├── _01_Raw/                    # Raw input data
│   │   ├── Datasets/companies.csv
│   │   ├── Datasets/reviews.csv
│   │   └── Datasets/shuttles.xlsx
│   ├── ...
│   └── _08_Reporting/              # Metrics and visualizations
├── Flows/
│   ├── DataEngineering/            # Data splitting and encoding
│   ├── DataScience/                # Model training and evaluation
│   └── Reporting/                  # Final reports
```
