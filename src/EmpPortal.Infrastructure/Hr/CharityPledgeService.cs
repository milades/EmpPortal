using System.Globalization;
using System.Text.Json;
using ClosedXML.Excel;
using EmpPortal.Application.Authorization;
using EmpPortal.Application.Hr;
using EmpPortal.Application.Localization;
using EmpPortal.Domain.Auditing;
using EmpPortal.Domain.Hr;
using EmpPortal.Infrastructure.Persistence;
using EmpPortal.Infrastructure.Persistence.Identity;
using Microsoft.EntityFrameworkCore;

namespace EmpPortal.Infrastructure.Hr;

public sealed class CharityPledgeService(
    IDbContextFactory<PortalDbContext> dbContextFactory,
    IPortalAccessEvaluator accessEvaluator) : ICharityPledgeService
{
    private const int MaximumExportRows = 50_000;

    public async Task<PagedResult<CharityPledgeData>> ListMineAsync(
        CharityPledgeListQuery query,
        PortalActor actor,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        await EnsureCanViewAsync(actor, cancellationToken);
        int page = Math.Max(1, query.Page);
        int pageSize = Math.Clamp(query.PageSize, 1, 100);
        await using PortalDbContext dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        IQueryable<CharityPledge> pledgesQuery = dbContext.CharityPledges.AsNoTracking()
            .Where(pledge => pledge.UserId == actor.UserId);
        long totalCount = await pledgesQuery.LongCountAsync(cancellationToken);
        page = ClampPage(page, pageSize, totalCount);
        CharityPledge[] pledges = await pledgesQuery
            .OrderByDescending(pledge => pledge.CreatedAtUtc)
            .ThenByDescending(pledge => pledge.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToArrayAsync(cancellationToken);
        return new PagedResult<CharityPledgeData>(
            pledges.Select(MapMine).ToArray(),
            totalCount,
            page,
            pageSize);
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
        return MapMine(pledge);
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

        if (pledge.IsResultsExported)
        {
            throw new InvalidOperationException(
                "پس از خروجی اکسل نتایج توسط مدیریت، حذف انفاق فقط توسط مدیر امکان‌پذیر است.");
        }

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

    public async Task<PagedResult<CharityPledgeAdminRow>> ListAllForAdminAsync(
        CharityPledgeListQuery query,
        PortalActor actor,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        await EnsureCanManageAsync(actor, cancellationToken);
        int page = Math.Max(1, query.Page);
        int pageSize = Math.Clamp(query.PageSize, 1, 100);
        await using PortalDbContext dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        long totalCount = await dbContext.CharityPledges.AsNoTracking()
            .LongCountAsync(cancellationToken);
        page = ClampPage(page, pageSize, totalCount);
        CharityPledgeAdminRow[] rows = await (
            from pledge in dbContext.CharityPledges.AsNoTracking()
            join user in dbContext.Users.AsNoTracking() on pledge.UserId equals user.Id
            orderby pledge.CreatedAtUtc descending, pledge.Id descending
            select new CharityPledgeAdminRow(
                pledge.Id,
                pledge.UserId,
                user.DisplayName,
                user.UserName ?? string.Empty,
                user.PersonnelCode,
                pledge.Amount,
                pledge.Mode,
                pledge.StartPersianYear,
                pledge.StartPersianMonth,
                pledge.EndPersianYear,
                pledge.EndPersianMonth,
                pledge.Note,
                pledge.IsConfirmed,
                pledge.CreatedAtUtc,
                pledge.ResultsExportedAtUtc))
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToArrayAsync(cancellationToken);
        return new PagedResult<CharityPledgeAdminRow>(rows, totalCount, page, pageSize);
    }

    public async Task DeleteAsAdminAsync(
        PortalActor actor,
        Guid pledgeId,
        CancellationToken cancellationToken = default)
    {
        await EnsureCanManageAsync(actor, cancellationToken);
        await using PortalDbContext dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        CharityPledge pledge = await dbContext.CharityPledges
            .FirstOrDefaultAsync(item => item.Id == pledgeId, cancellationToken)
            ?? throw new KeyNotFoundException("اعلام انفاق یافت نشد.");

        dbContext.CharityPledges.Remove(pledge);
        dbContext.AuditEvents.Add(AuditEvent.Create(
            DateTimeOffset.UtcNow,
            "CharityPledgeDeletedByAdmin",
            "Succeeded",
            actor.UserId,
            actor.Upn,
            pledge.Id.ToString("D"),
            actor.CorrelationId,
            actor.IpAddress,
            JsonSerializer.Serialize(new
            {
                pledge.UserId,
                pledge.Amount,
                pledge.Mode,
                pledge.ResultsExportedAtUtc
            })));
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<byte[]> ExportExcelAndLockAsync(
        PortalActor actor,
        CancellationToken cancellationToken = default)
    {
        await EnsureCanManageAsync(actor, cancellationToken);
        await using PortalDbContext dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var rows = await (
            from pledge in dbContext.CharityPledges
            join user in dbContext.Users.AsNoTracking() on pledge.UserId equals user.Id
            orderby pledge.CreatedAtUtc descending
            select new { Pledge = pledge, User = user })
            .Take(MaximumExportRows)
            .ToListAsync(cancellationToken);

        DateTimeOffset nowUtc = DateTimeOffset.UtcNow;
        foreach (var row in rows)
        {
            row.Pledge.MarkResultsExported(actor.UserId, nowUtc);
        }

        dbContext.AuditEvents.Add(AuditEvent.Create(
            nowUtc,
            "CharityPledgesExported",
            "Succeeded",
            actor.UserId,
            actor.Upn,
            rows.Count.ToString(CultureInfo.InvariantCulture),
            actor.CorrelationId,
            actor.IpAddress,
            JsonSerializer.Serialize(new { Count = rows.Count })));
        await dbContext.SaveChangesAsync(cancellationToken);

        using XLWorkbook workbook = new();
        IXLWorksheet sheet = workbook.Worksheets.Add("انفاق");
        sheet.RightToLeft = true;
        string[] headers =
        [
            "کد پرسنلی",
            "نام نمایشی",
            "نام کاربری",
            "مبلغ",
            "نوع",
            "سال شروع",
            "ماه شروع",
            "سال پایان",
            "ماه پایان",
            "یادداشت",
            "زمان ثبت",
            "وضعیت خروجی"
        ];
        for (int index = 0; index < headers.Length; index++)
        {
            IXLCell cell = sheet.Cell(1, index + 1);
            cell.Value = headers[index];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#0F766E");
            cell.Style.Font.FontColor = XLColor.White;
        }

        int excelRow = 2;
        foreach (var row in rows)
        {
            sheet.Cell(excelRow, 1).Value = SetSafeText(row.User.PersonnelCode);
            sheet.Cell(excelRow, 2).Value = SetSafeText(row.User.DisplayName);
            sheet.Cell(excelRow, 3).Value = SetSafeText(row.User.UserName);
            sheet.Cell(excelRow, 4).Value = row.Pledge.Amount;
            sheet.Cell(excelRow, 5).Value = row.Pledge.Mode == CharityPledgeMode.OneTime ? "یک‌بار" : "بازه ماهانه";
            sheet.Cell(excelRow, 6).Value = row.Pledge.StartPersianYear;
            sheet.Cell(excelRow, 7).Value = PersianDateTimeFormatter.MonthNames[row.Pledge.StartPersianMonth - 1];
            sheet.Cell(excelRow, 8).Value = row.Pledge.EndPersianYear?.ToString(CultureInfo.InvariantCulture) ?? "—";
            sheet.Cell(excelRow, 9).Value = row.Pledge.EndPersianMonth is int endMonth
                ? PersianDateTimeFormatter.MonthNames[endMonth - 1]
                : "—";
            sheet.Cell(excelRow, 10).Value = SetSafeText(row.Pledge.Note);
            sheet.Cell(excelRow, 11).Value = PersianDateTimeFormatter.FormatDateTime(row.Pledge.CreatedAtUtc);
            sheet.Cell(excelRow, 12).Value = "خروجی گرفته‌شده";
            excelRow++;
        }

        sheet.Columns().AdjustToContents();
        sheet.RangeUsed()?.SetAutoFilter();
        sheet.SheetView.FreezeRows(1);

        using MemoryStream stream = new();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    private async Task EnsureCanViewAsync(PortalActor actor, CancellationToken cancellationToken)
    {
        if (!await accessEvaluator.HasAccessAsync(actor, PortalResources.CharityView, cancellationToken))
        {
            throw new UnauthorizedAccessException("اجازه دسترسی به بخش انفاق را ندارید.");
        }
    }

    private async Task EnsureCanManageAsync(PortalActor actor, CancellationToken cancellationToken)
    {
        if (!await accessEvaluator.HasAccessAsync(actor, PortalResources.CharityManage, cancellationToken))
        {
            throw new UnauthorizedAccessException("اجازه مدیریت انفاق را ندارید.");
        }
    }

    private static CharityPledgeData MapMine(CharityPledge pledge) =>
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
            pledge.CreatedAtUtc,
            pledge.ResultsExportedAtUtc,
            CanUserDelete: !pledge.IsResultsExported);

    private static int ClampPage(int page, int pageSize, long totalCount)
    {
        int totalPages = totalCount == 0
            ? 1
            : Math.Max(1, (int)Math.Ceiling(Math.Min(totalCount, int.MaxValue) / (double)pageSize));
        return Math.Min(page, totalPages);
    }

    private static string SetSafeText(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        string trimmed = value.Trim();
        return trimmed.StartsWith('=') || trimmed.StartsWith('+') || trimmed.StartsWith('-') || trimmed.StartsWith('@')
            ? "'" + trimmed
            : trimmed;
    }
}
