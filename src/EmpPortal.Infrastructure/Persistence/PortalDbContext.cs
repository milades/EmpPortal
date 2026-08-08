using EmpPortal.Domain.Auditing;
using EmpPortal.Domain.Configuration;
using EmpPortal.Domain.Forms;
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

    public DbSet<FormDefinition> Forms => Set<FormDefinition>();

    public DbSet<FormVersion> FormVersions => Set<FormVersion>();

    public DbSet<FormAccessRule> FormAccessRules => Set<FormAccessRule>();

    public DbSet<FormSubmission> FormSubmissions => Set<FormSubmission>();

    public DbSet<FormAnswerIndex> FormAnswerIndexes => Set<FormAnswerIndex>();

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

        ConfigureForms(builder);
    }

    private static void ConfigureForms(ModelBuilder builder)
    {
        builder.Entity<FormDefinition>(entity =>
        {
            entity.ToTable("Forms", "forms");
            entity.HasKey(form => form.Id);
            entity.Property(form => form.Slug).HasMaxLength(120).IsRequired();
            entity.Property(form => form.Title).HasMaxLength(200).IsRequired();
            entity.Property(form => form.Description).HasMaxLength(2000);
            entity.Property(form => form.Status).HasConversion<int>();
            entity.Property(form => form.RowVersion).IsRowVersion();
            entity.HasIndex(form => form.Slug).IsUnique();
            entity.HasIndex(form => new { form.Status, form.OpensAtUtc, form.ClosesAtUtc });
            entity.HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(form => form.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(form => form.UpdatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<FormVersion>(entity =>
        {
            entity.ToTable("FormVersions", "forms", table =>
                table.HasCheckConstraint(
                    "CK_FormVersions_DefinitionJson_IsJson",
                    "ISJSON([DefinitionJson]) = 1"));
            entity.HasKey(version => version.Id);
            entity.Property(version => version.Status).HasConversion<int>();
            entity.Property(version => version.DefinitionJson).HasColumnType("nvarchar(max)").IsRequired();
            entity.Property(version => version.SchemaHash).HasColumnType("char(64)").IsRequired();
            entity.Property(version => version.RowVersion).IsRowVersion();
            entity.HasIndex(version => new { version.FormId, version.VersionNumber }).IsUnique();
            entity.HasIndex(version => new { version.FormId, version.Status });
            entity.HasOne<FormDefinition>()
                .WithMany()
                .HasForeignKey(version => version.FormId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(version => version.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(version => version.UpdatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<FormDefinition>()
            .HasOne<FormVersion>()
            .WithMany()
            .HasForeignKey(form => form.CurrentPublishedVersionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<FormAccessRule>(entity =>
        {
            entity.ToTable("FormAccessRules", "forms");
            entity.HasKey(rule => rule.Id);
            entity.Property(rule => rule.SubjectType).HasConversion<int>();
            entity.Property(rule => rule.SubjectKey).HasMaxLength(320).IsRequired();
            entity.Property(rule => rule.Rights).HasConversion<int>();
            entity.HasIndex(rule => new { rule.FormId, rule.SubjectType, rule.SubjectKey }).IsUnique();
            entity.HasOne<FormDefinition>()
                .WithMany()
                .HasForeignKey(rule => rule.FormId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(rule => rule.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<FormSubmission>(entity =>
        {
            entity.ToTable("FormSubmissions", "forms", table =>
                table.HasCheckConstraint(
                    "CK_FormSubmissions_DataJson_IsJson",
                    "ISJSON([DataJson]) = 1"));
            entity.HasKey(submission => submission.Id);
            entity.Property(submission => submission.Status).HasConversion<int>();
            entity.Property(submission => submission.DataJson).HasColumnType("nvarchar(max)").IsRequired();
            entity.Property(submission => submission.TrackingCode).HasMaxLength(40).IsRequired();
            entity.Property(submission => submission.RowVersion).IsRowVersion();
            entity.HasIndex(submission => submission.TrackingCode).IsUnique();
            entity.HasIndex(submission => new
            {
                submission.FormId,
                submission.Status,
                submission.SubmittedAtUtc
            });
            entity.HasIndex(submission => new
            {
                submission.SubmittedByUserId,
                submission.FormId,
                submission.Status
            });
            entity.HasIndex(submission => new
            {
                submission.SubmittedByUserId,
                submission.FormId
            })
                .HasFilter("[Status] = 0")
                .IsUnique();
            entity.HasOne<FormDefinition>()
                .WithMany()
                .HasForeignKey(submission => submission.FormId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<FormVersion>()
                .WithMany()
                .HasForeignKey(submission => submission.FormVersionId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(submission => submission.SubmittedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<FormAnswerIndex>(entity =>
        {
            entity.ToTable("FormAnswerIndexes", "forms");
            entity.HasKey(answer => answer.Id);
            entity.Property(answer => answer.FieldName).HasMaxLength(100).IsRequired();
            entity.Property(answer => answer.FieldType).HasMaxLength(40).IsRequired();
            entity.Property(answer => answer.StringValue).HasMaxLength(700);
            entity.Property(answer => answer.DecimalValue).HasPrecision(38, 10);
            entity.HasIndex(answer => new { answer.SubmissionId, answer.FieldId, answer.Sequence }).IsUnique();
            entity.HasIndex(answer => new { answer.FieldId, answer.StringValue });
            entity.HasIndex(answer => new { answer.FieldId, answer.DecimalValue });
            entity.HasIndex(answer => new { answer.FieldId, answer.DateTimeValue });
            entity.HasOne<FormSubmission>()
                .WithMany()
                .HasForeignKey(answer => answer.SubmissionId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
