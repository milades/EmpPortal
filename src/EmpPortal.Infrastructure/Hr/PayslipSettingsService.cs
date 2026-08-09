using System.Text.Json;
using EmpPortal.Application.Authorization;
using EmpPortal.Application.Hr;
using EmpPortal.Domain.Auditing;
using EmpPortal.Domain.Hr;
using EmpPortal.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EmpPortal.Infrastructure.Hr;

public sealed class PayslipSettingsService(
    IDbContextFactory<PortalDbContext> dbContextFactory,
    IPortalAccessEvaluator accessEvaluator) : IPayslipSettingsService
{
    public async Task<PayslipPeriodSettingData?> GetAsync(
        PortalActor actor,
        int persianYear,
        int persianMonth,
        CancellationToken cancellationToken = default)
    {
        await EnsureCanManageAsync(actor, cancellationToken);
        await using PortalDbContext dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        PayslipPeriodSetting? setting = await dbContext.PayslipPeriodSettings.AsNoTracking()
            .FirstOrDefaultAsync(
                item => item.PersianYear == persianYear && item.PersianMonth == persianMonth,
                cancellationToken);
        return setting is null ? null : Map(setting);
    }

    public async Task<IReadOnlyList<PayslipPeriodSettingData>> ListRecentAsync(
        PortalActor actor,
        int take = 24,
        CancellationToken cancellationToken = default)
    {
        await EnsureCanManageAsync(actor, cancellationToken);
        take = Math.Clamp(take, 1, 120);
        await using PortalDbContext dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        PayslipPeriodSetting[] settings = await dbContext.PayslipPeriodSettings.AsNoTracking()
            .OrderByDescending(item => item.PersianYear)
            .ThenByDescending(item => item.PersianMonth)
            .Take(take)
            .ToArrayAsync(cancellationToken);
        return settings.Select(Map).ToArray();
    }

    public async Task<PayslipPeriodSettingData> SetVisibilityAsync(
        PortalActor actor,
        int persianYear,
        int persianMonth,
        bool isVisibleToEmployees,
        CancellationToken cancellationToken = default)
    {
        await EnsureCanManageAsync(actor, cancellationToken);
        await using PortalDbContext dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        PayslipPeriodSetting? setting = await dbContext.PayslipPeriodSettings
            .FirstOrDefaultAsync(
                item => item.PersianYear == persianYear && item.PersianMonth == persianMonth,
                cancellationToken);

        DateTimeOffset nowUtc = DateTimeOffset.UtcNow;
        if (setting is null)
        {
            setting = PayslipPeriodSetting.Create(
                persianYear,
                persianMonth,
                isVisibleToEmployees,
                actor.UserId,
                nowUtc);
            dbContext.PayslipPeriodSettings.Add(setting);
        }
        else
        {
            setting.SetVisibility(isVisibleToEmployees, actor.UserId, nowUtc);
        }

        dbContext.AuditEvents.Add(AuditEvent.Create(
            nowUtc,
            "PayslipPeriodVisibilityChanged",
            "Succeeded",
            actor.UserId,
            actor.Upn,
            $"{persianYear}/{persianMonth:00}",
            actor.CorrelationId,
            actor.IpAddress,
            JsonSerializer.Serialize(new { persianYear, persianMonth, isVisibleToEmployees })));

        await dbContext.SaveChangesAsync(cancellationToken);
        return Map(setting);
    }

    public async Task<bool> IsPeriodVisibleToEmployeesAsync(
        int persianYear,
        int persianMonth,
        CancellationToken cancellationToken = default)
    {
        await using PortalDbContext dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await dbContext.PayslipPeriodSettings.AsNoTracking()
            .AnyAsync(
                item => item.PersianYear == persianYear &&
                    item.PersianMonth == persianMonth &&
                    item.IsVisibleToEmployees,
                cancellationToken);
    }

    private async Task EnsureCanManageAsync(PortalActor actor, CancellationToken cancellationToken)
    {
        if (!await accessEvaluator.HasAccessAsync(actor, PortalResources.PayslipSettings, cancellationToken))
        {
            throw new UnauthorizedAccessException("اجازه مدیریت تنظیمات فیش حقوقی را ندارید.");
        }
    }

    private static PayslipPeriodSettingData Map(PayslipPeriodSetting setting) =>
        new(
            setting.Id,
            setting.PersianYear,
            setting.PersianMonth,
            setting.IsVisibleToEmployees,
            setting.UpdatedByUserId,
            setting.UpdatedAtUtc);
}
