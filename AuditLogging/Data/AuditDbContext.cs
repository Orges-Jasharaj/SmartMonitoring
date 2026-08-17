using AuditLogging.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace AuditLogging.Data;

public class AuditDbContext(DbContextOptions<AuditDbContext> options) : DbContext(options)
{
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.ToTable("AuditLogs");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.ServiceName).HasMaxLength(128).IsRequired();
            entity.Property(x => x.EventType).HasMaxLength(128).IsRequired();
            entity.Property(x => x.Outcome).HasMaxLength(32).IsRequired();
            entity.Property(x => x.ActorUserId).HasMaxLength(450);
            entity.Property(x => x.ActorUserName).HasMaxLength(256);
            entity.Property(x => x.TargetEntityType).HasMaxLength(128);
            entity.Property(x => x.TargetEntityId).HasMaxLength(450);
            entity.Property(x => x.TargetUserName).HasMaxLength(256);
            entity.Property(x => x.Detail).HasMaxLength(2000);
            entity.Property(x => x.CorrelationId).HasMaxLength(128);
            entity.Property(x => x.IpAddress).HasMaxLength(64);

            entity.HasIndex(x => x.OccurredAtUtc);
            entity.HasIndex(x => x.ServiceName);
            entity.HasIndex(x => x.EventType);
            entity.HasIndex(x => x.ActorUserId);
            entity.HasIndex(x => x.CorrelationId);
        });
    }
}
