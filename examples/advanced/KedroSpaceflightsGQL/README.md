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

```mermaid
flowchart TB

    %% External Data Inputs
    Catalog_SeedCompanies[("Catalog.SeedCompanies")]
    Catalog_SeedShuttles[("Catalog.SeedShuttles")]
    Catalog_SeedReviews[("Catalog.SeedReviews")]
    Catalog_GQLShuttles[("Catalog.GQLShuttles")]
    Catalog_GQLCompanies[("Catalog.GQLCompanies")]
    Catalog_GQLReviews[("Catalog.GQLReviews")]

    subgraph DataProcessing["DataProcessing"]
        DataProcessing_CreateModelInputTable["DataProcessing.CreateModelInputTable"]
        Catalog_ModelInputTable[("Catalog.ModelInputTable")]

        DataProcessing_CreateModelInputTable --> Catalog_ModelInputTable
    end

    subgraph DataScience["DataScience"]
        DataScience_SplitData["DataScience.SplitData"]
        DataScience_TrainModel["DataScience.TrainModel"]
        DataScience_EvaluateModel["DataScience.EvaluateModel"]
        Catalog_XTrain[("Catalog.XTrain")]
        Catalog_XTest[("Catalog.XTest")]
        Catalog_Regressor[("Catalog.Regressor")]
        Catalog_ModelMetrics[("Catalog.ModelMetrics")]
        Catalog_ModelPredictions[("Catalog.ModelPredictions")]

        DataScience_SplitData --> Catalog_XTrain
        DataScience_SplitData --> Catalog_XTest
        Catalog_XTrain --> DataScience_TrainModel
        DataScience_TrainModel --> Catalog_Regressor
        Catalog_Regressor --> DataScience_EvaluateModel
        Catalog_XTest --> DataScience_EvaluateModel
        DataScience_EvaluateModel --> Catalog_ModelMetrics
        DataScience_EvaluateModel --> Catalog_ModelPredictions
    end

    subgraph Ingest["Ingest"]
        Ingest_PreprocessCompanies["Ingest.PreprocessCompanies"]
        Ingest_PreprocessReviews["Ingest.PreprocessReviews"]
        Ingest_PreprocessShuttles["Ingest.PreprocessShuttles"]
        Ingest_SeedGqlDatabase["Ingest.SeedGqlDatabase"]
        Catalog_PreprocessedCompanies[("Catalog.PreprocessedCompanies")]
        Catalog_PreprocessedShuttles[("Catalog.PreprocessedShuttles")]
        Catalog_PreprocessedReviews[("Catalog.PreprocessedReviews")]
        Catalog_GqlDatabaseSeeded[("Catalog.GqlDatabaseSeeded")]

        Ingest_PreprocessCompanies --> Catalog_PreprocessedCompanies
        Ingest_PreprocessReviews --> Catalog_PreprocessedReviews
        Ingest_PreprocessShuttles --> Catalog_PreprocessedShuttles
        Catalog_PreprocessedCompanies --> Ingest_SeedGqlDatabase
        Catalog_PreprocessedShuttles --> Ingest_SeedGqlDatabase
        Catalog_PreprocessedReviews --> Ingest_SeedGqlDatabase
        Ingest_SeedGqlDatabase --> Catalog_GqlDatabaseSeeded
    end

    subgraph Reporting["Reporting"]
        Reporting_ComparePassengerCapacity["Reporting.ComparePassengerCapacity"]
        Reporting_GeneratePassengerCapacityChart["Reporting.GeneratePassengerCapacityChart"]
        Reporting_GenerateConfusionMatrixChart["Reporting.GenerateConfusionMatrixChart"]
        Catalog_ShuttleCapacityReport[("Catalog.ShuttleCapacityReport")]
        Catalog_ShuttlePassengerCapacityChart[("Catalog.ShuttlePassengerCapacityChart")]
        Catalog_ConfusionMatrixChart[("Catalog.ConfusionMatrixChart")]

        Reporting_ComparePassengerCapacity --> Catalog_ShuttleCapacityReport
        Reporting_GeneratePassengerCapacityChart --> Catalog_ShuttlePassengerCapacityChart
        Reporting_GenerateConfusionMatrixChart --> Catalog_ConfusionMatrixChart
    end

    %% External Data to Flow Edges
    Catalog_SeedCompanies --> Ingest_PreprocessCompanies
    Catalog_SeedShuttles --> Ingest_PreprocessShuttles
    Catalog_SeedReviews --> Ingest_PreprocessReviews
    Catalog_GQLShuttles --> DataProcessing_CreateModelInputTable
    Catalog_GQLCompanies --> DataProcessing_CreateModelInputTable
    Catalog_GQLReviews --> DataProcessing_CreateModelInputTable

    %% Cross-Flow Data Flow
    Catalog_PreprocessedShuttles -.-> Reporting_ComparePassengerCapacity
    Catalog_PreprocessedShuttles -.-> Reporting_GeneratePassengerCapacityChart
    Catalog_GqlDatabaseSeeded -.-> DataProcessing_CreateModelInputTable
    Catalog_ModelInputTable -.-> DataScience_SplitData
    Catalog_ModelPredictions -.-> Reporting_GenerateConfusionMatrixChart
```
