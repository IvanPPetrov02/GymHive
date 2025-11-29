using Microsoft.EntityFrameworkCore;
using NotificationsService.DAL.Entities;

namespace NotificationsService.DAL.DbContexts;

public class NotificationsDbContext : DbContext
{
    public NotificationsDbContext(DbContextOptions<NotificationsDbContext> options) : base(options) { }

    public DbSet<Notification> Notifications { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Notification>(entity =>
        {
            entity.ToTable("Notifications");
            entity.HasKey(n => n.Id);
            entity.Property(n => n.Type).IsRequired().HasMaxLength(50);
            entity.Property(n => n.Title).IsRequired().HasMaxLength(200);
            entity.Property(n => n.Message).IsRequired().HasMaxLength(1000);
            entity.Property(n => n.RelatedEntityId).HasMaxLength(100);
            entity.Property(n => n.RelatedEntityType).HasMaxLength(50);
            entity.Property(n => n.IsRead).HasDefaultValue(false);
            entity.HasIndex(n => new { n.UserId, n.IsRead, n.CreatedAt });
        });
    }
}
