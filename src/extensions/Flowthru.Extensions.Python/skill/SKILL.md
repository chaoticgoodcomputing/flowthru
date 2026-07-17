---
name: flowthru-python
description: Deep skill for the Flowthru Python extension — running Python (pandas/scikit-learn/numpy) functions as typed Steps inside a Flowthru (.NET) Flow. Use when a project has Python steps, a .py module with @step functions, or a data-science transform that belongs in Python. Pairs with the umbrella `flowthru` skill.
metadata:
  flowthru:
    extension: Flowthru.Extensions.Python
    surface: step
    capability: Run Python (pandas/scikit-learn) functions as typed Steps; rows cross the boundary as Arrow → pandas.DataFrame.
    register: b.UsePython(…)
---

# flowthru-python

Lets a Step's body live in **Python** instead of C#. A `@step`-decorated function in a `.py` module becomes a first-class Step in the DAG — wired between typed Catalog Items, validated at pre-flight, and cached like any C# Step. Rows cross the boundary as Apache Arrow, so a Step handing `IEnumerable<TSchema>` to Python receives a `pandas.DataFrame` and returns one, with no glue code.

**Reach for it** when the transform is naturally a `pandas`/`scikit-learn`/`numpy` operation. The Flow doesn't care which side a Step runs on; keep row-at-a-time plumbing in C# and put the data-science body in Python.

## Register

```bash
dotnet add package Flowthru.Extensions.Python
```

Enable Python inside `AddFlowthru`, pointing it at your `.py` modules and interpreter:

<!-- flowthru:snippet:docs:register-python:start -->
```csharp
flowthru.UsePython(python =>
{
  python.ModuleSearchPaths.Add(basePath);
  python.ModuleSearchPaths.Add(outputPath);
  python.VenvPath = outputPath;
});
```
_(source: [`SpaceflightsPython/Program.cs`](https://github.com/chaoticgoodcomputing/flowthru/blob/main/examples/starter/SpaceflightsPython/Program.cs))_
<!-- flowthru:snippet:docs:register-python:end -->

Then register the flow with **`IPythonExecutor`** as the second generic, so the runner injects the executor into the flow factory: `b.RegisterFlow<Catalog, IPythonExecutor>("DataProcessing", DataProcessingFlow.Create)`.

## Wire a Python step

`AddPythonStep` points a step at a module + function and wires it between typed Catalog Items exactly like a C# step:

<!-- flowthru:snippet:docs:step-python-wire:start -->
```csharp
pipeline.AddPythonStep(
  label: "PreprocessCompanies",
  module: "Flows.DataProcessing.Steps.preprocess_companies",
  function: "preprocess_companies",
  input: catalog.Companies,
  output: catalog.PreprocessedCompanies,
  executor: executor
);
```
_(source: [`SpaceflightsPython/DataProcessingFlow.cs`](https://github.com/chaoticgoodcomputing/flowthru/blob/main/examples/starter/SpaceflightsPython/Flows/DataProcessing/DataProcessingFlow.cs))_
<!-- flowthru:snippet:docs:step-python-wire:end -->

The Python side declares its input/output schemas on the `@step` decorator — the names must match the Catalog Items wired on the C# side, and the pre-flight check verifies it. The body is ordinary `pandas`:

<!-- flowthru:snippet:docs:step-python-def:start -->
```python
@step(inputs=["CompanySchema"], outputs=["PreprocessedCompanySchema"], cacheable=True)
def preprocess_companies(companies: pd.DataFrame) -> pd.DataFrame:
```
_(source: [`SpaceflightsPython/preprocess_companies.py`](https://github.com/chaoticgoodcomputing/flowthru/blob/main/examples/starter/SpaceflightsPython/Flows/DataProcessing/Steps/preprocess_companies.py))_
<!-- flowthru:snippet:docs:step-python-def:end -->

## Notes

- **Serialized execution.** Python steps run through a single subprocess worker, so the scheduler runs them one at a time. Fan-out parallelism is a C#-side property, not a Python one — don't expect two Python steps to run concurrently.
- **Diagnostics** are namespaced `FTPY####`; a schema or module/function mismatch surfaces at pre-flight, not mid-run.
