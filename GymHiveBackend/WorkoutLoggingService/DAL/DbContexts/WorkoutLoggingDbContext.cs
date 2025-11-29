using Microsoft.EntityFrameworkCore;
using WorkoutLoggingService.DAL.Entities;

namespace WorkoutLoggingService.DAL.DbContexts;

public class WorkoutLoggingDbContext : DbContext
{
    public WorkoutLoggingDbContext(DbContextOptions<WorkoutLoggingDbContext> options) : base(options) { }

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
            entity.Property(w => w.CheckInTime).IsRequired();
            entity.Property(w => w.CreatedAt).IsRequired();
            
            // Index for querying user's workouts
            entity.HasIndex(w => new { w.UserId, w.CreatedAt });
            // Index for checking active check-ins
            entity.HasIndex(w => new { w.UserId, w.CheckOutTime });
        });
    }
}
