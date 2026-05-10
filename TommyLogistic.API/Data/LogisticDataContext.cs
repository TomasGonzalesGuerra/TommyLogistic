using TommyLogistic.Shared.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;

namespace TommyLogistic.API.Data;

public class LogisticDataContext(DbContextOptions<LogisticDataContext> options) : IdentityDbContext<User>(options)
{
    public DbSet<Driver> Drivers { get; set; }
    public DbSet<Company> Companies { get; set; }
    public DbSet<Order> Orders { get; set; }
    public DbSet<OrderEvent> OrderEvents { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Driver>()
            .HasKey(d => d.UserID);

        modelBuilder.Entity<User>()
            .HasOne(u => u.Driver)
            .WithOne(d => d.User)
            .HasForeignKey<Driver>(d => d.UserID);

        modelBuilder.Entity<Order>()
            .HasOne(o => o.Driver)
            .WithMany(d => d.Orders)
            .HasForeignKey(o => o.DriverID)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Order>()
            .HasOne(o => o.Company)
            .WithMany(c => c.Orders)
            .HasForeignKey(o => o.CompanyID)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<OrderEvent>()
            .HasOne(e => e.Order)
            .WithMany(o => o.Events)
            .HasForeignKey(e => e.OrderID)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<OrderEvent>()
            .HasOne(e => e.User)
            .WithMany(o => o.Events)
            .HasForeignKey(e => e.UserID)
            .OnDelete(DeleteBehavior.Restrict);
    }

}
