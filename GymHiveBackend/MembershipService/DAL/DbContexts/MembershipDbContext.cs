using MembershipService.BLL.Entities;
using Microsoft.EntityFrameworkCore;

namespace MembershipService.DAL.DbContexts;

public class MembershipDbContext : DbContext
{
    public MembershipDbContext(DbContextOptions<MembershipDbContext> options) : base(options)
    {
    }

    public DbSet<Membership> Memberships { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Membership>(entity =>
        {
            entity.ToTable("Memberships");
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.UserId).IsRequired();
            entity.Property(e => e.GymId).IsRequired();
            entity.Property(e => e.MembershipType).HasMaxLength(50).IsRequired();
            entity.Property(e => e.Price).HasColumnType("decimal(10,2)");
            entity.Property(e => e.StartDate).IsRequired();
            entity.Property(e => e.EndDate).IsRequired();
            entity.Property(e => e.IsActive).IsRequired();
            entity.Property(e => e.CreatedAt).IsRequired();

            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.GymId);
        });
    }
}
