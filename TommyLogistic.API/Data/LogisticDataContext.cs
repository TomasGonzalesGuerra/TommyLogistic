using TommyLogistic.Shared.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;

namespace TommyLogistic.API.Data;

public class LogisticDataContext(DbContextOptions<LogisticDataContext> options) : IdentityDbContext<User>(options)
{
    public DbSet<Driver> Drivers { get; set; }
    public DbSet<Company> Companies { get; set; }
    public DbSet<Order> Orders { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Driver>().HasKey(d => d.UserID);
        modelBuilder.Entity<User>().HasOne(u => u.Driver).WithOne(d => d.User).HasForeignKey<Driver>(d => d.UserID);
    }
}
