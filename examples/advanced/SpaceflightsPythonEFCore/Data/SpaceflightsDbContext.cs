using Microsoft.EntityFrameworkCore;
using SpaceflightsPythonEFCore.Data._02_Intermediate.Schemas;
using SpaceflightsPythonEFCore.Data._03_Primary.Schemas;
using SpaceflightsPythonEFCore.Data._07_ModelOutput.Schemas;

namespace SpaceflightsPythonEFCore.Data;

/// <summary>
/// EFCore DbContext for the SpaceflightsPythonEFCore pipeline.
///
/// Tables managed here are the EFCore-backed layers:
///   - _02_Intermediate: PreprocessedCompanies, PreprocessedShuttles  (C# writes)
///   - _03_Primary:      ModelInputTable                               (C# writes, Python reads)
///   - _07_ModelOutput:  ModelPredictions                              (Python writes, Python reads)
///
/// All other layers (raw files, memory splits, JSON model/metrics, reporting files) bypass this context.
/// </summary>
public class SpaceflightsDbContext : DbContext
{
    public SpaceflightsDbContext(DbContextOptions<SpaceflightsDbContext> options)
      : base(options) { }

    public DbSet<PreprocessedCompanySchema> PreprocessedCompanies => Set<PreprocessedCompanySchema>();
    public DbSet<PreprocessedShuttleSchema> PreprocessedShuttles => Set<PreprocessedShuttleSchema>();
    public DbSet<ModelInputTableSchema> ModelInputTable => Set<ModelInputTableSchema>();
    public DbSet<ModelPredictions> ModelPredictions => Set<ModelPredictions>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<PreprocessedCompanySchema>(entity =>
        {
            entity.HasKey(e => e.Id);
        });

        modelBuilder.Entity<PreprocessedShuttleSchema>(entity =>
        {
            entity.HasKey(e => e.Id);
        });

        modelBuilder.Entity<ModelInputTableSchema>(entity =>
        {
            entity.HasKey(e => e.ShuttleId);
        });

        // ModelPredictions has no natural key; use a shadow integer property.
        modelBuilder.Entity<ModelPredictions>(entity =>
        {
            entity.Property<int>("Id");
            entity.HasKey("Id");
        });
    }
}
