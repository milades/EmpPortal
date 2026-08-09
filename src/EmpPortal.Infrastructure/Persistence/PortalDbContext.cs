using EmpPortal.Domain.Access;
using EmpPortal.Domain.Auditing;
using EmpPortal.Domain.Configuration;
using EmpPortal.Domain.Forms;
using EmpPortal.Domain.Hr;
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

    public DbSet<PortalAccessGrant> PortalAccessGrants => Set<PortalAccessGrant>();

    public DbSet<PayslipPeriodSetting> PayslipPeriodSettings => Set<PayslipPeriodSetting>();

    public DbSet<PersonnelProfile> PersonnelProfiles => Set<PersonnelProfile>();

    public DbSet<PersonnelVehicle> PersonnelVehicles => Set<PersonnelVehicle>();

    public DbSet<CharityPledge> CharityPledges => Set<CharityPledge>();

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
            entity.Property(user => user.PersonnelCode).HasMaxLength(64);
            entity.HasIndex(user => user.DirectoryObjectGuid).IsUnique();
            entity.HasIndex(user => user.Sid).IsUnique();
            entity.HasIndex(user => user.PersonnelCode)
                .IsUnique()
                .HasFilter("[PersonnelCode] IS NOT NULL");
        });

        builder.Entity<ApplicationRole>(entity =>
        {
            entity.ToTable("Roles", "identity");
            entity.Property(role => role.Description).HasMaxLength(512);
            entity.Property(role => role.IsSystem).HasDefaultValue(false);
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
        ConfigureAccess(builder);
        ConfigureHr(builder);
    }

    private static void ConfigureAccess(ModelBuilder builder)
    {
        builder.Entity<PortalAccessGrant>(entity =>
        {
            entity.ToTable("PortalAccessGrants", "security");
            entity.HasKey(grant => grant.Id);
            entity.Property(grant => grant.ResourceKey).HasMaxLength(120).IsRequired();
            entity.Property(grant => grant.SubjectType).HasConversion<int>();
            entity.Property(grant => grant.SubjectKey).HasMaxLength(320).IsRequired();
            entity.HasIndex(grant => new { grant.ResourceKey, grant.SubjectType, grant.SubjectKey }).IsUnique();
            entity.HasIndex(grant => grant.ResourceKey);
            entity.HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(grant => grant.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void ConfigureHr(ModelBuilder builder)
    {
        builder.Entity<PayslipPeriodSetting>(entity =>
        {
            entity.ToTable("PayslipPeriodSettings", "hr");
            entity.HasKey(setting => setting.Id);
            entity.HasIndex(setting => new { setting.PersianYear, setting.PersianMonth }).IsUnique();
            entity.HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(setting => setting.UpdatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<PersonnelProfile>(entity =>
        {
            entity.ToTable("PersonnelProfiles", "hr");
            entity.HasKey(profile => profile.UserId);
            entity.Property(profile => profile.InternalPhone).HasMaxLength(32);
            entity.HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(profile => profile.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(profile => profile.UpdatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<PersonnelVehicle>(entity =>
        {
            entity.ToTable("PersonnelVehicles", "hr");
            entity.HasKey(vehicle => vehicle.Id);
            entity.Property(vehicle => vehicle.PlateNumber).HasMaxLength(32).IsRequired();
            entity.Property(vehicle => vehicle.VehicleType).HasMaxLength(80).IsRequired();
            entity.Property(vehicle => vehicle.Trim).HasMaxLength(80);
            entity.Property(vehicle => vehicle.Model).HasMaxLength(80);
            entity.Property(vehicle => vehicle.Color).HasMaxLength(40);
            entity.Property(vehicle => vehicle.Notes).HasMaxLength(500);
            entity.HasIndex(vehicle => vehicle.UserId);
            entity.HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(vehicle => vehicle.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(vehicle => vehicle.UpdatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<CharityPledge>(entity =>
        {
            entity.ToTable("CharityPledges", "hr");
            entity.HasKey(pledge => pledge.Id);
            entity.Property(pledge => pledge.Amount).HasPrecision(18, 0);
            entity.Property(pledge => pledge.Mode).HasConversion<int>();
            entity.Property(pledge => pledge.Note).HasMaxLength(500);
            entity.HasIndex(pledge => new { pledge.UserId, pledge.CreatedAtUtc });
            entity.HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(pledge => pledge.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(pledge => pledge.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(pledge => pledge.UpdatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });
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
