# Flowthru.Extensions.Python

Run Python functions as Steps inside a Flowthru Flow. A `@step`-decorated function in a `.py`
module becomes a first-class Step in the DAG — wired between typed Catalog Items, validated at
pre-flight, and cached like any C# Step. Rows cross the C#/Python boundary as Apache Arrow, so
a Step that hands `IEnumerable<TSchema>` to Python receives a `pandas.DataFrame` and returns
one, with no glue code in between.

[![coverage](https://codecov.io/gh/chaoticgoodcomputing/flowthru/branch/main/graph/badge.svg?component=flowthru_extensions_python)](https://codecov.io/gh/chaoticgoodcomputing/flowthru)

## Mental model

A Flowthru Step is a typed transform from input Items to output Items. This package lets that
transform live in Python instead of C# — the Flow doesn't care which side a Step runs on. Bring
your data-science mental model: a function that takes one or more DataFrames and returns one,
decorated so Flowthru knows its input and output schemas. The engine owns scheduling, type
contracts, and caching; you own the `pandas`/`scikit-learn`/`numpy` body. Python Steps run
through a single subprocess worker, so the scheduler serializes them — fan-out parallelism is a
C#-side property, not a Python one.

## Install

```bash
dotnet add package Flowthru.Extensions.Python
```

Register Python support, then add a Python Step that points at a module and function:

```csharp
services.AddFlowthru(flowthru =>
{
    flowthru.RegisterCatalog(sp => new Catalog(basePath, sp.GetRequiredService<IConfiguration>()));
    flowthru.UsePython(python =>
    {
        python.ModuleSearchPaths.Add(basePath);
        python.VenvPath = venvPath;
    });

    flowthru
        .RegisterFlow<Catalog, IPythonExecutor>("DataProcessing", DataProcessingFlow.Create)
        .WithDescription("Preprocesses companies and shuttles using Python");
});
```

```csharp
public static BuiltFlow Create(Catalog catalog, IPythonExecutor executor) =>
    FlowBuilder.CreateFlow("DataProcessing", pipeline =>
    {
        pipeline.AddPythonStep(
            label: "PreprocessCompanies",
            module: "Flows.DataProcessing.Steps.preprocess_companies",
            function: "preprocess_companies",
            input: catalog.Companies,
            output: catalog.PreprocessedCompanies,
            executor: executor
        );
    });
```

The Python side declares its schemas on the decorator:

```python
import pandas as pd
from flowthru import step

@step(inputs=["CompanySchema"], outputs=["PreprocessedCompanySchema"], cacheable=True)
def preprocess_companies(companies: pd.DataFrame) -> pd.DataFrame:
    companies["iata_approved"] = companies["iata_approved"] == "t"
    return companies
```
