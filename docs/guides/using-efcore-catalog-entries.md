# Using EFCore Catalog Entries

This guide is for teams who already have a working Flowthru pipeline backed by file-based catalog entries (CSV, JSON, Parquet) and want to move some or all entries to a relational database. You should be comfortable with Entity Framework Core at a conceptual level — DbContext, `OnModelCreating`, and `IDbContextFactory`.

## Install the extension

```bash
dotnet add package Flowthru.Extensions.EFCore
```

This package provides `EFCoreCatalogEntries`, the primary namespace for creating EFCore-backed catalog entries.

## What your schemas need

FlowthruSchemas work as EF entities without modification. The only requirement EF adds is a primary key — EF cannot infer one by convention from `record` types, so you must declare it explicitly in `OnModelCreating`.

```csharp
// Data/_02_Intermediate/Schemas/PreprocessedCompany.cs
[FlowthruSchema]
public partial record PreprocessedCompany
{
    public required int Id { get; init; }
    public required string Name { get; init; }
    public required double Rating { get; init; }
}
```

No new attributes on the schema. The key configuration goes in the DbContext, not on the type.

## Create a DbContext

```csharp
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PreprocessedCompany>().HasKey(e => e.Id);
        // Add HasKey() for each entity type you plan to use as a catalog entry
    }
}
```

The adapter calls `context.Set<T>()` directly, so explicit `DbSet<T>` properties are optional. They're useful for testing or querying from your own code, but the Flowthru adapter doesn't require them.

## Register the factory in DI

Always register `IDbContextFactory<TContext>` — not `AddDbContext`. Flowthru executes nodes concurrently, and `IDbContextFactory` is EFCore's answer to that: each operation gets a fresh, isolated context.

```csharp
// Program.cs
services.AddDbContextFactory<AppDbContext>(opts =>
    opts.UseNpgsql(connectionString)
    // or: opts.UseSqlite("Data Source=pipeline.db")
    // or: opts.UseSqlServer(connectionString)
);
```

## Accept the factory in your catalog

Pass `IDbContextFactory<TContext>` into your catalog constructor. Call `EnsureCreated()` (or run migrations) before initializing catalog properties so the schema exists when entries are first accessed.

```csharp
// Data/Catalog.cs
public partial class Catalog : DataCatalogBase
{
    private readonly string _basePath;
    private readonly IDbContextFactory<AppDbContext> _contextFactory;

    public Catalog(string basePath, IDbContextFactory<AppDbContext> contextFactory)
    {
        _basePath = basePath;
        _contextFactory = contextFactory;

        // Create the database schema on first run.
        // For production pipelines, use migrations instead of EnsureCreated.
        using var ctx = contextFactory.CreateDbContext();
        ctx.Database.EnsureCreated();

        InitializeCatalogProperties();
    }
}
```

Wire the factory from your DI container:

```csharp
// Program.cs
flowthru.UseCatalog(sp => new Catalog(
    basePath: Path.Combine(basePath, "Data"),
    contextFactory: sp.GetRequiredService<IDbContextFactory<AppDbContext>>()
));
```

## Create catalog entries

Use the typed overload `EFCore<T, TContext>` with your `IDbContextFactory`. This is the recommended pattern for all new pipeline code:

```csharp
// Data/_02_Intermediate/Catalog.Intermediate.cs
using Flowthru.Extensions.EFCore.Data;

public partial class Catalog
{
    public ICatalogEntry<IEnumerable<PreprocessedCompany>> PreprocessedCompanies =>
        GetOrCreateEntry(() =>
            EFCoreCatalogEntries.Enumerable.EFCore<PreprocessedCompany, AppDbContext>(
                label: "PreprocessedCompanies",
                contextFactory: _contextFactory
            )
        );
}
```

The `EFCore<T, TContext>` overload preserves the concrete context type through to save delegates — no casting inside callbacks.

## Single-entity entries

For tables that store exactly one row — a trained model, a configuration record, aggregated metrics — use the `Single` variant. The adapter enforces exactly one row during pre-flight validation and throws if the table is empty or has multiple rows.

```csharp
// Data/_06_Models/Catalog.Models.cs
public partial class Catalog
{
    public ICatalogEntry<TrainedModel> Regressor =>
        GetOrCreateEntry(() =>
            EFCoreCatalogEntries.Single.EFCore<TrainedModel, AppDbContext>(
                label: "Regressor",
                contextFactory: _contextFactory
            )
        );
}
```

