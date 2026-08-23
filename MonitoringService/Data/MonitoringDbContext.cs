using MonitoringService.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace MonitoringService.Data;

public class MonitoringDbContext(DbContextOptions<MonitoringDbContext> options) : DbContext(options)
{
    public DbSet<Company> Companies => Set<Company>();
    public DbSet<CompanyUser> CompanyUsers => Set<CompanyUser>();
    public DbSet<Device> Devices => Set<Device>();
    public DbSet<TemperatureReading> TemperatureReadings => Set<TemperatureReading>();
    public DbSet<Alert> Alerts => Set<Alert>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Company>(entity =>
        {
            entity.HasIndex(e => e.Name);
        });

        modelBuilder.Entity<CompanyUser>(entity =>
        {
            entity.HasIndex(e => new { e.CompanyId, e.UserId }).IsUnique();
            entity.HasOne(e => e.Company)
                .WithMany(c => c.Members)
                .HasForeignKey(e => e.CompanyId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Device>(entity =>
        {
            entity.HasIndex(e => e.DeviceKey).IsUnique();
            entity.HasIndex(e => new { e.CompanyId, e.Name });
            entity.HasOne(e => e.Company)
                .WithMany(c => c.Devices)
                .HasForeignKey(e => e.CompanyId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<TemperatureReading>(entity =>
        {
            entity.HasIndex(e => new { e.CompanyId, e.MeasuredAtUtc });
            entity.HasIndex(e => new { e.DeviceId, e.MeasuredAtUtc });
            entity.HasOne(e => e.Device)
                .WithMany(d => d.Readings)
                .HasForeignKey(e => e.DeviceId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Alert>(entity =>
        {
            entity.HasIndex(e => new { e.CompanyId, e.IsActive });
            entity.HasOne(e => e.Device)
                .WithMany(d => d.Alerts)
                .HasForeignKey(e => e.DeviceId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
