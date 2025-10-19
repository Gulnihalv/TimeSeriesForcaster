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
    public virtual DbSet<BackgroundJob> BackgroundJobs { get; set; }
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }
}
