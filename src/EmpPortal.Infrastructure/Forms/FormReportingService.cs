using System.Globalization;
using System.Text.Json;
using ClosedXML.Excel;
using EmpPortal.Application.Forms;
using EmpPortal.Application.Forms.Schema;
using EmpPortal.Application.Localization;
using EmpPortal.Domain.Forms;
using EmpPortal.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using QuestPDF.Drawing;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace EmpPortal.Infrastructure.Forms;

public sealed class FormReportingService(
    IDbContextFactory<PortalDbContext> dbContextFactory,
    IOptions<FormPdfOptions> pdfOptions,
    IHostEnvironment hostEnvironment) : IFormReportingService
{
    private const int MaximumExportRows = 50_000;
    private static readonly Lock PdfConfigurationLock = new();
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private static bool isPdfConfigured;

    public async Task<PagedResult<SubmissionSummary>> GetSubmissionsAsync(
        Guid formId,
        SubmissionQuery query,
        FormActor actor,
        CancellationToken cancellationToken = default)
    {
        EnsureValidActor(actor);
        ArgumentNullException.ThrowIfNull(query);
        (int page, int pageSize) = NormalizePaging(query.Page, query.PageSize);

        await using PortalDbContext dbContext = await dbContextFactory.CreateDbContextAsync(
            cancellationToken);
        FormDefinition form = await GetAuthorizedFormAsync(
            dbContext,
            formId,
            actor,
            FormAccessRights.ViewSubmissions,
            cancellationToken);

        string? normalizedSearch = string.IsNullOrWhiteSpace(query.Search) ? null : query.Search.Trim();
        var rows =
            from submission in dbContext.FormSubmissions.AsNoTracking()
            join user in dbContext.Users.AsNoTracking()
                on submission.SubmittedByUserId equals user.Id
            where submission.FormId == form.Id &&
                  (query.Status == null || submission.Status == query.Status) &&
                  (query.FromUtc == null || submission.CreatedAtUtc >= query.FromUtc) &&
                  (query.ToUtc == null || submission.CreatedAtUtc <= query.ToUtc) &&
                  (normalizedSearch == null ||
                   submission.TrackingCode.Contains(normalizedSearch) ||
                   (user.UserName != null && user.UserName.Contains(normalizedSearch)) ||
                   user.DisplayName.Contains(normalizedSearch))
            select new { Submission = submission, User = user };
        long totalCount = await rows.LongCountAsync(cancellationToken);
        SubmissionSummary[] items = await rows
            .OrderByDescending(row => row.Submission.SubmittedAtUtc)
            .ThenByDescending(row => row.Submission.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(row => new SubmissionSummary(
                row.Submission.Id,
                row.Submission.TrackingCode,
                row.User.UserName ?? string.Empty,
                row.User.DisplayName,
                row.Submission.Status,
                row.Submission.CreatedAtUtc,
                row.Submission.SubmittedAtUtc))
            .ToArrayAsync(cancellationToken);

        return new PagedResult<SubmissionSummary>(items, totalCount, page, pageSize);
    }

    public async Task<SubmissionDetails?> GetSubmissionAsync(
        Guid submissionId,
        FormActor actor,
        CancellationToken cancellationToken = default)
    {
        EnsureValidActor(actor);
        await using PortalDbContext dbContext = await dbContextFactory.CreateDbContextAsync(
            cancellationToken);
        SubmissionExportRow? row = await BuildDetailsQuery(
                dbContext,
                submissionId,
                formId: null,
                query: null)
            .SingleOrDefaultAsync(cancellationToken);
        if (row is null)
        {
            return null;
        }

        FormDefinition form = await GetAuthorizedFormAsync(
            dbContext,
            row.FormId,
            actor,
            FormAccessRights.ViewSubmissions,
            cancellationToken);
        _ = form;
        return ToDetails(row);
    }

    public async Task<byte[]> ExportExcelAsync(
        Guid formId,
        SubmissionQuery query,
        FormActor actor,
        CancellationToken cancellationToken = default)
    {
        EnsureValidActor(actor);
        ArgumentNullException.ThrowIfNull(query);
        await using PortalDbContext dbContext = await dbContextFactory.CreateDbContextAsync(
            cancellationToken);
        FormDefinition form = await GetAuthorizedFormAsync(
            dbContext,
            formId,
            actor,
            FormAccessRights.Export,
            cancellationToken);

        SubmissionExportRow[] submissions = await BuildDetailsQuery(
                dbContext,
                submissionId: null,
                form.Id,
                query)
            .Take(MaximumExportRows + 1)
            .ToArrayAsync(cancellationToken);
        if (submissions.Length > MaximumExportRows)
        {
            throw new InvalidOperationException(
                $"خروجی Excel نمی‌تواند بیش از {MaximumExportRows:N0} پاسخ داشته باشد. بازه گزارش را محدود کنید.");
        }

        List<ReportColumn> columns = BuildReportColumns(submissions);
        using XLWorkbook workbook = new();
        IXLWorksheet worksheet = workbook.Worksheets.Add("پاسخ‌ها");
        worksheet.RightToLeft = true;

        string[] fixedHeaders =
        [
            "کد رهگیری", "وضعیت", "نام کاربر", "نام کاربری", "زمان ایجاد", "زمان ثبت نهایی"
        ];
        for (int index = 0; index < fixedHeaders.Length; index++)
        {
            worksheet.Cell(1, index + 1).Value = fixedHeaders[index];
        }

        for (int index = 0; index < columns.Count; index++)
        {
            worksheet.Cell(1, fixedHeaders.Length + index + 1).Value = columns[index].Label;
        }

        for (int rowIndex = 0; rowIndex < submissions.Length; rowIndex++)
        {
            SubmissionExportRow submission = submissions[rowIndex];
            Dictionary<string, JsonElement> values = DeserializeValues(submission.DataJson);
            int excelRow = rowIndex + 2;
            SetSafeText(worksheet.Cell(excelRow, 1), submission.TrackingCode);
            SetSafeText(worksheet.Cell(excelRow, 2), GetStatusText(submission.Status));
            SetSafeText(worksheet.Cell(excelRow, 3), submission.UserDisplayName);
            SetSafeText(worksheet.Cell(excelRow, 4), submission.UserUpn);
            SetSafeText(
                worksheet.Cell(excelRow, 5),
                PersianDateTimeFormatter.FormatDateTime(submission.CreatedAtUtc));
            SetSafeText(
                worksheet.Cell(excelRow, 6),
                PersianDateTimeFormatter.FormatDateTime(submission.SubmittedAtUtc));

            for (int columnIndex = 0; columnIndex < columns.Count; columnIndex++)
            {
                ReportColumn column = columns[columnIndex];
                if (values.TryGetValue(column.Key, out JsonElement value))
                {
                    SetSafeText(
                        worksheet.Cell(excelRow, fixedHeaders.Length + columnIndex + 1),
                        FormatValue(column.Element, value));
                }
            }
        }

        int lastColumn = fixedHeaders.Length + columns.Count;
        IXLRange header = worksheet.Range(1, 1, 1, lastColumn);
        header.Style.Font.Bold = true;
        header.Style.Font.FontColor = XLColor.White;
        header.Style.Fill.BackgroundColor = XLColor.FromHtml("#0F766E");
        header.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        worksheet.SheetView.FreezeRows(1);
        worksheet.Range(1, 1, Math.Max(1, submissions.Length + 1), lastColumn).SetAutoFilter();
        worksheet.Columns(1, lastColumn).AdjustToContents(1, Math.Min(submissions.Length + 1, 200));
        foreach (IXLColumn column in worksheet.Columns(1, lastColumn))
        {
            if (column.Width > 45)
            {
                column.Width = 45;
            }
        }

        worksheet.RangeUsed()?.Style.Alignment.SetVertical(XLAlignmentVerticalValues.Center);
        worksheet.RangeUsed()?.Style.Alignment.SetWrapText();
        worksheet.Cell(1, 1).CreateComment().AddText($"فرم: {form.Title}");

        await using MemoryStream stream = new();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    public async Task<byte[]> ExportPdfAsync(
        Guid submissionId,
        FormActor actor,
        CancellationToken cancellationToken = default)
    {
        EnsureValidActor(actor);
        await using PortalDbContext dbContext = await dbContextFactory.CreateDbContextAsync(
            cancellationToken);
        SubmissionExportRow row = await BuildDetailsQuery(
                dbContext,
                submissionId,
                formId: null,
                query: null)
            .SingleOrDefaultAsync(cancellationToken) ??
            throw new KeyNotFoundException("پاسخ فرم یافت نشد.");
        FormDefinition form = await GetAuthorizedFormAsync(
            dbContext,
            row.FormId,
            actor,
            FormAccessRights.Export,
            cancellationToken);
        ConfigurePdf();

        FormSchemaDefinition schema = FormSchemaSerializer.Deserialize(row.DefinitionJson);
        Dictionary<string, JsonElement> values = DeserializeValues(row.DataJson);
        IReadOnlyList<(FormElementDefinition Element, string Value)> fields = FlattenElements(schema)
            .Where(element => !IsContentElement(element.Type) && values.ContainsKey(element.Key))
            .Select(element => (element, FormatValue(element, values[element.Key])))
            .ToArray();

        IDocument document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(28);
                page.ContentFromRightToLeft();
                page.DefaultTextStyle(style => style.FontFamily("Vazirmatn").FontSize(10));
                page.Header().Column(column =>
                {
                    column.Item().Text(form.Title).Bold().FontSize(18).FontColor(Colors.Teal.Darken2);
                    column.Item().PaddingTop(4).Text($"کد رهگیری: {row.TrackingCode}");
                    column.Item().Text($"کاربر: {row.UserDisplayName} ({row.UserUpn})");
                    column.Item().Text($"وضعیت: {GetStatusText(row.Status)}");
                });
                page.Content().PaddingVertical(18).Column(column =>
                {
                    foreach ((FormElementDefinition element, string value) in fields)
                    {
                        column.Item().PaddingBottom(8).BorderBottom(1).BorderColor(Colors.Grey.Lighten2)
                            .PaddingBottom(8).Column(field =>
                            {
                                field.Item().Text(element.Label).Bold().FontColor(Colors.Grey.Darken2);
                                field.Item().PaddingTop(3).Text(string.IsNullOrWhiteSpace(value) ? "—" : value);
                            });
                    }
                });
                page.Footer().AlignCenter().Text(text =>
                {
                    text.Span("صفحه ");
                    text.CurrentPageNumber();
                    text.Span(" از ");
                    text.TotalPages();
                });
            });
        });

        return document.GeneratePdf();
    }

    private void ConfigurePdf()
    {
        if (isPdfConfigured)
        {
            return;
        }

        lock (PdfConfigurationLock)
        {
            if (isPdfConfigured)
            {
                return;
            }

            FormPdfOptions options = pdfOptions.Value;
            if (!Enum.TryParse(options.License, ignoreCase: true, out LicenseType license))
            {
                throw new InvalidOperationException(
                    "نوع مجوز PDF تنظیم نشده است. Forms:Pdf:License باید Community، Professional یا Enterprise باشد.");
            }

            if (!hostEnvironment.IsDevelopment() && license == LicenseType.Evaluation)
            {
                throw new InvalidOperationException(
                    "مجوز Evaluation برای محیط عملیاتی QuestPDF مجاز نیست.");
            }

            string regularPath = ResolvePath(options.RegularFontPath);
            string boldPath = ResolvePath(options.BoldFontPath);
            using (FileStream font = File.OpenRead(regularPath))
            {
                FontManager.RegisterFontWithCustomName("Vazirmatn", font);
            }

            using (FileStream font = File.OpenRead(boldPath))
            {
                FontManager.RegisterFontWithCustomName("Vazirmatn", font);
            }

            QuestPDF.Settings.License = license;
            QuestPDF.Settings.UseEnvironmentFonts = false;
            isPdfConfigured = true;
        }
    }

    private string ResolvePath(string configuredPath) => Path.IsPathRooted(configuredPath)
        ? configuredPath
        : Path.GetFullPath(Path.Combine(hostEnvironment.ContentRootPath, configuredPath));

    private static IQueryable<SubmissionExportRow> BuildDetailsQuery(
        PortalDbContext dbContext,
        Guid? submissionId,
        Guid? formId,
        SubmissionQuery? query)
    {
        string? normalizedSearch = string.IsNullOrWhiteSpace(query?.Search)
            ? null
            : query.Search.Trim();
        FormSubmissionStatus? status = query?.Status;
        DateTimeOffset? fromUtc = query?.FromUtc;
        DateTimeOffset? toUtc = query?.ToUtc;
        var rows =
            from submission in dbContext.FormSubmissions.AsNoTracking()
            join version in dbContext.FormVersions.AsNoTracking()
                on submission.FormVersionId equals version.Id
            join user in dbContext.Users.AsNoTracking()
                on submission.SubmittedByUserId equals user.Id
            where (submissionId == null || submission.Id == submissionId) &&
                  (formId == null || submission.FormId == formId) &&
                  (status == null || submission.Status == status) &&
                  (fromUtc == null || submission.CreatedAtUtc >= fromUtc) &&
                  (toUtc == null || submission.CreatedAtUtc <= toUtc) &&
                  (normalizedSearch == null ||
                   submission.TrackingCode.Contains(normalizedSearch) ||
                   (user.UserName != null && user.UserName.Contains(normalizedSearch)) ||
                   user.DisplayName.Contains(normalizedSearch))
            select new { Submission = submission, Version = version, User = user };

        return rows
            .OrderByDescending(row => row.Submission.SubmittedAtUtc)
            .ThenByDescending(row => row.Submission.CreatedAtUtc)
            .Select(row => new SubmissionExportRow(
                row.Submission.Id,
                row.Submission.FormId,
                row.Submission.FormVersionId,
                row.Submission.TrackingCode,
                row.User.UserName ?? string.Empty,
                row.User.DisplayName,
                row.Submission.Status,
                row.Submission.CreatedAtUtc,
                row.Submission.SubmittedAtUtc,
                row.Version.DefinitionJson,
                row.Submission.DataJson));
    }

    private static async Task<FormDefinition> GetAuthorizedFormAsync(
        PortalDbContext dbContext,
        Guid formId,
        FormActor actor,
        FormAccessRights rights,
        CancellationToken cancellationToken)
    {
        FormDefinition form = await dbContext.Forms.AsNoTracking().SingleOrDefaultAsync(
            candidate => candidate.Id == formId,
            cancellationToken) ?? throw new KeyNotFoundException("فرم یافت نشد.");
        if (!await FormAuthorizationEvaluator.HasRightsAsync(
                dbContext,
                form,
                actor,
                rights,
                cancellationToken))
        {
            throw new UnauthorizedAccessException("دسترسی لازم به گزارش این فرم وجود ندارد.");
        }

        return form;
    }

    private static SubmissionDetails ToDetails(SubmissionExportRow row) => new(
        row.Id,
        row.FormId,
        row.TrackingCode,
        row.UserUpn,
        row.UserDisplayName,
        row.Status,
        row.CreatedAtUtc,
        row.SubmittedAtUtc,
        FormSchemaSerializer.Deserialize(row.DefinitionJson),
        DeserializeValues(row.DataJson));

    private static List<ReportColumn> BuildReportColumns(IEnumerable<SubmissionExportRow> submissions)
    {
        Dictionary<string, ReportColumn> columns = new(StringComparer.OrdinalIgnoreCase);
        foreach (SubmissionExportRow submission in submissions)
        {
            FormSchemaDefinition schema = FormSchemaSerializer.Deserialize(submission.DefinitionJson);
            foreach (FormElementDefinition element in FlattenElements(schema).Where(element =>
                         !IsContentElement(element.Type) &&
                         element.Type is not FormElementType.Hidden))
            {
                columns.TryAdd(element.Key, new ReportColumn(element.Key, element.Label, element));
            }
        }

        return columns.Values.ToList();
    }

    private static IEnumerable<FormElementDefinition> FlattenElements(FormSchemaDefinition schema)
    {
        foreach (FormElementDefinition element in schema.Pages
                     .SelectMany(page => page.Sections)
                     .SelectMany(section => section.Elements))
        {
            yield return element;
        }
    }

    private static Dictionary<string, JsonElement> DeserializeValues(string json) =>
        JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(
            json,
            JsonOptions) ??
        new Dictionary<string, JsonElement>(StringComparer.OrdinalIgnoreCase);

    private static string FormatValue(FormElementDefinition element, JsonElement value)
    {
        if (value.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
        {
            return string.Empty;
        }

        if (element.Type is FormElementType.Select or FormElementType.Radio)
        {
            string? optionValue = value.ValueKind == JsonValueKind.String ? value.GetString() : null;
            return element.Options.FirstOrDefault(option => option.Value == optionValue)?.Label ??
                optionValue ?? value.GetRawText();
        }

        if (element.Type == FormElementType.MultiSelect && value.ValueKind == JsonValueKind.Array)
        {
            return string.Join("، ", value.EnumerateArray().Select(item =>
            {
                string? optionValue = item.ValueKind == JsonValueKind.String ? item.GetString() : null;
                return element.Options.FirstOrDefault(option => option.Value == optionValue)?.Label ??
                    optionValue ?? item.GetRawText();
            }));
        }

        if (value.ValueKind == JsonValueKind.String)
        {
            string storedValue = value.GetString() ?? string.Empty;
            if (element.Type == FormElementType.Date &&
                PersianDateTimeFormatter.TryParseStoredDateTime(storedValue, out DateTimeOffset date))
            {
                return PersianDateTimeFormatter.FormatDate(date);
            }

            if (element.Type == FormElementType.DateTime &&
                PersianDateTimeFormatter.TryParseStoredDateTime(storedValue, out DateTimeOffset dateTime))
            {
                return PersianDateTimeFormatter.FormatDateTime(dateTime);
            }

            if (element.Type == FormElementType.Time &&
                PersianDateTimeFormatter.TryParseStoredTime(storedValue, out TimeOnly time))
            {
                return PersianDateTimeFormatter.FormatTime(time);
            }
        }

        if (element.Type == FormElementType.DateRange && value.ValueKind == JsonValueKind.Object)
        {
            string start = value.TryGetProperty("start", out JsonElement startValue) &&
                startValue.ValueKind == JsonValueKind.String &&
                PersianDateTimeFormatter.TryParseStoredDateTime(startValue.GetString(), out DateTimeOffset startDate)
                    ? PersianDateTimeFormatter.FormatDate(startDate)
                    : "—";
            string end = value.TryGetProperty("end", out JsonElement endValue) &&
                endValue.ValueKind == JsonValueKind.String &&
                PersianDateTimeFormatter.TryParseStoredDateTime(endValue.GetString(), out DateTimeOffset endDate)
                    ? PersianDateTimeFormatter.FormatDate(endDate)
                    : "—";
            return $"از {start} تا {end}";
        }

        return value.ValueKind switch
        {
            JsonValueKind.String => value.GetString() ?? string.Empty,
            JsonValueKind.True => "بله",
            JsonValueKind.False => "خیر",
            JsonValueKind.Array => string.Join(" | ", value.EnumerateArray().Select(item => item.GetRawText())),
            JsonValueKind.Object => value.GetRawText(),
            _ => value.GetRawText()
        };
    }

    private static void SetSafeText(IXLCell cell, string? value)
    {
        string text = value ?? string.Empty;
        if (text.Length > 32_767)
        {
            text = text[..32_767];
        }

        if (text.Length > 0 && text[0] is '=' or '+' or '-' or '@')
        {
            text = $"'{text}";
        }

        cell.Value = text;
    }

    private static string GetStatusText(FormSubmissionStatus status) => status switch
    {
        FormSubmissionStatus.Draft => "پیش‌نویس",
        FormSubmissionStatus.Submitted => "ثبت نهایی",
        FormSubmissionStatus.Withdrawn => "پس‌گرفته‌شده",
        _ => status.ToString()
    };

    private static bool IsContentElement(FormElementType type) =>
        type is FormElementType.Heading or FormElementType.Paragraph or FormElementType.Divider or
            FormElementType.Alert;

    private static (int Page, int PageSize) NormalizePaging(int page, int pageSize) =>
        (Math.Max(1, page), Math.Clamp(pageSize, 1, 200));

    private static void EnsureValidActor(FormActor actor)
    {
        ArgumentNullException.ThrowIfNull(actor);
        ArgumentOutOfRangeException.ThrowIfEqual(actor.UserId, Guid.Empty);
        ArgumentException.ThrowIfNullOrWhiteSpace(actor.Upn);
        ArgumentException.ThrowIfNullOrWhiteSpace(actor.CorrelationId);
    }

    private sealed record SubmissionExportRow(
        Guid Id,
        Guid FormId,
        Guid FormVersionId,
        string TrackingCode,
        string UserUpn,
        string UserDisplayName,
        FormSubmissionStatus Status,
        DateTimeOffset CreatedAtUtc,
        DateTimeOffset? SubmittedAtUtc,
        string DefinitionJson,
        string DataJson);

    private sealed record ReportColumn(
        string Key,
        string Label,
        FormElementDefinition Element);
}
