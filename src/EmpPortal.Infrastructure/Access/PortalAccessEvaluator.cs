using EmpPortal.Application.Authorization;
using EmpPortal.Domain.Access;
using EmpPortal.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EmpPortal.Infrastructure.Access;

public sealed class PortalAccessEvaluator(IDbContextFactory<PortalDbContext> dbContextFactory)
    : IPortalAccessEvaluator
{
    private static readonly (string Role, string Resource)[] ExtraRoleGrants =
    [
        (PortalRoles.FormAdministrator, PortalResources.FormsAdmin),
        (PortalRoles.FormDesigner, PortalResources.FormsAdmin),
        (PortalRoles.FormPublisher, PortalResources.FormsAdmin),
        (PortalRoles.SubmissionViewer, PortalResources.FormsAdmin),
        (PortalRoles.ReportExporter, PortalResources.FormsAdmin)
    ];

    public async Task EnsureDefaultsAsync(CancellationToken cancellationToken = default)
    {
        await using PortalDbContext dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        Guid? seedUserId = await dbContext.Users.AsNoTracking()
            .OrderBy(user => user.UserName)
            .Select(user => (Guid?)user.Id)
            .FirstOrDefaultAsync(cancellationToken);
        if (seedUserId is null)
        {
            return;
        }

        bool changed = false;
        foreach (string resourceKey in PortalResources.DefaultEmployeeResourceKeys)
        {
            changed |= await EnsureGrantAsync(
                dbContext,
                resourceKey,
                PortalAccessSubjectType.Role,
                PortalRoles.Employee,
                seedUserId.Value,
                cancellationToken);
        }

        foreach ((string role, string resource) in ExtraRoleGrants)
        {
            changed |= await EnsureGrantAsync(
                dbContext,
                resource,
                PortalAccessSubjectType.Role,
                role,
                seedUserId.Value,
                cancellationToken);
        }

        if (changed)
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<bool> HasAccessAsync(
        PortalActor actor,
        string resourceKey,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(actor);
        ArgumentException.ThrowIfNullOrWhiteSpace(resourceKey);

        if (actor.Roles.Contains(PortalRoles.SystemAdministrator))
        {
            return true;
        }

        if (!PortalResources.IsKnown(resourceKey))
        {
            return false;
        }

        await using PortalDbContext dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        string userKey = actor.UserId.ToString("D");
        string[] roleKeys = actor.Roles.ToArray();
        return await dbContext.PortalAccessGrants.AsNoTracking().AnyAsync(
            grant => grant.ResourceKey == resourceKey &&
                (grant.SubjectType == PortalAccessSubjectType.Everyone ||
                 grant.SubjectType == PortalAccessSubjectType.User && grant.SubjectKey == userKey ||
                 grant.SubjectType == PortalAccessSubjectType.Role && roleKeys.Contains(grant.SubjectKey)),
            cancellationToken);
    }

    public async Task<IReadOnlySet<string>> GetGrantedResourceKeysAsync(
        PortalActor actor,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(actor);

        if (actor.Roles.Contains(PortalRoles.SystemAdministrator))
        {
            return PortalResources.All.Select(resource => resource.Key).ToHashSet(StringComparer.OrdinalIgnoreCase);
        }

        await using PortalDbContext dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        string userKey = actor.UserId.ToString("D");
        string[] roleKeys = actor.Roles.ToArray();
        string[] keys = await dbContext.PortalAccessGrants.AsNoTracking()
            .Where(grant =>
                grant.SubjectType == PortalAccessSubjectType.Everyone ||
                grant.SubjectType == PortalAccessSubjectType.User && grant.SubjectKey == userKey ||
                grant.SubjectType == PortalAccessSubjectType.Role && roleKeys.Contains(grant.SubjectKey))
            .Select(grant => grant.ResourceKey)
            .Distinct()
            .ToArrayAsync(cancellationToken);

        return keys.Where(PortalResources.IsKnown).ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    private static async Task<bool> EnsureGrantAsync(
        PortalDbContext dbContext,
        string resourceKey,
        PortalAccessSubjectType subjectType,
        string subjectKey,
        Guid seedUserId,
        CancellationToken cancellationToken)
    {
        bool exists = await dbContext.PortalAccessGrants.AnyAsync(
            grant => grant.ResourceKey == resourceKey &&
                grant.SubjectType == subjectType &&
                grant.SubjectKey == subjectKey,
            cancellationToken);
        if (exists)
        {
            return false;
        }

        dbContext.PortalAccessGrants.Add(PortalAccessGrant.Create(
            resourceKey,
            subjectType,
            subjectKey,
            seedUserId,
            DateTimeOffset.UtcNow));
        return true;
    }
}
