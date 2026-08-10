using EmpPortal.Application.Authorization;
using EmpPortal.Domain.Hr;

namespace EmpPortal.Application.Hr;

public sealed record CharityPledgeData(
    Guid Id,
    decimal Amount,
    CharityPledgeMode Mode,
    int StartPersianYear,
    int StartPersianMonth,
    int? EndPersianYear,
    int? EndPersianMonth,
    string? Note,
    bool IsConfirmed,
    DateTimeOffset? ConfirmedAtUtc,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? ResultsExportedAtUtc,
    bool CanUserDelete);

public sealed record CharityPledgeAdminRow(
    Guid Id,
    Guid UserId,
    string UserDisplayName,
    string UserUpn,
    string? PersonnelCode,
    decimal Amount,
    CharityPledgeMode Mode,
    int StartPersianYear,
    int StartPersianMonth,
    int? EndPersianYear,
    int? EndPersianMonth,
    string? Note,
    bool IsConfirmed,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? ResultsExportedAtUtc);

public interface ICharityPledgeService
{
    public Task<IReadOnlyList<CharityPledgeData>> ListMineAsync(
        PortalActor actor,
        CancellationToken cancellationToken = default);

    public Task<CharityPledgeData> CreateAsync(
        PortalActor actor,
        decimal amount,
        CharityPledgeMode mode,
        int startPersianYear,
        int startPersianMonth,
        int? endPersianYear,
        int? endPersianMonth,
        string? note,
        bool confirmSelfDeclaration,
        CancellationToken cancellationToken = default);

    public Task ConfirmAsync(
        PortalActor actor,
        Guid pledgeId,
        CancellationToken cancellationToken = default);

    public Task DeleteAsync(
        PortalActor actor,
        Guid pledgeId,
        CancellationToken cancellationToken = default);

    public Task<IReadOnlyList<CharityPledgeAdminRow>> ListAllForAdminAsync(
        PortalActor actor,
        CancellationToken cancellationToken = default);

    public Task DeleteAsAdminAsync(
        PortalActor actor,
        Guid pledgeId,
        CancellationToken cancellationToken = default);

    public Task<byte[]> ExportExcelAndLockAsync(
        PortalActor actor,
        CancellationToken cancellationToken = default);
}
