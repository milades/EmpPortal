namespace EmpPortal.Application.Configuration;

public sealed record RuntimeSettingItem(
    string Key,
    string Value,
    string DisplayName,
    string Description,
    string Group,
    RuntimeSettingInputKind InputKind,
    bool IsRequired,
    bool IsSensitive,
    IReadOnlyList<string> AllowedValues,
    bool RequiresRestart,
    DateTimeOffset? UpdatedAtUtc);
