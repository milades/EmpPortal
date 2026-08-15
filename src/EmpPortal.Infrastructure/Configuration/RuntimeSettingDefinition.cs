using EmpPortal.Application.Configuration;

namespace EmpPortal.Infrastructure.Configuration;

internal sealed record RuntimeSettingDefinition(
    string Key,
    string DisplayName,
    string Description,
    string Group,
    RuntimeSettingInputKind InputKind,
    bool IsRequired,
    bool IsSensitive,
    IReadOnlyList<string> AllowedValues,
    bool RequiresRestart,
    Func<string, bool> IsValid);
