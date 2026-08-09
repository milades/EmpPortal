using EmpPortal.Domain.Access;

namespace EmpPortal.Application.Authorization;

public sealed record PortalActor(
    Guid UserId,
    string Upn,
    string DisplayName,
    IReadOnlySet<string> Roles,
    string CorrelationId,
    string? IpAddress);

public sealed record PortalAccessGrantData(
    Guid Id,
    string ResourceKey,
    string ResourceTitle,
    PortalAccessSubjectType SubjectType,
    string SubjectKey,
    string SubjectDisplayName,
    DateTimeOffset CreatedAtUtc);

public sealed record PortalUserAccessSummary(
    Guid UserId,
    string Upn,
    string DisplayName,
    string? PersonnelCode,
    IReadOnlyList<string> Roles);

public sealed record PortalRoleOption(
    Guid Id,
    string Name,
    string DisplayName,
    bool IsSystem,
    int UserCount);

public interface IPortalAccessEvaluator
{
    public Task EnsureDefaultsAsync(CancellationToken cancellationToken = default);

    public Task<bool> HasAccessAsync(
        PortalActor actor,
        string resourceKey,
        CancellationToken cancellationToken = default);

    public Task<IReadOnlySet<string>> GetGrantedResourceKeysAsync(
        PortalActor actor,
        CancellationToken cancellationToken = default);
}

public interface IPortalAccessAdministrationService
{
    public Task EnsureDefaultsAsync(CancellationToken cancellationToken = default);

    public Task<IReadOnlyList<PortalUserAccessSummary>> SearchUsersAsync(
        PortalActor actor,
        string? search,
        int take = 40,
        CancellationToken cancellationToken = default);

    public Task<PortalUserAccessSummary?> GetUserAsync(
        PortalActor actor,
        Guid userId,
        CancellationToken cancellationToken = default);

    public Task SetUserRolesAsync(
        PortalActor actor,
        Guid userId,
        IReadOnlyCollection<string> roleNames,
        CancellationToken cancellationToken = default);

    public Task SetPersonnelCodeAsync(
        PortalActor actor,
        Guid userId,
        string? personnelCode,
        CancellationToken cancellationToken = default);

    public Task<IReadOnlyList<PortalAccessGrantData>> GetGrantsAsync(
        PortalActor actor,
        CancellationToken cancellationToken = default);

    public Task AddGrantAsync(
        PortalActor actor,
        string resourceKey,
        PortalAccessSubjectType subjectType,
        string subjectKey,
        CancellationToken cancellationToken = default);

    public Task RemoveGrantAsync(
        PortalActor actor,
        Guid grantId,
        CancellationToken cancellationToken = default);

    public Task<IReadOnlyList<PortalRoleOption>> GetAssignableRolesAsync(
        PortalActor actor,
        CancellationToken cancellationToken = default);

    public Task<PortalRoleOption> CreateRoleAsync(
        PortalActor actor,
        string name,
        string description,
        CancellationToken cancellationToken = default);

    public Task UpdateRoleAsync(
        PortalActor actor,
        Guid roleId,
        string description,
        CancellationToken cancellationToken = default);

    public Task DeleteRoleAsync(
        PortalActor actor,
        Guid roleId,
        CancellationToken cancellationToken = default);
}
