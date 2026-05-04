using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SpaceflightsStagingSchema.Data._02_Intermediate.Schemas;
using SpaceflightsStagingSchema.Data._05_ModelInput.Schemas;
using SpaceflightsStagingSchema.Data._06_Models.Schemas;
using SpaceflightsStagingSchema.Data._07_ModelOutput.Schemas;

namespace SpaceflightsStagingSchema.Data;

/// <summary>
/// EF Core context for the persistent production database. Three normalized
/// source tables (Companies, Shuttles, Reviews) with FK constraints, plus the
/// science-side tables that DataScience writes (splits, model, metrics,
/// predictions).
/// </summary>
/// <remarks>
/// <para>
/// The production model input table is <strong>not</strong> a DbSet here — it
/// is a <see cref="Flowthru.Extensions.EFCore.Data.DbQuery{T}"/> view composed
/// at step time via <see cref="Flowthru.Extensions.EFCore.Data.DbQuery{T}.Project{TResult}"/>
/// over the three normalized tables.
/// </para>
/// <para>
/// FK enforcement is the <em>integrity contract</em> that distinguishes
/// production from staging. Promoting a row whose FK target is missing fails
/// at insert time — fail-fast moved from C# code to the database engine.
/// </para>
/// </remarks>
public class ProductionDbContext : DbContext
{
  public ProductionDbContext(DbContextOptions<ProductionDbContext> options)
    : base(options) { }

  // _02_Intermediate tables, promoted from staging with FK enforcement
  public DbSet<PreprocessedCompanySchema> Companies => Set<PreprocessedCompanySchema>();
  public DbSet<PreprocessedShuttleSchema> Shuttles => Set<PreprocessedShuttleSchema>();
  public DbSet<PreprocessedReviewSchema> Reviews => Set<PreprocessedReviewSchema>();

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

    modelBuilder.Entity<PreprocessedCompanySchema>(entity =>
    {
      entity.HasKey(e => e.Id);
      entity.Property(e => e.CompanyRating).HasPrecision(18, 6);
    });

    modelBuilder.Entity<PreprocessedShuttleSchema>(entity =>
    {
      entity.HasKey(e => e.Id);
      entity.Property(e => e.Price).HasPrecision(18, 6);

      // FK: a shuttle's CompanyId must reference a known Company.
      entity
        .HasOne<PreprocessedCompanySchema>()
        .WithMany()
        .HasForeignKey(s => s.CompanyId)
        .OnDelete(DeleteBehavior.Restrict);
    });

    modelBuilder.Entity<PreprocessedReviewSchema>(entity =>
    {
      entity.Property<int>("Id"); // shadow PK — Reviews has no natural primary key
      entity.HasKey("Id");
      entity.Property(e => e.ReviewScoresRating).HasPrecision(18, 6);

      // FK: a review's ShuttleId must reference a known Shuttle.
      entity
        .HasOne<PreprocessedShuttleSchema>()
        .WithMany()
        .HasForeignKey(r => r.ShuttleId)
        .OnDelete(DeleteBehavior.Cascade);
    });

    modelBuilder.Entity<TrainingData>(entity =>
    {
      entity.Property<int>("Id");
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

    modelBuilder.Entity<TestData>(entity =>
    {
      entity.Property<int>("Id");
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
