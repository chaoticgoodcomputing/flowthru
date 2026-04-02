# EFCore Integration Example

This example demonstrates using `Flowthru.Extensions.EFCore` to read and write data from a SQLite database.

## What This Demonstrates

- ✅ FlowthruSchemas work unchanged as EF entities
- ✅ Database catalog entries as pipeline seeds (Layer 0 inputs)
- ✅ Injected DbContext lifecycle management
- ✅ Reading and writing entities via EFCore adapter
- ✅ Partial class pattern for extending `CatalogEntries` from external package

## Project Structure

```
examples/efcore-integration/
├── README.md                 # This file
├── Program.cs                # Pipeline execution
├── Data/
│   │── AppDbContext.cs       # EF Core DbContext
│   └── CompanySchema.cs      # FlowthruSchema entity
├── Catalog/
│   └── DataCatalog.cs        # Catalog entry definitions
└── Nodes/
    ├── ExtractCompaniesNode.cs
    └── TransformCompaniesNode.cs
```

## Running the Example

```bash
cd examples/efcore-integration
dotnet run
```

## Key Code Snippets

### 1. FlowthruSchema as EF Entity

```csharp
// Data/CompanySchema.cs
[FlowthruSchema]
public record CompanySchema(
    int Id,
    string Name,
    string Industry,
    int EmployeeCount,
    DateTime Founded
);

// Automatically implements IFlatSchema, IStructuredSerializable
// Works with both EFCore and file-based catalogs (CSV, JSON, etc.)
```

### 2. DbContext Configuration

```csharp
// Data/AppDbContext.cs
public class AppDbContext : DbContext
{
    public DbSet<CompanySchema> Companies { get; set; }
    
    protected override void OnConfiguring(DbContextOptionsBuilder options)
        => options.UseSqlite("Data Source=companies.db");
}
```

### 3. EFCore Catalog Entries

```csharp
// Catalog/DataCatalog.cs
public static partial class DataCatalog
{
    // Database source (seed)
    public static ICatalogEntry<IEnumerable<CompanySchema>> SourceCompanies(DbContext db) =>
        CatalogEntries.Enumerable.EFCore<CompanySchema>("source_companies", db, readOnly: true);
    
    // Database destination (output)
    public static ICatalogEntry<IEnumerable<CompanySchema>> ProcessedCompanies(DbContext db) =>
        CatalogEntries.Enumerable.EFCore<CompanySchema>("processed_companies", db);
}
```

### 4. Pipeline Using EFCore

```csharp
// Program.cs
using var db = new AppDbContext();
await db.Database.MigrateAsync();

var pipeline = new FlowBuilder("CompanyETL")
    .AddNode("extract", catalog => new ExtractCompaniesNode(
        inputs: catalog.SourceCompanies(db),
        outputs: catalog.RawCompanies()
    ))
    .AddNode("transform", catalog => new TransformCompaniesNode(
        inputs: catalog.RawCompanies(),
        outputs: catalog.ProcessedCompanies(db)
    ))
    .Build();

await pipeline.ExecuteAsync();
```

## Notes

- **Migrations:** Run before pipeline execution
- **Transactions:** Each Save() is an independent transaction
- **Read-Only:** Source catalog prevents accidental writes to production
- **Seedable:** Database tables are automatically detected as Layer 0 seeds

## Next Steps

- Try different database providers (SQL Server, PostgreSQL)
- Implement upsert semantics in a custom node
- Use factory-based DbContext for scoped patterns
- Combine EFCore with CSV/Parquet exports
