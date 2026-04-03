# Kedro Spaceflights (Python Steps)

A Flowthru example demonstrating Python node integration using the [Kedro Spaceflights](https://github.com/kedro-org/kedro-starters) tutorial as a reference.

## Overview

This project replicates the Kedro Spaceflights data science pipeline using Flowthru's Python node extension. It demonstrates:

- **Mixed C#/Python pipelines** — Python nodes for data preprocessing and ML training
- **Multi-I/O patterns** — 3×1 joins, 1×4 splits, 2×1 training operations
- **Apache Arrow marshalling** — Efficient DataFrame exchange via Arrow IPC
- **Pre-flight validation** — Schema mismatches caught before execution
- **Auto-generated Python schemas** — Type-safe imports from `flowthru_schemas`

## Project Structure

```
KedroSpaceflightsPython/
├── pyproject.toml              # Python dependencies (uv)
├── .python-version             # Pinned Python version
├── .venv/                      # Virtual environment (auto-detected)
├── Data/                       # Data catalog and schemas
│   ├── Catalog.cs
│   ├── _01_Raw/                # Raw data CSVs/Excel
│   └── _08_Reporting/          # Visualization outputs
└── Flows/
    ├── DataProcessing/         # Data preprocessing nodes (Python)
    ├── DataScience/            # ML training nodes (Python)
    └── Reporting/              # Visualization nodes (Python)
```

## Setup

Requires Python 3.10+. Install with `uv`:

```bash
cd examples/starter/KedroSpaceflightsPython
uv sync
```

## Running

```bash
dotnet run
```

Or via NX:

```bash
nx run KedroSpaceflightsPython:run
```

## Python Steps

All data preprocessing, ML, and visualization logic is implemented in Python:

- **Data Processing Flow:**
  - `preprocess_companies` — Clean company data
  - `preprocess_shuttles` — Clean shuttle data
  - `create_model_input_table` — Join datasets (3×1 inputs)

- **Data Science Flow:**
  - `split_data` — Split into train/test sets (1×4 outputs)
  - `train_model` — Train linear regression (2×1 inputs)
  - `evaluate_model` — Compute metrics (3×1 inputs)

- **Reporting Flow:**
  - `compare_passenger_capacity_exp` — Generate Plotly Express bar chart
  - `compare_passenger_capacity_go` — Generate Plotly Graph Objects bar chart
  - `create_confusion_matrix` — Generate matplotlib/seaborn confusion matrix heatmap

## Schema Generation

C# schemas in `Data/` are automatically exported to Python:

```bash
dotnet build  # Generates _generated/flowthru_schemas/
```

Python nodes import the generated schemas:

```python
from flowthru import node
from flowthru_schemas import CompanyRawSchema, CompanyPreprocessedSchema

@node(inputs=[CompanyRawSchema], outputs=[CompanyPreprocessedSchema])
def preprocess_companies(companies: pd.DataFrame) -> pd.DataFrame:
    # ...
```
