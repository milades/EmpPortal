namespace EmpPortal.Infrastructure.Configuration;

internal sealed record RuntimeSettingDefinition(
    string Key,
    string DisplayName,
    string Description,
    bool RequiresRestart,
    Func<string, bool> IsValid);
