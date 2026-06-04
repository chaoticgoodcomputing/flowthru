# RetailDataSplitFlow Advanced

> [!NOTE]
> How do I shard a Flow across N parallel branches at runtime?

This project demonstrates a **Flow factory** pattern — one Step template is instantiated once per country from a config-driven list, producing N parallel analysis branches that fan back in through a variadic consolidation Step. Pure vanilla Flowthru — no extensions, no reflection, no source generators; just closure capture, `foreach`-driven Step registration, and a variadic `AddStep` overload.

This project:

- Ingests a 43 MB retail-transactions CSV from GitHub (HTTP-cached for 24h), enriched with a country-currency mapping and FX rates, and reduces it to weekly per-country DTU (Daily-Transacting-Users) records.
- Reads a list of country names from [`appsettings.json`](./appsettings.json) under `Analysis:Countries`, then instantiates the analysis Step once per country — five parallel branches in the default config (Germany, France, Eire, Spain, Netherlands), each producing a country-specific Parquet output.
- Consolidates the parallel outputs through a single fan-in Step (`ConsolidateShards`) that takes the N input Catalog Items as one variadic parameter and flattens them with `SelectMany`.
- Renders the consolidated dataset into Plotly PNG charts (dollars, transactions, users) via three Python Steps in the `Graphing` Flow.

**This is not a template** — `dotnet new` does not scaffold it, and the country list, dataset URL, and currency mapping are domain-specific to the retail example. The Flow shape is original to this repo (not Kedro-derived). Assumes you've worked through [Spaceflights](../../starter/Spaceflights/) and [SpaceflightsPython](../../starter/SpaceflightsPython/).

## Getting Started

