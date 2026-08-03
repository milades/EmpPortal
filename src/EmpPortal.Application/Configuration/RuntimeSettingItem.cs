namespace EmpPortal.Application.Configuration;

public sealed record RuntimeSettingItem(
    string Key,
    string Value,
    string DisplayName,
    string Description,
    bool RequiresRestart,
    DateTimeOffset? UpdatedAtUtc);
