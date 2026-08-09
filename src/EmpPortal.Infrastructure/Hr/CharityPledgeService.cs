using System.Text.Json;
using EmpPortal.Application.Authorization;
using EmpPortal.Application.Hr;
using EmpPortal.Domain.Auditing;
using EmpPortal.Domain.Hr;
using EmpPortal.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EmpPortal.Infrastructure.Hr;

public sealed class CharityPledgeService(
    IDbContextFactory<PortalDbContext> dbContextFactory,
    IPortalAccessEvaluator accessEvaluator) : ICharityPledgeService
{
    public async Task<IReadOnlyList<CharityPledgeData>> ListMineAsync(
        PortalActor actor,
        CancellationToken cancellationToken = default)
    {
        await EnsureCanViewAsync(actor, cancellationToken);
        await using PortalDbContext dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        CharityPledge[] pledges = await dbContext.CharityPledges.AsNoTracking()
            .Where(pledge => pledge.UserId == actor.UserId)
            .OrderByDescending(pledge => pledge.CreatedAtUtc)
            .ToArrayAsync(cancellationToken);
        return pledges.Select(Map).ToArray();
    }

    public async Task<CharityPledgeData> CreateAsync(
        PortalActor actor,
        decimal amount,
        CharityPledgeMode mode,
        int startPersianYear,
        int startPersianMonth,
        int? endPersianYear,
        int? endPersianMonth,
        string? note,
        bool confirmSelfDeclaration,
        CancellationToken cancellationToken = default)
    {
        await EnsureCanViewAsync(actor, cancellationToken);
        if (!confirmSelfDeclaration)
        {
            throw new InvalidOperationException("برای ثبت انفاق باید خوداظهاری را تأیید کنید.");
        }

        DateTimeOffset nowUtc = DateTimeOffset.UtcNow;
        CharityPledge pledge = mode switch
        {
            CharityPledgeMode.OneTime => CharityPledge.CreateOneTime(
                actor.UserId,
                amount,
                startPersianYear,
                startPersianMonth,
                note,
                confirm: true,
                actor.UserId,
                nowUtc),
            CharityPledgeMode.MonthlyRange => CharityPledge.CreateMonthlyRange(
                actor.UserId,
                amount,
                startPersianYear,
                startPersianMonth,
                endPersianYear ?? throw new InvalidOperationException("ماه پایان بازه الزامی است."),
                endPersianMonth ?? throw new InvalidOperationException("ماه پایان بازه الزامی است."),
                note,
                confirm: true,
                actor.UserId,
                nowUtc),
            _ => throw new ArgumentOutOfRangeException(nameof(mode))
        };

        await using PortalDbContext dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        dbContext.CharityPledges.Add(pledge);
        dbContext.AuditEvents.Add(AuditEvent.Create(
            nowUtc,
            "CharityPledgeCreated",
            "Succeeded",
            actor.UserId,
            actor.Upn,
            pledge.Id.ToString("D"),
            actor.CorrelationId,
            actor.IpAddress,
            JsonSerializer.Serialize(new
            {
                pledge.Amount,
                pledge.Mode,
                pledge.StartPersianYear,
                pledge.StartPersianMonth,
                pledge.EndPersianYear,
                pledge.EndPersianMonth
            })));
        await dbContext.SaveChangesAsync(cancellationToken);
        return Map(pledge);
    }

    public async Task ConfirmAsync(
        PortalActor actor,
        Guid pledgeId,
        CancellationToken cancellationToken = default)
    {
        await EnsureCanViewAsync(actor, cancellationToken);
        await using PortalDbContext dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        CharityPledge pledge = await dbContext.CharityPledges
            .FirstOrDefaultAsync(item => item.Id == pledgeId && item.UserId == actor.UserId, cancellationToken)
            ?? throw new KeyNotFoundException("اعلام انفاق یافت نشد.");

        DateTimeOffset nowUtc = DateTimeOffset.UtcNow;
        pledge.Confirm(actor.UserId, nowUtc);
        dbContext.AuditEvents.Add(AuditEvent.Create(
            nowUtc,
            "CharityPledgeConfirmed",
            "Succeeded",
            actor.UserId,
            actor.Upn,
            pledge.Id.ToString("D"),
            actor.CorrelationId,
            actor.IpAddress));
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(
        PortalActor actor,
        Guid pledgeId,
        CancellationToken cancellationToken = default)
    {
        await EnsureCanViewAsync(actor, cancellationToken);
        await using PortalDbContext dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        CharityPledge pledge = await dbContext.CharityPledges
            .FirstOrDefaultAsync(item => item.Id == pledgeId && item.UserId == actor.UserId, cancellationToken)
            ?? throw new KeyNotFoundException("اعلام انفاق یافت نشد.");

        dbContext.CharityPledges.Remove(pledge);
        dbContext.AuditEvents.Add(AuditEvent.Create(
            DateTimeOffset.UtcNow,
            "CharityPledgeDeleted",
            "Succeeded",
            actor.UserId,
            actor.Upn,
            pledge.Id.ToString("D"),
            actor.CorrelationId,
            actor.IpAddress,
            JsonSerializer.Serialize(new { pledge.Amount, pledge.Mode })));
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task EnsureCanViewAsync(PortalActor actor, CancellationToken cancellationToken)
    {
        if (!await accessEvaluator.HasAccessAsync(actor, PortalResources.CharityView, cancellationToken))
        {
            throw new UnauthorizedAccessException("اجازه دسترسی به بخش انفاق را ندارید.");
        }
    }

    private static CharityPledgeData Map(CharityPledge pledge) =>
        new(
            pledge.Id,
            pledge.Amount,
            pledge.Mode,
            pledge.StartPersianYear,
            pledge.StartPersianMonth,
            pledge.EndPersianYear,
            pledge.EndPersianMonth,
            pledge.Note,
            pledge.IsConfirmed,
            pledge.ConfirmedAtUtc,
            pledge.CreatedAtUtc);
}