See [allowEmptyData](#optional-tables) below if the entry may legitimately be empty on first run.

## Customizing the load query

The `queryCustomizer` parameter receives the raw `IQueryable<T>` before the adapter executes it. Use it for ordering, filtering, or loading navigation properties:

```csharp
// Order for deterministic output
EFCoreCatalogEntries.Enumerable.EFCore<Shuttle, AppDbContext>(
    label: "Shuttles",
    contextFactory: _contextFactory,
    queryCustomizer: q => q.OrderBy(s => s.Id)
)

// Load with a navigation property
EFCoreCatalogEntries.Enumerable.EFCore<Person, AppDbContext>(
    label: "Persons",
    contextFactory: _contextFactory,
    queryCustomizer: q => q.Include(p => p.Address).AsNoTracking()
)

// Filter to a subset
EFCoreCatalogEntries.Enumerable.EFCore<Record, AppDbContext>(
    label: "ActiveRecords",
    contextFactory: _contextFactory,
    queryCustomizer: q => q.Where(r => r.IsActive)
)
```

The `queryCustomizer` is purely for shaping the query — you don't call `ToListAsync()` yourself; the adapter handles execution.

## Custom save strategies

The default save strategy is `RemoveRange` + `AddRange` (full replace semantics, matching file-based adapters). Override it with `saveFunc` when you need something different:

```csharp
// Upsert instead of replace
EFCoreCatalogEntries.Enumerable.EFCore<Company, AppDbContext>(
    label: "Companies",
    contextFactory: _contextFactory,
    saveFunc: async (ctx, data, ct) =>
    {
        foreach (var company in data)
        {
            var existing = await ctx.Set<Company>().FindAsync(new object[] { company.Id }, ct);
            if (existing is null)
                ctx.Set<Company>().Add(company);
            else
                ctx.Entry(existing).CurrentValues.SetValues(company);
        }
        await ctx.SaveChangesAsync(ct);
    }
)
```

For provider-specific strategies (for example, PostgreSQL `TRUNCATE CASCADE`), extract the delegate into a static helper class so the catalog stays readable and the strategy is testable in isolation:

```csharp
// Data/Storage/MyProjectSaveFuncs.cs
internal static class MyProjectSaveFuncs
{
    public static async Task ReplaceCompanies(
        AppDbContext ctx,
        IEnumerable<Company> data,
        CancellationToken ct)
    {
        await ctx.Database.ExecuteSqlRawAsync("TRUNCATE TABLE companies", ct);
        await ctx.Set<Company>().AddRangeAsync(data, ct);
        await ctx.SaveChangesAsync(ct);
    }
}

// In your catalog entry:
EFCoreCatalogEntries.Enumerable.EFCore<Company, AppDbContext>(
    label: "Companies",
    contextFactory: _contextFactory,
    saveFunc: MyProjectSaveFuncs.ReplaceCompanies
)
```

To add a wrapper around the default save (e.g., logging or metrics), reference `EFCoreStorageAdapter<T>.DefaultSave` directly:

```csharp
saveFunc: async (ctx, data, ct) =>
{
    _logger.LogInformation("Saving {Count} records", data.Count());
    await EFCoreStorageAdapter<Company>.DefaultSave(ctx, data, ct);
}
```

## Read-only entries

Use `.Constrain()` to mark source or reference tables as read-only. Flowthru will fail at pipeline build time — before any data moves — if a node attempts to write to this entry:

```csharp
EFCoreCatalogEntries.Enumerable.EFCore<SourceRecord, SourceDbContext>(
    label: "SourceRecords",
    contextFactory: _sourceFactory)
.Constrain(traits => traits with { CanWrite = false })
```

See [Constraining Catalog Entries](constraining-catalog-entries.md) for the full constraint API.

## Optional tables

By default, an empty table fails pre-flight validation. The adapter expects each catalog entry to have data before the pipeline runs. For tables that are legitimately empty on first run — audit logs, optional output tables, or incremental pipelines — set `allowEmptyData: true`:

```csharp
EFCoreCatalogEntries.Enumerable.EFCore<AuditEvent, AppDbContext>(
    label: "AuditEvents",
    contextFactory: _contextFactory,
    allowEmptyData: true
)
```

The `Single` adapter also supports this: without `allowEmptyData: true`, an empty table is a validation failure.

## Schema initialization

`EnsureCreated()` is the simplest way to initialize the database for non-production pipelines. For production, use EF migrations:

```csharp
// Development / starter pattern: EnsureCreated in catalog constructor
using var ctx = contextFactory.CreateDbContext();
ctx.Database.EnsureCreated();

// Production pattern: run migrations at startup before catalog is resolved
using var ctx = sp.GetRequiredService<IDbContextFactory<AppDbContext>>().CreateDbContext();
await ctx.Database.MigrateAsync();
```

For non-EF schema setup (e.g., PostgreSQL `CREATE SCHEMA IF NOT EXISTS`), handle this in `Program.cs` before registering the catalog — the catalog constructor should only see a schema that already exists.
