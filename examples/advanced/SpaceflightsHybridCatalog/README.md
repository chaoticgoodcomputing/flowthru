# SpaceflightsHybridCatalog

A Flowthru pipeline that swaps its data backend at startup based on the
`ASPNETCORE_ENVIRONMENT` environment variable. Flow factories and step
transforms never see which backend is in play — the abstract
[`Catalog`](Data/Catalog.cs) is resolved from DI, and the runtime picks
either the file-backed [`DevelopmentCatalog`](Data/DevelopmentCatalog.cs) or
the EFCore-backed [`ProductionCatalog`](Data/ProductionCatalog.cs).

## Why

`KedroSpaceflights` (file-backed) and `SpaceflightsEFCore` (DB-backed) are
two ways of running the same logical pipeline. In practice most teams want
both — interactive iteration on flat files locally, plus transactional
SQL persistence for deployed runs — without rewriting flows or steps. This
example shows that the only thing that has to vary is the catalog
implementation; everything downstream (steps, flow factories, configuration)
stays identical.

## The DI swap

The whole switch is one factory in [Program.cs](Program.cs):

```csharp
var isProduction = string.Equals(
  Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"),
  "Production",
  StringComparison.OrdinalIgnoreCase);

flowthru.RegisterCatalog<Catalog>(sp => isProduction
  ? new ProductionCatalog(
      basePath: dataPath,
      contextFactory: sp.GetRequiredService<IDbContextFactory<SpaceflightsDbContext>>())
  : new DevelopmentCatalog(basePath: dataPath));
```

The registration is typed on the abstract base, so
`RegisterFlow<Catalog>(…)` resolves the same singleton regardless of which
subclass was constructed. The `ASPNETCORE_ENVIRONMENT` convention mirrors
ASP.NET Core hosting — handy when this service is embedded alongside an API
host that already keys off the same variable.

## Layout

Items are split across three catalog files:

| File | Role |
| --- | --- |
| [Data/Catalog.cs](Data/Catalog.cs) | Abstract base. Declares every divergent item as `abstract`. |
| [Data/\_01\_Raw/Catalog.Raw.cs](Data/_01_Raw/Catalog.Raw.cs), [Data/\_08\_Reporting/Catalog.Reporting.cs](Data/_08_Reporting/Catalog.Reporting.cs) | Concrete shared items on the base — raw CSV/Excel inputs and the JSON report output. These are the same in both environments. |
| [Data/DevelopmentCatalog.cs](Data/DevelopmentCatalog.cs) | File-backed overrides (Parquet / JSON / Memory). |
| [Data/ProductionCatalog.cs](Data/ProductionCatalog.cs) | EFCore overrides backed by [SpaceflightsDbContext](Data/SpaceflightsDbContext.cs). |

`CheckStatus` uses `[SerializedEnum("t"/"f")]` for file round-tripping; the
DbContext adds `HasConversion<string>()` so EF stores the enum member name
in SQLite. The two on-disk representations are intentionally independent.

## Running

```bash
# Development (default) — reads/writes Parquet, JSON, Excel under Data/
dotnet run --project examples/advanced/SpaceflightsHybridCatalog -- run

# Production — persists every intermediate to Data/spaceflights.db
ASPNETCORE_ENVIRONMENT=Production \
  dotnet run --project examples/advanced/SpaceflightsHybridCatalog -- run
```

Layered config files (`appsettings.json`, `appsettings.Development.json`,
`appsettings.Production.json`) follow the same `ASPNETCORE_ENVIRONMENT`
convention.
