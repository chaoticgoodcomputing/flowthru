using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SpaceflightsHybridCatalog.Data._02_Intermediate.Schemas;
using SpaceflightsHybridCatalog.Data._03_Primary.Schemas;
using SpaceflightsHybridCatalog.Data._05_ModelInput.Schemas;
using SpaceflightsHybridCatalog.Data._06_Models.Schemas;
using SpaceflightsHybridCatalog.Data._07_ModelOutput.Schemas;

namespace SpaceflightsHybridCatalog.Data;

/// <summary>
/// EFCore DbContext backing <see cref="ProductionCatalog"/>. Persists every
/// intermediate, primary, and model artifact to a SQLite database file at
/// <c>Data/spaceflights.db</c>.
/// </summary>
public class SpaceflightsDbContext : DbContext
{
  public SpaceflightsDbContext(DbContextOptions<SpaceflightsDbContext> options)
    : base(options) { }

  public DbSet<PreprocessedCompanySchema> PreprocessedCompanies => Set<PreprocessedCompanySchema>();
  public DbSet<PreprocessedShuttleSchema> PreprocessedShuttles => Set<PreprocessedShuttleSchema>();
  public DbSet<ModelInputTableSchema> ModelInputTable => Set<ModelInputTableSchema>();
  public DbSet<TrainingData> TrainingData => Set<TrainingData>();
  public DbSet<TestData> TestData => Set<TestData>();
  public DbSet<LinearRegressionModel> Models => Set<LinearRegressionModel>();
  public DbSet<ModelMetrics> ModelMetrics => Set<ModelMetrics>();
  public DbSet<ModelPredictions> ModelPredictions => Set<ModelPredictions>();

  protected override void OnModelCreating(ModelBuilder modelBuilder)
  {
    base.OnModelCreating(modelBuilder);

    // CheckStatus is an enum carrying a [SerializedEnum("t"/"f")] mapping for
    // file formats. EFCore needs its own value converter — `.HasConversion<string>()`
    // tells EF to store the .NET enum member name ("Complete"/"Incomplete") as
    // text. The two on-disk representations are intentionally different: the
    // file backend round-trips via the [SerializedEnum] codec, the DB backend
    // round-trips via this converter.

    modelBuilder.Entity<PreprocessedCompanySchema>(entity =>
    {
      entity.HasKey(e => e.Id);
      entity.Property(e => e.CompanyRating).HasPrecision(18, 6);
    });

    modelBuilder.Entity<PreprocessedShuttleSchema>(entity =>
    {
      entity.HasKey(e => e.Id);
      entity.Property(e => e.DCheckComplete).HasConversion<string>();
      entity.Property(e => e.MoonClearanceComplete).HasConversion<string>();
      entity.Property(e => e.Price).HasPrecision(18, 6);
    });

    modelBuilder.Entity<ModelInputTableSchema>(entity =>
    {
      entity.HasKey(e => e.ShuttleId);
      entity.Property(e => e.DCheckComplete).HasConversion<string>();
      entity.Property(e => e.MoonClearanceComplete).HasConversion<string>();
      entity.Property(e => e.CompanyRating).HasPrecision(18, 6);
      entity.Property(e => e.ReviewScoresRating).HasPrecision(18, 6);
      entity.Property(e => e.Price).HasPrecision(18, 6);
    });

    modelBuilder.Entity<TrainingData>(entity =>
    {
      entity.Property<int>("Id");
      entity.HasKey("Id");
      entity.OwnsOne(
        e => e.Features,
        features =>
        {
          features.Property(f => f.DCheckComplete).HasConversion<string>();
          features.Property(f => f.MoonClearanceComplete).HasConversion<string>();
          features.Property(f => f.CompanyRating).HasPrecision(18, 6);
          features.Property(f => f.ReviewScoresRating).HasPrecision(18, 6);
        }
      );
      entity.Property(e => e.Label).HasPrecision(18, 6);
    });

    modelBuilder.Entity<TestData>(entity =>
    {
      entity.Property<int>("Id");
      entity.HasKey("Id");
      entity.OwnsOne(
        e => e.Features,
        features =>
        {
          features.Property(f => f.DCheckComplete).HasConversion<string>();
          features.Property(f => f.MoonClearanceComplete).HasConversion<string>();
          features.Property(f => f.CompanyRating).HasPrecision(18, 6);
          features.Property(f => f.ReviewScoresRating).HasPrecision(18, 6);
        }
      );
      entity.Property(e => e.Label).HasPrecision(18, 6);
    });

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
      entity
        .Property(e => e.FeatureNames)
        .HasColumnType("text")
        .HasConversion(
          v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
          v =>
            JsonSerializer.Deserialize<string[]>(v, (JsonSerializerOptions?)null)
            ?? Array.Empty<string>()
        );
      entity.Property(e => e.Intercept).IsRequired();
    });

    modelBuilder.Entity<ModelMetrics>(entity =>
    {
      entity.HasKey(e => e.R2Score);
      entity.Property(e => e.R2Score).HasPrecision(18, 6);
      entity.Property(e => e.MeanAbsoluteError).HasPrecision(18, 6);
      entity.Property(e => e.MaxError).HasPrecision(18, 6);
    });

    modelBuilder.Entity<ModelPredictions>(entity =>
    {
      entity.Property<int>("Id");
      entity.HasKey("Id");
    });
  }
}
