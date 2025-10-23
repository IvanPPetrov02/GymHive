using GymService.BLL.Entities;
using Microsoft.EntityFrameworkCore;

namespace GymService.DAL.DbContexts;

public class GymDbContext : DbContext
{
    public GymDbContext(DbContextOptions<GymDbContext> options) : base(options)
    {
    }

    public DbSet<Gym> Gyms { get; set; }
    public DbSet<Membership> Memberships { get; set; }
    public DbSet<GymGroup> GymGroups { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure Gym entity
        modelBuilder.Entity<Gym>(entity =>
        {
            entity.HasKey(g => g.Id);
            entity.Property(g => g.Name).IsRequired().HasMaxLength(200);
            entity.Property(g => g.Description).HasMaxLength(1000);
            entity.Property(g => g.Address).HasMaxLength(300);
            entity.Property(g => g.City).HasMaxLength(100);
            entity.Property(g => g.Country).HasMaxLength(100);
            entity.Property(g => g.Phone).HasMaxLength(20);
            entity.Property(g => g.Email).HasMaxLength(200);

            entity.HasMany(g => g.Memberships)
                .WithOne(m => m.Gym)
                .HasForeignKey(m => m.GymId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(g => g.GymGroups)
                .WithOne(gg => gg.Gym)
                .HasForeignKey(gg => gg.GymId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure Membership entity
        modelBuilder.Entity<Membership>(entity =>
        {
            entity.HasKey(m => m.Id);
            entity.Property(m => m.MembershipType).IsRequired().HasMaxLength(50);
            entity.Property(m => m.Price).HasColumnType("decimal(18,2)");
        });

        // Configure GymGroup entity
        modelBuilder.Entity<GymGroup>(entity =>
        {
            entity.HasKey(gg => gg.Id);
            entity.Property(gg => gg.Name).IsRequired().HasMaxLength(200);
            entity.Property(gg => gg.Description).HasMaxLength(1000);
            entity.Property(gg => gg.Schedule).HasMaxLength(500);
        });
    }
}
