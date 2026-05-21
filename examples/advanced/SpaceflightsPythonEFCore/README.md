# Spaceflights: Python & EFCore Cooperation Demo

This pipeline is an iteration of the Spaceflights pipeline used throughout the Flowthru examples set. This specific pipeline targets testing interoperability between the `Flowthru.Extensions.EFCore` and `Flowthru.Extensions.Python` packages, to demonstrate the separability of:

1. Python and C# nodes coexisting in the same project; with
2. Python taking advantage of advanced implementations of `IStorageAdapter`, such as the implementation used for EFCore.

<!-- flowthru:mermaid:start -->
```mermaid
flowchart TB

    %% External Data Inputs
    Companies[("Companies")]
    Reviews[("Reviews")]
    Shuttles[("Shuttles")]
    SplitDataOptions[("SplitDataOptions")]

    subgraph DataProcessing["DataProcessing"]
        PreprocessCompanies["PreprocessCompanies"]
        PreprocessedCompanies[("PreprocessedCompanies")]
        PreprocessShuttles["PreprocessShuttles"]
        PreprocessedShuttles[("PreprocessedShuttles")]
        CreateModelInputTable["CreateModelInputTable"]
        ModelInputTable[("ModelInputTable")]
    end

    subgraph DataScience["DataScience"]
        SplitData["SplitData"]
        XTrain[("XTrain")]
        XTest[("XTest")]
        YTrain[("YTrain")]
        YTest[("YTest")]
        TrainModel["TrainModel"]
        Regressor[("Regressor")]
        EvaluateModel["EvaluateModel"]
        ModelMetrics[("ModelMetrics")]
        GeneratePredictions["GeneratePredictions"]
        ModelPredictions[("ModelPredictions")]
    end

    subgraph Reporting["Reporting"]
        ComparePassengerCapacityExpress["ComparePassengerCapacityExpress"]
        CapacityPlotExpress[("CapacityPlotExpress")]
        ComparePassengerCapacityGraphObj["ComparePassengerCapacityGraphObj"]
        CapacityPlotGraphObj[("CapacityPlotGraphObj")]
        CreateConfusionMatrix["CreateConfusionMatrix"]
        ConfusionMatrix[("ConfusionMatrix")]
    end

    %% Edges
    Companies --> PreprocessCompanies
    PreprocessCompanies --> PreprocessedCompanies
    Shuttles --> PreprocessShuttles
    PreprocessShuttles --> PreprocessedShuttles
    PreprocessedShuttles --> CreateModelInputTable
    PreprocessedCompanies --> CreateModelInputTable
    Reviews --> CreateModelInputTable
    CreateModelInputTable --> ModelInputTable
    PreprocessedShuttles --> ComparePassengerCapacityExpress
    ComparePassengerCapacityExpress --> CapacityPlotExpress
    PreprocessedShuttles --> ComparePassengerCapacityGraphObj
    ComparePassengerCapacityGraphObj --> CapacityPlotGraphObj
    ModelInputTable --> SplitData
    SplitDataOptions --> SplitData
    SplitData --> XTrain
    SplitData --> XTest
    SplitData --> YTrain
    SplitData --> YTest
    XTrain --> TrainModel
    YTrain --> TrainModel
    TrainModel --> Regressor
    Regressor --> EvaluateModel
    XTest --> EvaluateModel
    YTest --> EvaluateModel
    EvaluateModel --> ModelMetrics
    Regressor --> GeneratePredictions
    XTest --> GeneratePredictions
    YTest --> GeneratePredictions
    GeneratePredictions --> ModelPredictions
    ModelPredictions --> CreateConfusionMatrix
    CreateConfusionMatrix --> ConfusionMatrix

```
<!-- flowthru:mermaid:end -->
