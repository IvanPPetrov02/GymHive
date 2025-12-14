using Microsoft.EntityFrameworkCore;
using WorkoutLoggingService.BLL.Entities;

namespace WorkoutLoggingService.DAL.DbContexts;

public class WorkoutLoggingDbContext : DbContext
{
    public WorkoutLoggingDbContext(DbContextOptions<WorkoutLoggingDbContext> options) : base(options)
    {
    }

    public DbSet<WorkoutLog> WorkoutLogs { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<WorkoutLog>(entity =>
        {
            entity.ToTable("WorkoutLogs");
            entity.HasKey(w => w.Id);
            entity.Property(w => w.UserId).IsRequired();
            entity.Property(w => w.GymId).IsRequired();
            entity.Property(w => w.VisitDate).IsRequired();
            
            // Index for querying user's gym visits by date
            entity.HasIndex(w => new { w.UserId, w.VisitDate });
            
            // Unique constraint: one visit per gym per day
            entity.HasIndex(w => new { w.UserId, w.GymId, w.VisitDate }).IsUnique();
        });
    }
}
