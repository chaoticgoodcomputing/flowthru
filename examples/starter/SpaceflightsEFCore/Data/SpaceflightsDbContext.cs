using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SpaceflightsEFCore.Data._02_Intermediate.Schemas;
using SpaceflightsEFCore.Data._03_Primary.Schemas;
using SpaceflightsEFCore.Data._05_ModelInput.Schemas;
using SpaceflightsEFCore.Data._06_Models.Schemas;
using SpaceflightsEFCore.Data._07_ModelOutput.Schemas;

namespace SpaceflightsEFCore.Data;

/// <summary>
/// Entity Framework Core DbContext for the Spaceflights pipeline.
/// Provides access to all intermediate, primary, and model data as SQLite database tables.
/// </summary>
/// <remarks>
/// This DbContext is configured to use a SQLite file database, which provides:
/// - FK enforcement and realistic relational semantics not available with InMemory
/// - A persistent file that can be inspected between runs
/// - Concurrency-safe access when used via <see cref="Microsoft.EntityFrameworkCore.IDbContextFactory{TContext}"/>
///
/// The database file is created at <c>Data/spaceflights.db</c> on first run via EnsureCreated().
/// </remarks>
public class SpaceflightsDbContext : DbContext
{
  public SpaceflightsDbContext(DbContextOptions<SpaceflightsDbContext> options)
    : base(options) { }

  // _02_Intermediate layer
  public DbSet<PreprocessedCompanySchema> PreprocessedCompanies => Set<PreprocessedCompanySchema>();
  public DbSet<PreprocessedShuttleSchema> PreprocessedShuttles => Set<PreprocessedShuttleSchema>();

  // _03_Primary layer
  public DbSet<ModelInputTableSchema> ModelInputTable => Set<ModelInputTableSchema>();

  // _05_ModelInput layer
  public DbSet<TrainingData> TrainingData => Set<TrainingData>();
  public DbSet<TestData> TestData => Set<TestData>();

  // _06_Models layer
  public DbSet<LinearRegressionModel> Models => Set<LinearRegressionModel>();

  // _07_ModelOutput layer
  public DbSet<ModelMetrics> ModelMetrics => Set<ModelMetrics>();
  public DbSet<ModelPredictions> ModelPredictions => Set<ModelPredictions>();

  protected override void OnModelCreating(ModelBuilder modelBuilder)
  {
    base.OnModelCreating(modelBuilder);

    // Configure PreprocessedCompanySchema
    modelBuilder.Entity<PreprocessedCompanySchema>(entity =>
    {
      entity.HasKey(e => e.Id);
      entity.Property(e => e.CompanyRating).HasPrecision(18, 6);
    });

    // Configure PreprocessedShuttleSchema
    modelBuilder.Entity<PreprocessedShuttleSchema>(entity =>
    {
      entity.HasKey(e => e.Id);
    });

    // Configure ModelInputTableSchema
    modelBuilder.Entity<ModelInputTableSchema>(entity =>
    {
      entity.HasKey(e => e.ShuttleId);
      entity.Property(e => e.CompanyRating).HasPrecision(18, 6);
      entity.Property(e => e.ReviewScoresRating).HasPrecision(18, 6);
      entity.Property(e => e.Price).HasPrecision(18, 6);
    });

    // Configure TrainingData - use auto-generated ID as EF can't key on owned types
    modelBuilder.Entity<TrainingData>(entity =>
    {
      entity.Property<int>("Id"); // Shadow property for key
      entity.HasKey("Id");
      entity.OwnsOne(
        e => e.Features,
        features =>
        {
          features.Property(f => f.CompanyRating).HasPrecision(18, 6);
          features.Property(f => f.ReviewScoresRating).HasPrecision(18, 6);
        }
      );
      entity.Property(e => e.Label).HasPrecision(18, 6);
    });

    // Configure TestData - use auto-generated ID
    modelBuilder.Entity<TestData>(entity =>
    {
      entity.Property<int>("Id"); // Shadow property for key
      entity.HasKey("Id");
      entity.OwnsOne(
        e => e.Features,
        features =>
        {
          features.Property(f => f.CompanyRating).HasPrecision(18, 6);
          features.Property(f => f.ReviewScoresRating).HasPrecision(18, 6);
        }
      );
      entity.Property(e => e.Label).HasPrecision(18, 6);
    });

    // Configure LinearRegressionModel - use auto-generated shadow property as key
    modelBuilder.Entity<LinearRegressionModel>(entity =>
    {
      entity.ToTable("LinearRegressionModels");
      entity.Property<int>("Id");
      entity.HasKey("Id");
      entity
        .Property(e => e.Coefficients)
        .HasColumnType("text")
        .HasConversion(
          v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
          v =>
            JsonSerializer.Deserialize<double[]>(v, (JsonSerializerOptions?)null)
            ?? Array.Empty<double>()
        );
      entity.Property(e => e.Intercept).IsRequired();
    });

    // Configure ModelMetrics - generate key
    modelBuilder.Entity<ModelMetrics>(entity =>
    {
      entity.HasKey(e => e.R2Score); // Use R2Score as identifier (single model scenario)
      entity.Property(e => e.R2Score).HasPrecision(18, 6);
      entity.Property(e => e.MeanAbsoluteError).HasPrecision(18, 6);
      entity.Property(e => e.MaxError).HasPrecision(18, 6);
    });

    // Configure ModelPredictions - generate key
    modelBuilder.Entity<ModelPredictions>(entity =>
    {
      entity.Property<int>("Id"); // Shadow property for key
      entity.HasKey("Id");
    });
  }
}
