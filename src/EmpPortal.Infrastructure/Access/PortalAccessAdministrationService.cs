using System.Text.Json;
using System.Text.RegularExpressions;
using EmpPortal.Application.Authorization;
using EmpPortal.Domain.Access;
using EmpPortal.Domain.Auditing;
using EmpPortal.Infrastructure.Persistence;
using EmpPortal.Infrastructure.Persistence.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace EmpPortal.Infrastructure.Access;

public sealed partial class PortalAccessAdministrationService(
    IDbContextFactory<PortalDbContext> dbContextFactory,
    UserManager<ApplicationUser> userManager,
    RoleManager<ApplicationRole> roleManager,
    IPortalAccessEvaluator accessEvaluator) : IPortalAccessAdministrationService
{
    public Task EnsureDefaultsAsync(CancellationToken cancellationToken = default) =>
        accessEvaluator.EnsureDefaultsAsync(cancellationToken);

    public async Task<IReadOnlyList<PortalUserAccessSummary>> SearchUsersAsync(
        PortalActor actor,
        string? search,
        int take = 40,
        CancellationToken cancellationToken = default)
    {
        await EnsureCanManageAsync(actor, cancellationToken);
        take = Math.Clamp(take, 1, 100);

        await using PortalDbContext dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        IQueryable<ApplicationUser> query = dbContext.Users.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(search))
        {
            string term = search.Trim();
            query = query.Where(user =>
                user.UserName!.Contains(term) ||
                user.DisplayName.Contains(term) ||
                (user.PersonnelCode != null && user.PersonnelCode.Contains(term)));
        }

        ApplicationUser[] users = await query
            .OrderBy(user => user.DisplayName)
            .ThenBy(user => user.UserName)
            .Take(take)
            .ToArrayAsync(cancellationToken);

        List<PortalUserAccessSummary> result = [];
        foreach (ApplicationUser user in users)
        {
            result.Add(await MapUserAsync(user, cancellationToken));
        }

        return result;
    }

    public async Task<PortalUserAccessSummary?> GetUserAsync(
        PortalActor actor,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        await EnsureCanManageAsync(actor, cancellationToken);
        ApplicationUser? user = await userManager.FindByIdAsync(userId.ToString("D"));
        return user is null ? null : await MapUserAsync(user, cancellationToken);
    }

    public async Task SetUserRolesAsync(
        PortalActor actor,
        Guid userId,
        IReadOnlyCollection<string> roleNames,
        CancellationToken cancellationToken = default)
    {
        await EnsureCanManageAsync(actor, cancellationToken);
        ArgumentNullException.ThrowIfNull(roleNames);

        ApplicationUser user = await userManager.FindByIdAsync(userId.ToString("D"))
            ?? throw new KeyNotFoundException("کاربر یافت نشد.");

        HashSet<string> requested = roleNames
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Select(name => name.Trim())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        requested.Add(PortalRoles.Employee);

        foreach (string roleName in requested)
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                throw new InvalidOperationException($"نقش «{roleName}» در سامانه تعریف نشده است.");
            }
        }

        IList<string> currentRoles = await userManager.GetRolesAsync(user);
        bool removingAdmin = currentRoles.Any(role =>
                string.Equals(role, PortalRoles.SystemAdministrator, StringComparison.OrdinalIgnoreCase)) &&
            !requested.Contains(PortalRoles.SystemAdministrator);
        if (removingAdmin)
        {
            int remainingAdmins = await CountSystemAdministratorsAsync(excludeUserId: user.Id, cancellationToken);
            if (remainingAdmins == 0)
            {
                throw new InvalidOperationException("حداقل یک مدیر کل سامانه باید باقی بماند.");
            }
        }

        string[] toRemove = currentRoles
            .Where(role => !requested.Contains(role))
            .ToArray();
        string[] toAdd = requested
            .Where(role => !currentRoles.Any(current =>
                string.Equals(current, role, StringComparison.OrdinalIgnoreCase)))
            .ToArray();

        if (toRemove.Length > 0)
        {
            IdentityResult removeResult = await userManager.RemoveFromRolesAsync(user, toRemove);
            if (!removeResult.Succeeded)
            {
                throw new InvalidOperationException(string.Join("؛ ", removeResult.Errors.Select(error => error.Description)));
            }
        }

        if (toAdd.Length > 0)
        {
            IdentityResult addResult = await userManager.AddToRolesAsync(user, toAdd);
            if (!addResult.Succeeded)
            {
                throw new InvalidOperationException(string.Join("؛ ", addResult.Errors.Select(error => error.Description)));
            }
        }

        user.AuthorizationVersion++;
        IdentityResult updateResult = await userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
        {
            throw new InvalidOperationException(string.Join("؛ ", updateResult.Errors.Select(error => error.Description)));
        }

        await AddAuditAsync(
            actor,
            "UserRolesUpdated",
            user.Id.ToString("D"),
            new { user.UserName, Added = toAdd, Removed = toRemove },
            cancellationToken);
    }

    public async Task SetPersonnelCodeAsync(
        PortalActor actor,
        Guid userId,
        string? personnelCode,
        CancellationToken cancellationToken = default)
    {
        await EnsureCanManageAsync(actor, cancellationToken);

        ApplicationUser user = await userManager.FindByIdAsync(userId.ToString("D"))
            ?? throw new KeyNotFoundException("کاربر یافت نشد.");

        string? normalized = string.IsNullOrWhiteSpace(personnelCode) ? null : personnelCode.Trim();
        if (normalized is { Length: > 64 })
        {
            throw new InvalidOperationException("کد پرسنلی نباید بیشتر از ۶۴ نویسه باشد.");
        }

        if (normalized is not null)
        {
            ApplicationUser? conflict = await userManager.Users
                .FirstOrDefaultAsync(
                    candidate => candidate.PersonnelCode == normalized && candidate.Id != user.Id,
                    cancellationToken);
            if (conflict is not null)
            {
                throw new InvalidOperationException("این کد پرسنلی قبلاً به کاربر دیگری اختصاص داده شده است.");
            }
        }

        user.PersonnelCode = normalized;
        user.AuthorizationVersion++;
        IdentityResult updateResult = await userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
        {
            throw new InvalidOperationException(string.Join("؛ ", updateResult.Errors.Select(error => error.Description)));
        }

        await AddAuditAsync(
            actor,
            "PersonnelCodeUpdated",
            user.Id.ToString("D"),
            new { user.UserName, PersonnelCode = normalized },
            cancellationToken);
    }

    public async Task<IReadOnlyList<PortalAccessGrantData>> GetGrantsAsync(
        PortalActor actor,
        CancellationToken cancellationToken = default)
    {
        await EnsureCanManageAsync(actor, cancellationToken);
        await using PortalDbContext dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        PortalAccessGrant[] grants = await dbContext.PortalAccessGrants.AsNoTracking()
            .OrderBy(grant => grant.ResourceKey)
            .ThenBy(grant => grant.SubjectType)
            .ThenBy(grant => grant.SubjectKey)
            .ToArrayAsync(cancellationToken);

        Dictionary<Guid, string> userNames = await LoadUserDisplayNamesAsync(
            dbContext,
            grants.Where(grant => grant.SubjectType == PortalAccessSubjectType.User)
                .Select(grant => grant.SubjectKey)
                .ToArray(),
            cancellationToken);
        Dictionary<string, string> roleTitles = await LoadRoleTitlesAsync(dbContext, cancellationToken);

        return grants.Select(grant => new PortalAccessGrantData(
            grant.Id,
            grant.ResourceKey,
            PortalResources.Find(grant.ResourceKey)?.Title ?? grant.ResourceKey,
            grant.SubjectType,
            grant.SubjectKey,
            ResolveSubjectDisplayName(grant, userNames, roleTitles),
            grant.CreatedAtUtc)).ToArray();
    }

    public async Task AddGrantAsync(
        PortalActor actor,
        string resourceKey,
        PortalAccessSubjectType subjectType,
        string subjectKey,
        CancellationToken cancellationToken = default)
    {
        await EnsureCanManageAsync(actor, cancellationToken);
        if (!PortalResources.IsKnown(resourceKey))
        {
            throw new InvalidOperationException("منبع دسترسی نامعتبر است.");
        }

        await using PortalDbContext dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        PortalAccessGrant grant = PortalAccessGrant.Create(
            resourceKey,
            subjectType,
            subjectKey,
            actor.UserId,
            DateTimeOffset.UtcNow);

        bool exists = await dbContext.PortalAccessGrants.AnyAsync(
            item => item.ResourceKey == grant.ResourceKey &&
                item.SubjectType == grant.SubjectType &&
                item.SubjectKey == grant.SubjectKey,
            cancellationToken);
        if (exists)
        {
            throw new InvalidOperationException("این دسترسی قبلاً ثبت شده است.");
        }

        dbContext.PortalAccessGrants.Add(grant);
        await BumpAuthorizationVersionsAsync(dbContext, grant, cancellationToken);
        AddAudit(dbContext, actor, "PortalAccessGrantAdded", grant.ResourceKey, new
        {
            grant.SubjectType,
            grant.SubjectKey
        });
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task RemoveGrantAsync(
        PortalActor actor,
        Guid grantId,
        CancellationToken cancellationToken = default)
    {
        await EnsureCanManageAsync(actor, cancellationToken);
        await using PortalDbContext dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        PortalAccessGrant grant = await dbContext.PortalAccessGrants
            .FirstOrDefaultAsync(item => item.Id == grantId, cancellationToken)
            ?? throw new KeyNotFoundException("دسترسی یافت نشد.");

        dbContext.PortalAccessGrants.Remove(grant);
        await BumpAuthorizationVersionsAsync(dbContext, grant, cancellationToken);
        AddAudit(dbContext, actor, "PortalAccessGrantRemoved", grant.ResourceKey, new
        {
            grant.SubjectType,
            grant.SubjectKey
        });
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<PortalRoleOption>> GetAssignableRolesAsync(
        PortalActor actor,
        CancellationToken cancellationToken = default)
    {
        await EnsureCanManageAsync(actor, cancellationToken);
        await using PortalDbContext dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        ApplicationRole[] roles = await dbContext.Roles.AsNoTracking()
            .OrderBy(role => role.Name)
            .ToArrayAsync(cancellationToken);

        List<PortalRoleOption> result = [];
        foreach (ApplicationRole role in roles)
        {
            int userCount = await dbContext.UserRoles.AsNoTracking()
                .CountAsync(item => item.RoleId == role.Id, cancellationToken);
            result.Add(MapRole(role, userCount));
        }

        return result;
    }

    public async Task<PortalRoleOption> CreateRoleAsync(
        PortalActor actor,
        string name,
        string description,
        CancellationToken cancellationToken = default)
    {
        await EnsureCanManageAsync(actor, cancellationToken);
        string normalizedName = NormalizeRoleName(name);
        string normalizedDescription = NormalizeRoleDescription(description);

        if (await roleManager.RoleExistsAsync(normalizedName))
        {
            throw new InvalidOperationException("نقشی با این نام از قبل وجود دارد.");
        }

        ApplicationRole role = new()
        {
            Id = Guid.NewGuid(),
            Name = normalizedName,
            NormalizedName = normalizedName.ToUpperInvariant(),
            Description = normalizedDescription,
            IsSystem = PortalRoles.IsSystemRoleName(normalizedName)
        };
        IdentityResult createResult = await roleManager.CreateAsync(role);
        if (!createResult.Succeeded)
        {
            throw new InvalidOperationException(string.Join("؛ ", createResult.Errors.Select(error => error.Description)));
        }

        await AddAuditAsync(actor, "PortalRoleCreated", role.Name!, new { role.Description }, cancellationToken);
        return MapRole(role, 0);
    }

    public async Task UpdateRoleAsync(
        PortalActor actor,
        Guid roleId,
        string description,
        CancellationToken cancellationToken = default)
    {
        await EnsureCanManageAsync(actor, cancellationToken);
        ApplicationRole role = await roleManager.FindByIdAsync(roleId.ToString("D"))
            ?? throw new KeyNotFoundException("نقش یافت نشد.");

        role.Description = NormalizeRoleDescription(description);
        if (PortalRoles.IsSystemRoleName(role.Name ?? string.Empty))
        {
            role.IsSystem = true;
        }

        IdentityResult updateResult = await roleManager.UpdateAsync(role);
        if (!updateResult.Succeeded)
        {
            throw new InvalidOperationException(string.Join("؛ ", updateResult.Errors.Select(error => error.Description)));
        }

        await AddAuditAsync(actor, "PortalRoleUpdated", role.Name!, new { role.Description }, cancellationToken);
    }

    public async Task DeleteRoleAsync(
        PortalActor actor,
        Guid roleId,
        CancellationToken cancellationToken = default)
    {
        await EnsureCanManageAsync(actor, cancellationToken);
        ApplicationRole role = await roleManager.FindByIdAsync(roleId.ToString("D"))
            ?? throw new KeyNotFoundException("نقش یافت نشد.");

        if (role.IsSystem || PortalRoles.IsSystemRoleName(role.Name ?? string.Empty))
        {
            throw new InvalidOperationException("نقش‌های سیستمی قابل حذف نیستند.");
        }

        await using PortalDbContext dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        bool assigned = await dbContext.UserRoles.AnyAsync(item => item.RoleId == role.Id, cancellationToken);
        if (assigned)
        {
            throw new InvalidOperationException("نقش به کاربرانی اختصاص داده شده و قابل حذف نیست.");
        }

        PortalAccessGrant[] grants = await dbContext.PortalAccessGrants
            .Where(grant => grant.SubjectType == PortalAccessSubjectType.Role && grant.SubjectKey == role.Name)
            .ToArrayAsync(cancellationToken);
        dbContext.PortalAccessGrants.RemoveRange(grants);
        await dbContext.SaveChangesAsync(cancellationToken);

        IdentityResult deleteResult = await roleManager.DeleteAsync(role);
        if (!deleteResult.Succeeded)
        {
            throw new InvalidOperationException(string.Join("؛ ", deleteResult.Errors.Select(error => error.Description)));
        }

        await AddAuditAsync(actor, "PortalRoleDeleted", role.Name!, new { role.Description }, cancellationToken);
    }

    private async Task EnsureCanManageAsync(PortalActor actor, CancellationToken cancellationToken)
    {
        if (!await accessEvaluator.HasAccessAsync(actor, PortalResources.AccessManage, cancellationToken))
        {
            throw new UnauthorizedAccessException("اجازه مدیریت دسترسی‌ها را ندارید.");
        }
    }

    private async Task<PortalUserAccessSummary> MapUserAsync(
        ApplicationUser user,
        CancellationToken cancellationToken)
    {
        IList<string> roles = await userManager.GetRolesAsync(user);
        return new PortalUserAccessSummary(
            user.Id,
            user.UserName ?? string.Empty,
            user.DisplayName,
            user.PersonnelCode,
            roles.OrderBy(role => role, StringComparer.OrdinalIgnoreCase).ToArray());
    }

    private static PortalRoleOption MapRole(ApplicationRole role, int userCount) =>
        new(
            role.Id,
            role.Name ?? string.Empty,
            string.IsNullOrWhiteSpace(role.Description) ? role.Name ?? string.Empty : role.Description!,
            role.IsSystem || PortalRoles.IsSystemRoleName(role.Name ?? string.Empty),
            userCount);

    private async Task<int> CountSystemAdministratorsAsync(Guid excludeUserId, CancellationToken cancellationToken)
    {
        await using PortalDbContext dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        ApplicationRole? role = await dbContext.Roles.AsNoTracking()
            .FirstOrDefaultAsync(item => item.Name == PortalRoles.SystemAdministrator, cancellationToken);
        if (role is null)
        {
            return 0;
        }

        return await dbContext.UserRoles.AsNoTracking()
            .CountAsync(item => item.RoleId == role.Id && item.UserId != excludeUserId, cancellationToken);
    }

    private static async Task<Dictionary<Guid, string>> LoadUserDisplayNamesAsync(
        PortalDbContext dbContext,
        IReadOnlyCollection<string> subjectKeys,
        CancellationToken cancellationToken)
    {
        Guid[] userIds = subjectKeys
            .Select(key => Guid.TryParse(key, out Guid id) ? id : Guid.Empty)
            .Where(id => id != Guid.Empty)
            .Distinct()
            .ToArray();
        if (userIds.Length == 0)
        {
            return [];
        }

        return await dbContext.Users.AsNoTracking()
            .Where(user => userIds.Contains(user.Id))
            .ToDictionaryAsync(
                user => user.Id,
                user => string.IsNullOrWhiteSpace(user.DisplayName) ? user.UserName ?? user.Id.ToString("D") : user.DisplayName,
                cancellationToken);
    }

    private static async Task<Dictionary<string, string>> LoadRoleTitlesAsync(
        PortalDbContext dbContext,
        CancellationToken cancellationToken)
    {
        return await dbContext.Roles.AsNoTracking()
            .ToDictionaryAsync(
                role => role.Name ?? role.Id.ToString("D"),
                role => string.IsNullOrWhiteSpace(role.Description) ? role.Name ?? role.Id.ToString("D") : role.Description!,
                StringComparer.OrdinalIgnoreCase,
                cancellationToken);
    }

    private static string ResolveSubjectDisplayName(
        PortalAccessGrant grant,
        Dictionary<Guid, string> userNames,
        Dictionary<string, string> roleTitles) =>
        grant.SubjectType switch
        {
            PortalAccessSubjectType.Everyone => "همه کاربران",
            PortalAccessSubjectType.Role => roleTitles.TryGetValue(grant.SubjectKey, out string? title)
                ? title
                : grant.SubjectKey,
            PortalAccessSubjectType.User when Guid.TryParse(grant.SubjectKey, out Guid userId) &&
                userNames.TryGetValue(userId, out string? name) => name,
            _ => grant.SubjectKey
        };

    private static async Task BumpAuthorizationVersionsAsync(
        PortalDbContext dbContext,
        PortalAccessGrant grant,
        CancellationToken cancellationToken)
    {
        if (grant.SubjectType == PortalAccessSubjectType.User &&
            Guid.TryParse(grant.SubjectKey, out Guid userId))
        {
            ApplicationUser? user = await dbContext.Users.FirstOrDefaultAsync(item => item.Id == userId, cancellationToken);
            if (user is not null)
            {
                user.AuthorizationVersion++;
            }

            return;
        }

        if (grant.SubjectType == PortalAccessSubjectType.Role)
        {
            ApplicationRole? role = await dbContext.Roles.AsNoTracking()
                .FirstOrDefaultAsync(item => item.Name == grant.SubjectKey, cancellationToken);
            if (role is null)
            {
                return;
            }

            Guid[] userIds = await dbContext.UserRoles.AsNoTracking()
                .Where(item => item.RoleId == role.Id)
                .Select(item => item.UserId)
                .ToArrayAsync(cancellationToken);
            ApplicationUser[] users = await dbContext.Users
                .Where(user => userIds.Contains(user.Id))
                .ToArrayAsync(cancellationToken);
            foreach (ApplicationUser user in users)
            {
                user.AuthorizationVersion++;
            }

            return;
        }

        ApplicationUser[] allUsers = await dbContext.Users.ToArrayAsync(cancellationToken);
        foreach (ApplicationUser user in allUsers)
        {
            user.AuthorizationVersion++;
        }
    }

    private async Task AddAuditAsync(
        PortalActor actor,
        string eventType,
        string subject,
        object details,
        CancellationToken cancellationToken)
    {
        await using PortalDbContext dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        AddAudit(dbContext, actor, eventType, subject, details);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static void AddAudit(
        PortalDbContext dbContext,
        PortalActor actor,
        string eventType,
        string subject,
        object details)
    {
        dbContext.AuditEvents.Add(AuditEvent.Create(
            DateTimeOffset.UtcNow,
            eventType,
            "Succeeded",
            actor.UserId,
            actor.Upn,
            subject,
            actor.CorrelationId,
            actor.IpAddress,
            JsonSerializer.Serialize(details)));
    }

    private static string NormalizeRoleName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new InvalidOperationException("نام نقش الزامی است.");
        }

        string normalized = name.Trim();
        if (!RoleNameRegex().IsMatch(normalized))
        {
            throw new InvalidOperationException("نام نقش فقط می‌تواند شامل حروف لاتین، عدد و زیرخط باشد و با حرف شروع شود.");
        }

        if (normalized.Length > 64)
        {
            throw new InvalidOperationException("نام نقش نباید بیشتر از ۶۴ نویسه باشد.");
        }

        return normalized;
    }

    private static string NormalizeRoleDescription(string description)
    {
        if (string.IsNullOrWhiteSpace(description))
        {
            throw new InvalidOperationException("عنوان نمایشی نقش الزامی است.");
        }

        string normalized = description.Trim();
        if (normalized.Length > 512)
        {
            throw new InvalidOperationException("عنوان نقش نباید بیشتر از ۵۱۲ نویسه باشد.");
        }

        return normalized;
    }

    [GeneratedRegex("^[A-Za-z][A-Za-z0-9_]*$", RegexOptions.CultureInvariant)]
    private static partial Regex RoleNameRegex();
}
