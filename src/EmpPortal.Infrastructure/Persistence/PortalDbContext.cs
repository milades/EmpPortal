using EmpPortal.Domain.Auditing;
using EmpPortal.Domain.Configuration;
using EmpPortal.Domain.Sessions;
using EmpPortal.Infrastructure.Persistence.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace EmpPortal.Infrastructure.Persistence;

public sealed class PortalDbContext(DbContextOptions<PortalDbContext> options)
    : IdentityDbContext<ApplicationUser, ApplicationRole, Guid>(options)
{
    public DbSet<AuditEvent> AuditEvents => Set<AuditEvent>();

    public DbSet<ApplicationSession> ApplicationSessions => Set<ApplicationSession>();

    public DbSet<RuntimeSetting> RuntimeSettings => Set<RuntimeSetting>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        base.OnModelCreating(builder);

        builder.HasDefaultSchema("portal");

        builder.Entity<ApplicationUser>(entity =>
        {
            entity.ToTable("Users", "identity");
            entity.Property(user => user.Sid).HasMaxLength(256).IsRequired();
            entity.Property(user => user.DisplayName).HasMaxLength(256).IsRequired();
            entity.HasIndex(user => user.DirectoryObjectGuid).IsUnique();
            entity.HasIndex(user => user.Sid).IsUnique();
        });

        builder.Entity<ApplicationRole>(entity =>
        {
            entity.ToTable("Roles", "identity");
            entity.Property(role => role.Description).HasMaxLength(512);
        });

        builder.Entity<IdentityUserClaim<Guid>>()
            .ToTable("UserClaims", "identity");
        builder.Entity<IdentityUserLogin<Guid>>()
            .ToTable("UserLogins", "identity");
        builder.Entity<IdentityUserToken<Guid>>()
            .ToTable("UserTokens", "identity");
        builder.Entity<IdentityRoleClaim<Guid>>()
            .ToTable("RoleClaims", "identity");
        builder.Entity<IdentityUserRole<Guid>>()
            .ToTable("UserRoles", "identity");

        builder.Entity<ApplicationSession>(entity =>
        {
            entity.ToTable("ApplicationSessions", "security");
            entity.HasKey(session => session.Id);
            entity.Property(session => session.RevocationReason).HasMaxLength(512);
            entity.HasIndex(session => new { session.UserId, session.RevokedAtUtc });
            entity.HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(session => session.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<RuntimeSetting>(entity =>
        {
            entity.ToTable("RuntimeSettings", "portal");
            entity.HasKey(setting => setting.Key);
            entity.Property(setting => setting.Key).HasMaxLength(200);
            entity.Property(setting => setting.Value).HasMaxLength(2000);
            entity.Property(setting => setting.RowVersion).IsRowVersion();
            entity.HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(setting => setting.UpdatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<AuditEvent>(entity =>
        {
            entity.ToTable("AuditEvents", "audit");
            entity.HasKey(auditEvent => auditEvent.Id);
            entity.Property(auditEvent => auditEvent.EventType).HasMaxLength(120);
            entity.Property(auditEvent => auditEvent.Outcome).HasMaxLength(40);
            entity.Property(auditEvent => auditEvent.ActorUpn).HasMaxLength(320);
            entity.Property(auditEvent => auditEvent.Subject).HasMaxLength(500);
            entity.Property(auditEvent => auditEvent.CorrelationId).HasMaxLength(100);
            entity.Property(auditEvent => auditEvent.IpAddress).HasMaxLength(64);
            entity.Property(auditEvent => auditEvent.DetailsJson).HasMaxLength(4000);
            entity.HasIndex(auditEvent => auditEvent.OccurredAtUtc);
            entity.HasIndex(auditEvent => new { auditEvent.EventType, auditEvent.Outcome });
        });
    }
}
