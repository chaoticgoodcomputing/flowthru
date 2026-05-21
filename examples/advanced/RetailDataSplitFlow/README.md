# RetailDataSplitFlow

<!-- flowthru:mermaid:start -->
```mermaid
flowchart TB

    %% External Data Inputs
    CountryCurrencies[("CountryCurrencies")]
    OfxRates[("OfxRates")]
    RetailTransactionsRaw[("RetailTransactionsRaw")]

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
    end

    subgraph Consolidation["Consolidation"]
        ConsolidateShards["ConsolidateShards"]
        AllCountriesWeeklyDtu[("AllCountriesWeeklyDtu")]
    end

    subgraph DataIngestion["DataIngestion"]
        ValidateCsvTransactions["ValidateCsvTransactions"]
        AllRetailTransactions[("AllRetailTransactions")]
    end

    subgraph Graphing["Graphing"]
        PlotDollarsChart["PlotDollarsChart"]
        DollarsChart[("DollarsChart")]
        PlotTransactionsChart["PlotTransactionsChart"]
        TransactionsChart[("TransactionsChart")]
        PlotUsersChart["PlotUsersChart"]
        UsersChart[("UsersChart")]
    end

    subgraph Reporting["Reporting"]
        SummarizeByCountry["SummarizeByCountry"]
        CountryTransactionSummary[("CountryTransactionSummary")]
    end

    %% Edges
    RetailTransactionsRaw --> ValidateCsvTransactions
    ValidateCsvTransactions --> AllRetailTransactions
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
    AllRetailTransactions --> SummarizeByCountry
    SummarizeByCountry --> CountryTransactionSummary
    WeeklyDtu_germany --> ConsolidateShards
    WeeklyDtu_france --> ConsolidateShards
    WeeklyDtu_eire --> ConsolidateShards
    WeeklyDtu_spain --> ConsolidateShards
    WeeklyDtu_netherlands --> ConsolidateShards
    ConsolidateShards --> AllCountriesWeeklyDtu
    AllCountriesWeeklyDtu --> PlotDollarsChart
    PlotDollarsChart --> DollarsChart
    AllCountriesWeeklyDtu --> PlotTransactionsChart
    PlotTransactionsChart --> TransactionsChart
    AllCountriesWeeklyDtu --> PlotUsersChart
    PlotUsersChart --> UsersChart

```
<!-- flowthru:mermaid:end -->
