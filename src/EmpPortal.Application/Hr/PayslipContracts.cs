using EmpPortal.Application.Authorization;

namespace EmpPortal.Application.Hr;

public sealed record PayslipPeriodSettingData(
    Guid Id,
    int PersianYear,
    int PersianMonth,
    bool IsVisibleToEmployees,
    Guid UpdatedByUserId,
    DateTimeOffset UpdatedAtUtc);

public interface IPayslipSettingsService
{
    public Task<PayslipPeriodSettingData?> GetAsync(
        PortalActor actor,
        int persianYear,
        int persianMonth,
        CancellationToken cancellationToken = default);

    public Task<IReadOnlyList<PayslipPeriodSettingData>> ListRecentAsync(
        PortalActor actor,
        int take = 24,
        CancellationToken cancellationToken = default);

    public Task<PayslipPeriodSettingData> SetVisibilityAsync(
        PortalActor actor,
        int persianYear,
        int persianMonth,
        bool isVisibleToEmployees,
        CancellationToken cancellationToken = default);

    public Task<bool> IsPeriodVisibleToEmployeesAsync(
        int persianYear,
        int persianMonth,
        CancellationToken cancellationToken = default);
}

public sealed record PayslipPdfResult(
    byte[] Content,
    string FileName,
    string ContentType);

public interface IPayslipReportService
{
    public Task<PayslipPdfResult> RenderMyPayslipPdfAsync(
        PortalActor actor,
        int persianYear,
        int persianMonth,
        CancellationToken cancellationToken = default);
}
