# KedroSpaceflights Starter

A Flowthru example project modeled off of the [Kedro Spaceflights Starter](https://github.com/kedro-org/kedro-starters/tree/main/spaceflights-pandas). 

This particular example demonstrates how to use GraphQL catalog items to ingest from a database. It's similar in style to Flowthru's EFCore examples.

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

<!-- flowthru:mermaid:start -->
```mermaid
flowchart TB

    %% External Data Inputs
    ConfusionMatrixOptions[("ConfusionMatrixOptions")]
    GQLCompanies[("GQLCompanies")]
    GQLReviews[("GQLReviews")]
    GQLShuttles[("GQLShuttles")]
    ModelOptions[("ModelOptions")]
    SeedCompanies[("SeedCompanies")]
    SeedReviews[("SeedReviews")]
    SeedShuttles[("SeedShuttles")]

    subgraph DataProcessing["DataProcessing"]
        CreateModelInputTable["CreateModelInputTable"]
        ModelInputTable[("ModelInputTable")]
    end

    subgraph DataScience["DataScience"]
        SplitData["SplitData"]
        XTrain[("XTrain")]
        XTest[("XTest")]
        TrainModel["TrainModel"]
        Regressor[("Regressor")]
        EvaluateModel["EvaluateModel"]
        ModelMetrics[("ModelMetrics")]
        ModelPredictions[("ModelPredictions")]
    end

    subgraph Ingest["Ingest"]
        PreprocessCompanies["PreprocessCompanies"]
        PreprocessedCompanies[("PreprocessedCompanies")]
        PreprocessShuttles["PreprocessShuttles"]
        PreprocessedShuttles[("PreprocessedShuttles")]
        PreprocessReviews["PreprocessReviews"]
        PreprocessedReviews[("PreprocessedReviews")]
        SeedGqlDatabase["SeedGqlDatabase"]
        GqlDatabaseSeeded[("GqlDatabaseSeeded")]
    end

    subgraph Reporting["Reporting"]
        ComparePassengerCapacity["ComparePassengerCapacity"]
        ShuttleCapacityReport[("ShuttleCapacityReport")]
        GeneratePassengerCapacityChart["GeneratePassengerCapacityChart"]
        ShuttlePassengerCapacityChart[("ShuttlePassengerCapacityChart")]
        GenerateConfusionMatrixChart["GenerateConfusionMatrixChart"]
        ConfusionMatrixChart[("ConfusionMatrixChart")]
    end

    %% Edges
    SeedCompanies --> PreprocessCompanies
    PreprocessCompanies --> PreprocessedCompanies
    SeedShuttles --> PreprocessShuttles
    PreprocessShuttles --> PreprocessedShuttles
    SeedReviews --> PreprocessReviews
    PreprocessReviews --> PreprocessedReviews
    PreprocessedShuttles --> ComparePassengerCapacity
    ComparePassengerCapacity --> ShuttleCapacityReport
    PreprocessedShuttles --> GeneratePassengerCapacityChart
    GeneratePassengerCapacityChart --> ShuttlePassengerCapacityChart
    PreprocessedCompanies --> SeedGqlDatabase
    PreprocessedShuttles --> SeedGqlDatabase
    PreprocessedReviews --> SeedGqlDatabase
    SeedGqlDatabase --> GqlDatabaseSeeded
    GqlDatabaseSeeded --> CreateModelInputTable
    GQLShuttles --> CreateModelInputTable
    GQLCompanies --> CreateModelInputTable
    GQLReviews --> CreateModelInputTable
    CreateModelInputTable --> ModelInputTable
    ModelInputTable --> SplitData
    ModelOptions --> SplitData
    SplitData --> XTrain
    SplitData --> XTest
    XTrain --> TrainModel
    TrainModel --> Regressor
    Regressor --> EvaluateModel
    XTest --> EvaluateModel
    EvaluateModel --> ModelMetrics
    EvaluateModel --> ModelPredictions
    ModelPredictions --> GenerateConfusionMatrixChart
    ConfusionMatrixOptions --> GenerateConfusionMatrixChart
    GenerateConfusionMatrixChart --> ConfusionMatrixChart

```
<!-- flowthru:mermaid:end -->
