using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using TimeSeriesForecaster.Domain.Entities;

namespace TimeSeriesForecaster.Infrastructure.Persistence;

public class AppDbContext : IdentityDbContext<AppUser, IdentityRole<int>, int>
{
    public virtual DbSet<Project> Projects { get; set; }
    public virtual DbSet<Dataset> Datasets { get; set; }
    public virtual DbSet<DataPoint> DataPoints { get; set; }
    public virtual DbSet<Model> Models { get; set; }
    public virtual DbSet<Prediction> Predictions { get; set; }
    public virtual DbSet<ModelMetric> ModelMetrics { get; set; }
    public DbSet<Notification> Notifications { get; set; }
    public DbSet<DashboardDismissal> DashboardDismissals { get; set; }
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<DashboardDismissal>()
            .HasIndex(d => new { d.UserId, d.EntityType, d.EntityId })
            .IsUnique();

        modelBuilder.Entity<DataPoint>()
            .HasIndex(dp => new { dp.DatasetId, dp.Timestamp });
    }
}
