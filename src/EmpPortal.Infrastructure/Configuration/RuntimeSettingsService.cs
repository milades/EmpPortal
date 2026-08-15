using System.Diagnostics;
using EmpPortal.Application.Configuration;
using EmpPortal.Domain.Auditing;
using EmpPortal.Domain.Configuration;
using EmpPortal.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace EmpPortal.Infrastructure.Configuration;

public sealed class RuntimeSettingsService(
    IDbContextFactory<PortalDbContext> dbContextFactory,
    IConfiguration configuration,
    TimeProvider timeProvider) : IRuntimeSettingsService
{
    private static readonly RuntimeSettingDefinition[] Definitions =
    [
        Text("ActiveDirectory:DomainFqdn", "نام دامنه", "نام کامل DNS دامنه سازمانی", "Active Directory", 253),
        Text("ActiveDirectory:BaseDn", "مبنای جستجوی AD", "Base DN برای جستجوی کاربران", "Active Directory", 500),
        Text("ActiveDirectory:DomainControllers:0", "کنترل‌کننده دامنه اصلی", "FQDN نخستین Domain Controller دارای LDAPS", "Active Directory", 253),
        Text("ActiveDirectory:DomainControllers:1", "کنترل‌کننده دامنه جایگزین", "FQDN دومین Domain Controller برای Failover", "Active Directory", 253),
        Integer("ActiveDirectory:LdapsPort", "پورت LDAPS", "پورت امن LDAP", "Active Directory", 1, 65535),
        Integer("ActiveDirectory:OperationTimeoutSeconds", "مهلت پاسخ AD", "مهلت هر عملیات بر حسب ثانیه", "Active Directory", 1, 60),
        Boolean("Authentication:SsoEnabled", "ورود یکپارچه", "فعال‌بودن Windows SSO", "احراز هویت"),
        Boolean("Authentication:ManualLoginEnabled", "ورود دستی", "فعال‌بودن ورود UPN و رمز عبور", "احراز هویت"),
        Integer("Login:AttemptLimit", "سقف تلاش ورود", "حداکثر تلاش ورود دستی در بازه کنترل", "احراز هویت", 1, 20),
        Integer("Login:AttemptWindowMinutes", "بازه کنترل ورود", "طول بازه محدودسازی ورود بر حسب دقیقه", "احراز هویت", 1, 60),
        Integer("Session:AbsoluteMinutes", "حداکثر نشست", "حداکثر عمر نشست بر حسب دقیقه", "نشست کاربران", 30, 720),
        Integer("Session:IdleMinutes", "مهلت عدم فعالیت", "مهلت عدم فعالیت بر حسب دقیقه", "نشست کاربران", 5, 180),
        Integer("Session:MaxConcurrentPerUser", "نشست هم‌زمان", "حداکثر نشست فعال هر کاربر", "نشست کاربران", 1, 10),
        Integer("Session:AdRevalidationSeconds", "بازاعتبارسنجی AD", "فاصله کنترل وضعیت حساب بر حسب ثانیه", "نشست کاربران", 15, 60),
        Integer("Jwt:AccessTokenMinutes", "عمر JWT", "عمر Access Token بر حسب دقیقه", "توکن API", 1, 15),
        Text("Portal:Title", "عنوان پرتال", "عنوان نمایشی سامانه", "ظاهر و لینک‌ها", 120),
        Text(PortalRuntimeSettingKeys.LoginFooterText, "متن پایین صفحه ورود", "متن نمایشی پایین فرم ورود؛ برای پنهان‌کردن خالی بگذارید", "ظاهر و لینک‌ها", 250, required: false, requiresRestart: false),
        Text(PortalRuntimeSettingKeys.BrandMark, "نشان منوی اصلی", "حرف یا نویسه کنار عنوان در منوی اصلی داشبورد؛ حداکثر 5 نویسه. خالی = نمایش پیش‌فرض اِ", "ظاهر و لینک‌ها", 5, required: false, requiresRestart: false),
        Url(PortalRuntimeSettingKeys.FoodReservationExternalUrl, "نشانی رزرو غذا", "آدرس سامانه خارجی رزرو غذا", "ظاهر و لینک‌ها", 2000, required: false, requiresRestart: false),
        Select("Forms:Pdf:License", "مجوز QuestPDF", "نوع مجوز تأییدشده برای تولید PDF", "فرم و PDF", ["Community", "Professional", "Enterprise"]),
        Text("Forms:Pdf:RegularFontPath", "فونت معمولی PDF", "مسیر نسبی یا کامل فونت معمولی", "فرم و PDF", 1000),
        Text("Forms:Pdf:BoldFontPath", "فونت ضخیم PDF", "مسیر نسبی یا کامل فونت ضخیم", "فرم و PDF", 1000),
        Text("Payslip:Report:TemplateRelativePath", "قالب فیش حقوقی", "مسیر فایل گزارش Stimulsoft", "فیش حقوقی", 1000),
        Text("Payslip:Report:PersonnelCodeVariable", "متغیر کد پرسنلی", "نام متغیر کد پرسنلی در قالب", "فیش حقوقی", 200),
        Text("Payslip:Report:PersianYearVariable", "متغیر سال", "نام متغیر سال شمسی در قالب", "فیش حقوقی", 200),
        Text("Payslip:Report:PersianMonthVariable", "متغیر ماه", "نام متغیر ماه شمسی در قالب", "فیش حقوقی", 200),
        Text("ExternalData:Benefits:ViewName", "View مزایا", "نام کامل View مزایا", "داده‌های خارجی", 300),
        Text("ExternalData:Benefits:PersonnelCodeColumn", "ستون کد پرسنلی مزایا", "نام ستون کد پرسنلی", "داده‌های خارجی", 200),
        Text("ExternalData:Assets:ViewName", "View اموال", "نام کامل View اموال", "داده‌های خارجی", 300),
        Text("ExternalData:Assets:PersonnelCodeColumn", "ستون کد پرسنلی اموال", "نام ستون کد پرسنلی", "داده‌های خارجی", 200),
        Select("Logging:LogLevel:Default", "سطح لاگ عمومی", "حداقل سطح ثبت رویدادهای برنامه", "ثبت رویداد", ["Trace", "Debug", "Information", "Warning", "Error", "Critical", "None"]),
        Select("Logging:LogLevel:Microsoft.AspNetCore", "سطح لاگ ASP.NET Core", "حداقل سطح ثبت رویدادهای فریم‌ورک", "ثبت رویداد", ["Trace", "Debug", "Information", "Warning", "Error", "Critical", "None"])
    ];

    public async Task<IReadOnlyList<RuntimeSettingItem>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        await using PortalDbContext dbContext = await dbContextFactory.CreateDbContextAsync(
            cancellationToken);
        Dictionary<string, RuntimeSetting> storedSettings = await dbContext.RuntimeSettings
            .AsNoTracking()
            .ToDictionaryAsync(setting => setting.Key, StringComparer.OrdinalIgnoreCase, cancellationToken);

        return Definitions.Select(definition =>
        {
            storedSettings.TryGetValue(definition.Key, out RuntimeSetting? storedSetting);
            return new RuntimeSettingItem(
                definition.Key,
                storedSetting?.Value ?? configuration[definition.Key] ?? string.Empty,
                definition.DisplayName,
                definition.Description,
                definition.Group,
                definition.InputKind,
                definition.IsRequired,
                definition.IsSensitive,
                definition.AllowedValues,
                definition.RequiresRestart,
                storedSetting?.UpdatedAtUtc);
        }).ToArray();
    }

    public async Task<string?> GetValueAsync(
        string key,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        await using PortalDbContext dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        string? storedValue = await dbContext.RuntimeSettings.AsNoTracking()
            .Where(setting => setting.Key == key)
            .Select(setting => setting.Value)
            .FirstOrDefaultAsync(cancellationToken);
        string? value = storedValue ?? configuration[key];
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    public async Task UpdateAsync(
        string key,
        string value,
        Guid actorUserId,
        string actorUpn,
        string correlationId,
        string? ipAddress,
        CancellationToken cancellationToken = default)
    {
        RuntimeSettingDefinition definition = Definitions.SingleOrDefault(candidate =>
            string.Equals(candidate.Key, key, StringComparison.OrdinalIgnoreCase)) ??
            throw new ArgumentException("The runtime setting is not editable.", nameof(key));

        string normalizedValue = value.Trim();
        if (!definition.IsValid(normalizedValue))
        {
            throw new ArgumentException("The runtime setting value is invalid.", nameof(value));
        }

        await using PortalDbContext dbContext = await dbContextFactory.CreateDbContextAsync(
            cancellationToken);
        RuntimeSetting? setting = await dbContext.RuntimeSettings.SingleOrDefaultAsync(
            candidate => candidate.Key == definition.Key,
            cancellationToken);
        DateTimeOffset nowUtc = timeProvider.GetUtcNow();
        string? previousValue = setting?.Value;

        if (setting is null)
        {
            setting = RuntimeSetting.Create(
                definition.Key,
                normalizedValue,
                nowUtc,
                actorUserId);
            dbContext.RuntimeSettings.Add(setting);
        }
        else
        {
            setting.Update(normalizedValue, nowUtc, actorUserId);
        }

        dbContext.AuditEvents.Add(AuditEvent.Create(
            nowUtc,
            "RuntimeSettingChanged",
            "Succeeded",
            actorUserId,
            actorUpn,
            definition.Key,
            string.IsNullOrWhiteSpace(correlationId)
                ? Activity.Current?.Id ?? Guid.NewGuid().ToString("D")
                : correlationId,
            ipAddress,
            System.Text.Json.JsonSerializer.Serialize(new
            {
                PreviousValue = previousValue,
                NewValue = normalizedValue,
                definition.RequiresRestart
            })));

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static RuntimeSettingDefinition Text(
        string key,
        string displayName,
        string description,
        string group,
        int maximumLength,
        bool required = true,
        bool requiresRestart = true) =>
        new(
            key,
            displayName,
            description,
            group,
            RuntimeSettingInputKind.Text,
            required,
            false,
            [],
            requiresRestart,
            value => (!required || !string.IsNullOrWhiteSpace(value)) && value.Length <= maximumLength);

    private static RuntimeSettingDefinition Integer(
        string key,
        string displayName,
        string description,
        string group,
        int minimum,
        int maximum) =>
        new(
            key,
            displayName,
            description,
            group,
            RuntimeSettingInputKind.Number,
            true,
            false,
            [],
            RequiresRestart: true,
            value => int.TryParse(
                value,
                System.Globalization.NumberStyles.Integer,
                System.Globalization.CultureInfo.InvariantCulture,
                out int parsed) && parsed >= minimum && parsed <= maximum);

    private static RuntimeSettingDefinition Boolean(
        string key,
        string displayName,
        string description,
        string group) =>
        new(
            key,
            displayName,
            description,
            group,
            RuntimeSettingInputKind.Boolean,
            true,
            false,
            ["true", "false"],
            RequiresRestart: true,
            value => bool.TryParse(value, out _));

    private static RuntimeSettingDefinition Url(
        string key,
        string displayName,
        string description,
        string group,
        int maximumLength,
        bool required,
        bool requiresRestart = true) =>
        new(
            key,
            displayName,
            description,
            group,
            RuntimeSettingInputKind.Url,
            required,
            false,
            [],
            requiresRestart,
            value =>
            {
                if (string.IsNullOrWhiteSpace(value))
                {
                    return !required;
                }

                return value.Length <= maximumLength &&
                    Uri.TryCreate(value, UriKind.Absolute, out Uri? uri) &&
                    (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
            });

    private static RuntimeSettingDefinition Select(
        string key,
        string displayName,
        string description,
        string group,
        IReadOnlyList<string> allowedValues) =>
        new(
            key,
            displayName,
            description,
            group,
            RuntimeSettingInputKind.Select,
            true,
            false,
            allowedValues,
            RequiresRestart: true,
            value => allowedValues.Contains(value, StringComparer.OrdinalIgnoreCase));
}
