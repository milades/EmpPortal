using EmpPortal.Application.Authorization;
using EmpPortal.Application.Hr;
using EmpPortal.Infrastructure.Persistence;
using EmpPortal.Infrastructure.Persistence.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Stimulsoft.Base;
using Stimulsoft.Report;

namespace EmpPortal.Infrastructure.Hr;

public sealed class PayslipReportService(
    IDbContextFactory<PortalDbContext> dbContextFactory,
    IPortalAccessEvaluator accessEvaluator,
    IPayslipSettingsService payslipSettingsService,
    IOptions<PayslipReportOptions> reportOptions,
    IHostEnvironment hostEnvironment) : IPayslipReportService
{
    private static readonly object LicenseLock = new();
    private static bool isLicenseConfigured;

    public async Task<PayslipPdfResult> RenderMyPayslipPdfAsync(
        PortalActor actor,
        int persianYear,
        int persianMonth,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(actor);
        if (persianMonth is < 1 or > 12)
        {
            throw new ArgumentOutOfRangeException(nameof(persianMonth), "ماه شمسی باید بین ۱ تا ۱۲ باشد.");
        }

        if (!await accessEvaluator.HasAccessAsync(actor, PortalResources.PayslipView, cancellationToken))
        {
            throw new UnauthorizedAccessException("اجازه مشاهده فیش حقوقی را ندارید.");
        }

        if (!await payslipSettingsService.IsPeriodVisibleToEmployeesAsync(persianYear, persianMonth, cancellationToken))
        {
            throw new InvalidOperationException("فیش حقوقی این دوره برای پرسنل فعال نیست.");
        }

        await using PortalDbContext dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        ApplicationUser? user = await dbContext.Users.AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == actor.UserId, cancellationToken);
        string? personnelCode = user?.PersonnelCode;
        if (string.IsNullOrWhiteSpace(personnelCode))
        {
            throw new InvalidOperationException(
                "کد پرسنلی برای حساب شما ثبت نشده است. از مدیر سامانه بخواهید کد پرسنلی را تنظیم کند.");
        }

        PayslipReportOptions options = reportOptions.Value;
        string templatePath = ResolvePath(options.TemplateRelativePath);
        if (!File.Exists(templatePath))
        {
            throw new InvalidOperationException(
                $"فایل گزارش فیش حقوقی یافت نشد: {options.TemplateRelativePath}");
        }

        EnsureLicense(options.LicenseKey);

        try
        {
            using StiReport report = new();
            report.Load(templatePath);
            TrySetVariable(report, options.PersonnelCodeVariable, personnelCode.Trim());
            TrySetVariable(report, options.PersianYearVariable, persianYear);
            TrySetVariable(report, options.PersianMonthVariable, persianMonth);
            report.Render(false);

            using MemoryStream stream = new();
            report.ExportDocument(StiExportFormat.Pdf, stream);
            byte[] content = stream.ToArray();
            if (content.Length == 0)
            {
                throw new InvalidOperationException("خروجی PDF فیش حقوقی خالی است.");
            }

            string fileName =
                $"payslip-{personnelCode.Trim()}-{persianYear}-{persianMonth:00}.pdf";
            return new PayslipPdfResult(content, fileName, "application/pdf");
        }
        catch (Exception exception) when (exception is not InvalidOperationException and not UnauthorizedAccessException
            and not ArgumentOutOfRangeException and not OperationCanceledException)
        {
            throw new InvalidOperationException(
                "تولید فیش حقوقی با خطا مواجه شد. صحت فایل گزارش و اتصال داخلی آن را بررسی کنید.",
                exception);
        }
    }

    private static void EnsureLicense(string? licenseKey)
    {
        if (string.IsNullOrWhiteSpace(licenseKey))
        {
            return;
        }

        lock (LicenseLock)
        {
            if (isLicenseConfigured)
            {
                return;
            }

            StiLicense.Key = licenseKey.Trim();
            isLicenseConfigured = true;
        }
    }

    private static void TrySetVariable(StiReport report, string? variableName, object value)
    {
        if (string.IsNullOrWhiteSpace(variableName))
        {
            return;
        }

        string name = variableName.Trim();
        if (!report.Dictionary.Variables.Contains(name))
        {
            return;
        }

        report[name] = value;
    }

    private string ResolvePath(string configuredPath) => Path.IsPathRooted(configuredPath)
        ? configuredPath
        : Path.GetFullPath(Path.Combine(hostEnvironment.ContentRootPath, configuredPath));
}
