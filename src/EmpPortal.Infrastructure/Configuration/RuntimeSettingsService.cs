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
        Text("ActiveDirectory:DomainFqdn", "نام دامنه", "نام کامل DNS دامنه سازمانی", 253),
        Text("ActiveDirectory:BaseDn", "مبنای جستجوی AD", "Base DN برای جستجوی کاربران", 500),
        Integer("ActiveDirectory:LdapsPort", "پورت LDAPS", "پورت امن LDAP", 1, 65535),
        Integer("ActiveDirectory:OperationTimeoutSeconds", "مهلت پاسخ AD", "مهلت هر عملیات بر حسب ثانیه", 1, 60),
        Boolean("Authentication:SsoEnabled", "ورود یکپارچه", "فعال‌بودن Windows SSO"),
        Boolean("Authentication:ManualLoginEnabled", "ورود دستی", "فعال‌بودن ورود UPN و رمز عبور"),
        Integer("Session:AbsoluteMinutes", "حداکثر نشست", "حداکثر عمر نشست بر حسب دقیقه", 30, 720),
        Integer("Session:IdleMinutes", "مهلت عدم فعالیت", "مهلت عدم فعالیت بر حسب دقیقه", 5, 180),
        Integer("Session:MaxConcurrentPerUser", "نشست هم‌زمان", "حداکثر نشست فعال هر کاربر", 1, 10),
        Integer("Session:AdRevalidationSeconds", "بازاعتبارسنجی AD", "فاصله کنترل وضعیت حساب بر حسب ثانیه", 15, 60),
        Integer("Jwt:AccessTokenMinutes", "عمر JWT", "عمر Access Token بر حسب دقیقه", 1, 15),
        Text("Portal:Title", "عنوان پرتال", "عنوان نمایشی سامانه", 120)
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
                definition.RequiresRestart,
                storedSetting?.UpdatedAtUtc);
        }).ToArray();
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
        int maximumLength) =>
        new(
            key,
            displayName,
            description,
            RequiresRestart: true,
            value => !string.IsNullOrWhiteSpace(value) && value.Length <= maximumLength);

    private static RuntimeSettingDefinition Integer(
        string key,
        string displayName,
        string description,
        int minimum,
        int maximum) =>
        new(
            key,
            displayName,
            description,
            RequiresRestart: true,
            value => int.TryParse(
                value,
                System.Globalization.NumberStyles.Integer,
                System.Globalization.CultureInfo.InvariantCulture,
                out int parsed) && parsed >= minimum && parsed <= maximum);

    private static RuntimeSettingDefinition Boolean(
        string key,
        string displayName,
        string description) =>
        new(
            key,
            displayName,
            description,
            RequiresRestart: true,
            value => bool.TryParse(value, out _));
}
