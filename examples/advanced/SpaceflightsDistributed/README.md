# SpaceflightsDistributed

<!-- flowthru:mermaid:start -->
```mermaid
flowchart TB

    %% External Data Inputs
    Companies[("Companies")]
    ConfusionMatrixOptions[("ConfusionMatrixOptions")]
    ModelOptions[("ModelOptions")]
    Reviews[("Reviews")]
    Shuttles[("Shuttles")]

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
        TrainModel["TrainModel"]
        Regressor[("Regressor")]
        EvaluateModel["EvaluateModel"]
        ModelMetrics[("ModelMetrics")]
        ModelPredictions[("ModelPredictions")]
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
    Companies --> PreprocessCompanies
    PreprocessCompanies --> PreprocessedCompanies
    Shuttles --> PreprocessShuttles
    PreprocessShuttles --> PreprocessedShuttles
    PreprocessedShuttles --> CreateModelInputTable
    PreprocessedCompanies --> CreateModelInputTable
    Reviews --> CreateModelInputTable
    CreateModelInputTable --> ModelInputTable
    PreprocessedShuttles --> ComparePassengerCapacity
    ComparePassengerCapacity --> ShuttleCapacityReport
    PreprocessedShuttles --> GeneratePassengerCapacityChart
    GeneratePassengerCapacityChart --> ShuttlePassengerCapacityChart
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

<!-- flowthru:filetree:start -->
```
SpaceflightsDistributed/
├── SpaceflightsDistributed/
│   ├── Program.cs  # entry point
│   └── Data/
│       ├── _01_Raw/
│       │   └── Datasets/
│       │       ├── companies.csv
│       │       ├── NOTICE
│       │       ├── reviews.csv
│       │       └── shuttles.xlsx
│       ├── ...
│       └── _08_Reporting/
│           └── Datasets/
│               └── shuttle_capacity_report.json
├── SpaceflightsDistributed.DataProcessing/
│   ├── Data/
│   │   ├── DataProcessingCatalog.cs
│   │   ├── _01_Raw/
│   │   │   └── Schemas/
│   │   │       ├── CompanySchema.cs
│   │   │       ├── ReviewSchema.cs
│   │   │       └── ShuttleSchema.cs
│   │   ├── ...
│   │   └── _03_Primary/
│   │       └── Schemas/
│   │           └── ModelInputTableSchema.cs
│   └── Flows/
│       └── DataProcessing/
│           └── Steps/
│               ├── CreateModelInputTableStep.cs
│               ├── PreprocessCompaniesStep.cs
│               └── PreprocessShuttlesStep.cs
├── SpaceflightsDistributed.DataScience/
│   ├── Data/
│   │   ├── DataScienceCatalog.cs
│   │   ├── _05_ModelInput/
│   │   │   └── Schemas/
│   │   │       └── TestTrainSplit.cs
│   │   ├── ...
│   │   └── _07_ModelOutput/
│   │       └── Schemas/
│   │           ├── ModelMetrics.cs
│   │           └── ModelPredictions.cs
│   └── Flows/
│       └── DataScience/
│           └── Steps/
│               ├── EvaluateModelStep.cs
│               ├── SplitDataStep.cs
│               └── TrainModelStep.cs
└── SpaceflightsDistributed.Reporting/
    ├── Data/
    │   ├── ReportingCatalog.cs
    │   └── _08_Reporting/
    │       └── Schemas/
    │           └── ShuttleCapacityReport.cs
    └── Flows/
        └── Reporting/
            └── Steps/
                ├── ComparePassengerCapacityStep.cs
                ├── CreateConfusionMatrixStep.cs
                └── GeneratePassengerCapacityChartStep.cs
```
<!-- flowthru:filetree:end -->
