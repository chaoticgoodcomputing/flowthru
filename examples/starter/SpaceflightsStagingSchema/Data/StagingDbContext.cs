using Microsoft.EntityFrameworkCore;
using SpaceflightsStagingSchema.Data._02_Intermediate.Schemas;

namespace SpaceflightsStagingSchema.Data;

/// <summary>
/// EF Core context for the ephemeral staging database. Three tables — companies,
/// shuttles, reviews — in their preprocessed (typed) form. <strong>No FK
/// constraints</strong>: staging is the unconstrained scratchpad that
/// DataProcessing fills in any order.
/// </summary>
/// <remarks>
/// FK enforcement happens on promotion to production. Keeping staging
/// constraint-free is deliberate — it lets the three preprocess steps run in
/// parallel without an explicit ordering edge in the DAG.
/// </remarks>
public class StagingDbContext : DbContext
{
  public StagingDbContext(DbContextOptions<StagingDbContext> options)
    : base(options) { }

  public DbSet<PreprocessedCompanySchema> Companies => Set<PreprocessedCompanySchema>();
  public DbSet<PreprocessedShuttleSchema> Shuttles => Set<PreprocessedShuttleSchema>();
  public DbSet<PreprocessedReviewSchema> Reviews => Set<PreprocessedReviewSchema>();

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
      // No FK to Companies — staging is unconstrained on purpose.
    });

    modelBuilder.Entity<PreprocessedReviewSchema>(entity =>
    {
      entity.Property<int>("Id"); // shadow PK
      entity.HasKey("Id");
      entity.Property(e => e.ReviewScoresRating).HasPrecision(18, 6);
      // No FK to Shuttles — staging is unconstrained on purpose.
    });
  }
}