Requires Python 3.10+ and the [`uv`](https://docs.astral.sh/uv/) CLI. Bootstrap the Python environment, then run:

```bash
uv sync
nx run RetailDataSplitFlow
```

First run downloads the 43 MB transactions CSV from GitHub (subsequent runs within 24h use the HTTP cache). The chart outputs land under [`Data/_08_Reporting/Charts/`](./Data/_08_Reporting/Charts/) — `dollars_chart.png`, `transactions_chart.png`, `users_chart.png` — and the country-summary CSV at [`Data/_08_Reporting/Datasets/country_transaction_summary.csv`](./Data/_08_Reporting/Datasets/country_transaction_summary.csv).

## Concepts

> **Reminder:** the patterns below illustrate vanilla Flowthru's compositional limits, **not** a template to clone. The country list, dataset, and currency mapping are domain-specific to the retail example.

- **[Step factory with a closure-captured parameter](./Flows/Analysis/Steps/ComputeWeeklyDtuStep.cs):** `ComputeWeeklyDtuStep.Create(string country)` returns a parametrized `Func<...>` that closes over the country string and filters transactions in its WHERE clause. The Step type is one C# class; instances are differentiated only by what the factory closure-captured at registration time.
- **[`foreach`-driven Flow registration](./Flows/Analysis/AnalysisFlow.cs):** the Flow's `Create(...)` iterates the configured country list and calls `pipeline.AddStep(...)` once per country, assigning a distinct label (`Analyze_germany`, `Analyze_france`, ...) via `Slugify(country)`. The DAG sees five Steps; the code defines one Step template.
- **[Per-shard Catalog Items via instance properties](./Data/_03_Primary/):** `CountryShardCatalog` is instantiated once per country. Its `WeeklyDtu` property builds an Item whose name and Parquet path are computed from `Slugify(Country)`, so `CountryShardCatalog("Germany", ...)` produces `WeeklyDtu_germany.parquet`. Per-shard Items materialize from the same Catalog class, parameterized by the constructor.
- **[Variadic fan-in for consolidation](./Flows/Consolidation/):** `ConsolidationFlow` uses the variadic `AddStep` overload — its `inputs:` parameter is a single `IEnumerable<IItem<T>>` (the N per-shard Items packed into one list), not N separate Item arguments. The transform receives `IEnumerable<IEnumerable<T>>` (a sequence of batches) and flattens with `SelectMany`. One declaration handles N inputs; growing N requires no change to the Flow code.
- **[Config-driven shard list](./appsettings.json):** the country list lives in `appsettings.json` under `Analysis:Countries`. Adding a sixth country (say, `"Italy"`) is a two-file edit: append the name to the list, then add a matching row to [`country_currencies.json`](./Data/_01_Raw/Datasets/country_currencies.json) so the analysis Step can resolve its currency. No Flow code changes — `Program.cs` reads the list at startup, builds a `CountryShardCatalog` per country, and threads the list into both `AnalysisFlow.Create` and `ConsolidationFlow.Create`.
- **[Python Steps for visualization](./Flows/Graphing/Steps/plot_dtu_charts.py):** three sibling `@step` functions (`plot_dollars_chart`, `plot_transactions_chart`, `plot_users_chart`) render Plotly line charts from the consolidated DataFrame — the same Arrow-IPC marshalling pattern from `SpaceflightsPython`, applied to one consolidated input rather than per-Step.

## Structure

### Diagram

<!-- flowthru:mermaid:start -->
#### Analysis

```mermaid
flowchart TB

    %% External Data Inputs
    CountryCurrencies[("CountryCurrencies")]
    OfxRates[("OfxRates")]

    subgraph DataIngestion_us["DataIngestion"]
        AllRetailTransactions[("AllRetailTransactions")]
    end

    subgraph Analysis["Analysis"]
        Analyze_germany["Analyze_germany"]
        WeeklyDtu_germany[("WeeklyDtu_germany")]
        Analyze_france["Analyze_france"]
        WeeklyDtu_france[("WeeklyDtu_france")]
        Analyze_eire["Analyze_eire"]
        WeeklyDtu_eire[("WeeklyDtu_eire")]
        Analyze_spain["Analyze_spain"]
        WeeklyDtu_spain[("WeeklyDtu_spain")]
        Analyze_netherlands["Analyze_netherlands"]
        WeeklyDtu_netherlands[("WeeklyDtu_netherlands")]
        Analyze_belgium["Analyze_belgium"]
        WeeklyDtu_belgium[("WeeklyDtu_belgium")]
        Analyze_switzerland["Analyze_switzerland"]
        WeeklyDtu_switzerland[("WeeklyDtu_switzerland")]
        Analyze_portugal["Analyze_portugal"]
        WeeklyDtu_portugal[("WeeklyDtu_portugal")]
        Analyze_australia["Analyze_australia"]
        WeeklyDtu_australia[("WeeklyDtu_australia")]
        Analyze_norway["Analyze_norway"]
        WeeklyDtu_norway[("WeeklyDtu_norway")]
        Analyze_italy["Analyze_italy"]
        WeeklyDtu_italy[("WeeklyDtu_italy")]
        Analyze_channel_islands["Analyze_channel_islands"]
        WeeklyDtu_channel_islands[("WeeklyDtu_channel_islands")]
        Analyze_finland["Analyze_finland"]
        WeeklyDtu_finland[("WeeklyDtu_finland")]
        Analyze_sweden["Analyze_sweden"]
        WeeklyDtu_sweden[("WeeklyDtu_sweden")]
        Analyze_denmark["Analyze_denmark"]
        WeeklyDtu_denmark[("WeeklyDtu_denmark")]
        Analyze_japan["Analyze_japan"]
        WeeklyDtu_japan[("WeeklyDtu_japan")]
    end

    subgraph Consolidation_ds["Consolidation"]
        ConsolidateShards["ConsolidateShards"]
    end

    %% Edges
    AllRetailTransactions --> Analyze_germany
    CountryCurrencies --> Analyze_germany
    OfxRates --> Analyze_germany
    Analyze_germany --> WeeklyDtu_germany
    AllRetailTransactions --> Analyze_france
    CountryCurrencies --> Analyze_france
    OfxRates --> Analyze_france
    Analyze_france --> WeeklyDtu_france
    AllRetailTransactions --> Analyze_eire
    CountryCurrencies --> Analyze_eire
    OfxRates --> Analyze_eire
    Analyze_eire --> WeeklyDtu_eire
    AllRetailTransactions --> Analyze_spain
    CountryCurrencies --> Analyze_spain
    OfxRates --> Analyze_spain
    Analyze_spain --> WeeklyDtu_spain
    AllRetailTransactions --> Analyze_netherlands
    CountryCurrencies --> Analyze_netherlands
    OfxRates --> Analyze_netherlands
    Analyze_netherlands --> WeeklyDtu_netherlands
    AllRetailTransactions --> Analyze_belgium
    CountryCurrencies --> Analyze_belgium
    OfxRates --> Analyze_belgium
    Analyze_belgium --> WeeklyDtu_belgium
    AllRetailTransactions --> Analyze_switzerland
    CountryCurrencies --> Analyze_switzerland
    OfxRates --> Analyze_switzerland
    Analyze_switzerland --> WeeklyDtu_switzerland
    AllRetailTransactions --> Analyze_portugal
    CountryCurrencies --> Analyze_portugal
    OfxRates --> Analyze_portugal
    Analyze_portugal --> WeeklyDtu_portugal
    AllRetailTransactions --> Analyze_australia
    CountryCurrencies --> Analyze_australia
    OfxRates --> Analyze_australia
    Analyze_australia --> WeeklyDtu_australia
    AllRetailTransactions --> Analyze_norway
    CountryCurrencies --> Analyze_norway
    OfxRates --> Analyze_norway
    Analyze_norway --> WeeklyDtu_norway
    AllRetailTransactions --> Analyze_italy
    CountryCurrencies --> Analyze_italy
    OfxRates --> Analyze_italy
    Analyze_italy --> WeeklyDtu_italy
    AllRetailTransactions --> Analyze_channel_islands
    CountryCurrencies --> Analyze_channel_islands
    OfxRates --> Analyze_channel_islands
    Analyze_channel_islands --> WeeklyDtu_channel_islands
    AllRetailTransactions --> Analyze_finland
    CountryCurrencies --> Analyze_finland
    OfxRates --> Analyze_finland
    Analyze_finland --> WeeklyDtu_finland
    AllRetailTransactions --> Analyze_sweden
    CountryCurrencies --> Analyze_sweden
    OfxRates --> Analyze_sweden
    Analyze_sweden --> WeeklyDtu_sweden
    AllRetailTransactions --> Analyze_denmark
    CountryCurrencies --> Analyze_denmark
    OfxRates --> Analyze_denmark
    Analyze_denmark --> WeeklyDtu_denmark
    AllRetailTransactions --> Analyze_japan
    CountryCurrencies --> Analyze_japan
    OfxRates --> Analyze_japan
    Analyze_japan --> WeeklyDtu_japan
    WeeklyDtu_germany --> ConsolidateShards
    WeeklyDtu_france --> ConsolidateShards
    WeeklyDtu_eire --> ConsolidateShards
    WeeklyDtu_spain --> ConsolidateShards
    WeeklyDtu_netherlands --> ConsolidateShards
    WeeklyDtu_belgium --> ConsolidateShards
    WeeklyDtu_switzerland --> ConsolidateShards
    WeeklyDtu_portugal --> ConsolidateShards
    WeeklyDtu_australia --> ConsolidateShards
    WeeklyDtu_norway --> ConsolidateShards
    WeeklyDtu_italy --> ConsolidateShards
    WeeklyDtu_channel_islands --> ConsolidateShards
    WeeklyDtu_finland --> ConsolidateShards
    WeeklyDtu_sweden --> ConsolidateShards
    WeeklyDtu_denmark --> ConsolidateShards
    WeeklyDtu_japan --> ConsolidateShards

    classDef collapsed stroke-dasharray:5 5,fill:transparent
    class DataIngestion_us,Consolidation_ds collapsed
```

#### Consolidation

```mermaid
flowchart TB

    subgraph Analysis_us["Analysis"]
        WeeklyDtu_australia[("WeeklyDtu_australia")]
        WeeklyDtu_belgium[("WeeklyDtu_belgium")]
        WeeklyDtu_channel_islands[("WeeklyDtu_channel_islands")]
        WeeklyDtu_denmark[("WeeklyDtu_denmark")]
        WeeklyDtu_eire[("WeeklyDtu_eire")]
        WeeklyDtu_finland[("WeeklyDtu_finland")]
        WeeklyDtu_france[("WeeklyDtu_france")]
        WeeklyDtu_germany[("WeeklyDtu_germany")]
        WeeklyDtu_italy[("WeeklyDtu_italy")]
        WeeklyDtu_japan[("WeeklyDtu_japan")]
        WeeklyDtu_netherlands[("WeeklyDtu_netherlands")]
        WeeklyDtu_norway[("WeeklyDtu_norway")]
        WeeklyDtu_portugal[("WeeklyDtu_portugal")]
        WeeklyDtu_spain[("WeeklyDtu_spain")]
        WeeklyDtu_sweden[("WeeklyDtu_sweden")]
        WeeklyDtu_switzerland[("WeeklyDtu_switzerland")]
    end

    subgraph Consolidation["Consolidation"]
        ConsolidateShards["ConsolidateShards"]
        AllCountriesWeeklyDtu[("AllCountriesWeeklyDtu")]
    end

    subgraph Graphing_ds["Graphing"]
        PlotDollarsChart["PlotDollarsChart"]
        PlotTransactionsChart["PlotTransactionsChart"]
        PlotUsersChart["PlotUsersChart"]
    end

    %% Edges
    WeeklyDtu_germany --> ConsolidateShards
    WeeklyDtu_france --> ConsolidateShards
    WeeklyDtu_eire --> ConsolidateShards
    WeeklyDtu_spain --> ConsolidateShards
    WeeklyDtu_netherlands --> ConsolidateShards
    WeeklyDtu_belgium --> ConsolidateShards
    WeeklyDtu_switzerland --> ConsolidateShards
    WeeklyDtu_portugal --> ConsolidateShards
    WeeklyDtu_australia --> ConsolidateShards
    WeeklyDtu_norway --> ConsolidateShards
    WeeklyDtu_italy --> ConsolidateShards
    WeeklyDtu_channel_islands --> ConsolidateShards
    WeeklyDtu_finland --> ConsolidateShards
    WeeklyDtu_sweden --> ConsolidateShards
    WeeklyDtu_denmark --> ConsolidateShards
    WeeklyDtu_japan --> ConsolidateShards
    ConsolidateShards --> AllCountriesWeeklyDtu
    AllCountriesWeeklyDtu --> PlotDollarsChart
    AllCountriesWeeklyDtu --> PlotTransactionsChart
    AllCountriesWeeklyDtu --> PlotUsersChart

    classDef collapsed stroke-dasharray:5 5,fill:transparent
    class Analysis_us,Graphing_ds collapsed
```

#### DataIngestion

```mermaid
flowchart TB

    %% External Data Inputs
    RetailTransactionsRaw[("RetailTransactionsRaw")]

    subgraph DataIngestion["DataIngestion"]
        ValidateCsvTransactions["ValidateCsvTransactions"]
        AllRetailTransactions[("AllRetailTransactions")]
    end

    subgraph Analysis_ds["Analysis"]
        Analyze_australia["Analyze_australia"]
        Analyze_belgium["Analyze_belgium"]
        Analyze_channel_islands["Analyze_channel_islands"]
        Analyze_denmark["Analyze_denmark"]
        Analyze_eire["Analyze_eire"]
        Analyze_finland["Analyze_finland"]
        Analyze_france["Analyze_france"]
        Analyze_germany["Analyze_germany"]
        Analyze_italy["Analyze_italy"]
        Analyze_japan["Analyze_japan"]
        Analyze_netherlands["Analyze_netherlands"]
        Analyze_norway["Analyze_norway"]
        Analyze_portugal["Analyze_portugal"]
        Analyze_spain["Analyze_spain"]
        Analyze_sweden["Analyze_sweden"]
        Analyze_switzerland["Analyze_switzerland"]
    end

    subgraph Reporting_ds["Reporting"]
        SummarizeByCountry["SummarizeByCountry"]
    end

    %% Edges
    RetailTransactionsRaw --> ValidateCsvTransactions
    ValidateCsvTransactions --> AllRetailTransactions
    AllRetailTransactions --> Analyze_australia
    AllRetailTransactions --> Analyze_belgium
    AllRetailTransactions --> Analyze_channel_islands
    AllRetailTransactions --> Analyze_denmark
    AllRetailTransactions --> Analyze_eire
    AllRetailTransactions --> Analyze_finland
    AllRetailTransactions --> Analyze_france
    AllRetailTransactions --> Analyze_germany
    AllRetailTransactions --> Analyze_italy
    AllRetailTransactions --> Analyze_japan
    AllRetailTransactions --> Analyze_netherlands
    AllRetailTransactions --> Analyze_norway
    AllRetailTransactions --> Analyze_portugal
    AllRetailTransactions --> Analyze_spain
    AllRetailTransactions --> Analyze_sweden
    AllRetailTransactions --> Analyze_switzerland
    AllRetailTransactions --> SummarizeByCountry

    classDef collapsed stroke-dasharray:5 5,fill:transparent
    class Analysis_ds,Reporting_ds collapsed
```

#### Graphing

```mermaid
flowchart TB

    subgraph Consolidation_us["Consolidation"]
        AllCountriesWeeklyDtu[("AllCountriesWeeklyDtu")]
    end

    subgraph Graphing["Graphing"]
        PlotDollarsChart["PlotDollarsChart<br>──<br>IPythonExecutor"]
        DollarsChart[("DollarsChart")]
        PlotTransactionsChart["PlotTransactionsChart<br>──<br>IPythonExecutor"]
        TransactionsChart[("TransactionsChart")]
        PlotUsersChart["PlotUsersChart<br>──<br>IPythonExecutor"]
        UsersChart[("UsersChart")]
    end

    %% Service legend
    subgraph service_legend["services"]
        svc_Flowthru_Step_Python_IPythonExecutor["IPythonExecutor<br>• cache: neutral<br>• cap: 1"]
    end
    style service_legend fill:#EEF4FF,stroke:#3B6FB0
    classDef serviceNode fill:#EEF4FF,stroke:#3B6FB0
    class svc_Flowthru_Step_Python_IPythonExecutor serviceNode

    %% Edges
    AllCountriesWeeklyDtu --> PlotDollarsChart
    PlotDollarsChart --> DollarsChart
    AllCountriesWeeklyDtu --> PlotTransactionsChart
    PlotTransactionsChart --> TransactionsChart
    AllCountriesWeeklyDtu --> PlotUsersChart
    PlotUsersChart --> UsersChart

    classDef collapsed stroke-dasharray:5 5,fill:transparent
    class Consolidation_us collapsed
```

#### Reporting

```mermaid
flowchart TB

    subgraph DataIngestion_us["DataIngestion"]
        AllRetailTransactions[("AllRetailTransactions")]
    end

    subgraph Reporting["Reporting"]
        SummarizeByCountry["SummarizeByCountry"]
        CountryTransactionSummary[("CountryTransactionSummary")]
    end

    %% Edges
    AllRetailTransactions --> SummarizeByCountry
    SummarizeByCountry --> CountryTransactionSummary

    classDef collapsed stroke-dasharray:5 5,fill:transparent
    class DataIngestion_us collapsed
```
<!-- flowthru:mermaid:end -->

### Files

<!-- flowthru:filetree:start -->
```
RetailDataSplitFlow/
├── Program.cs  # entry point
├── Data/
│   ├── CoreCatalog.cs
│   ├── _01_Raw/
│   │   ├── CoreCatalog.Raw.cs
│   │   ├── Datasets/
│   │   │   ├── country_currencies.json
│   │   │   ├── LICENSE.md
│   │   │   └── ofx_rates.json
│   │   └── Schemas/
│   │       ├── CountryCurrencySchema.cs
│   │       ├── OfxRateResponseSchema.cs
│   │       └── RetailTransactionSchema.cs
│   ├── ...
│   └── _08_Reporting/
│       ├── CoreCatalog.Reporting.cs
│       ├── Charts/
│       │   ├── dollars_chart.png
│       │   ├── transactions_chart.png
│       │   └── users_chart.png
│       ├── Datasets/
│       │   └── country_transaction_summary.csv
│       └── Schemas/
│           └── CountryTransactionSummarySchema.cs
└── Flows/
    ├── Analysis/
    │   └── Steps/
    │       └── ComputeWeeklyDtuStep.cs
    ├── Consolidation/
    ├── DataIngestion/
    │   └── Steps/
    │       └── ValidateCsvStep.cs
    ├── Graphing/
    │   ├── __init__.py
    │   ├── __pycache__/
    │   │   ├── __init__.cpython-310.pyc
    │   │   └── __init__.cpython-313.pyc
    │   └── Steps/
    │       ├── __init__.py
    │       ├── plot_dtu_charts.py
    │       └── __pycache__/
    │           ├── __init__.cpython-310.pyc
    │           ├── __init__.cpython-313.pyc
    │           ├── plot_dtu_charts.cpython-310.pyc
    │           └── plot_dtu_charts.cpython-313.pyc
    └── Reporting/
        └── Steps/
            └── SummarizeByCountryStep.cs
```
<!-- flowthru:filetree:end -->
