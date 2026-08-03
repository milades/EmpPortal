using EmpPortal.Application.Identity;

namespace EmpPortal.Infrastructure.Identity;

public sealed record DevelopmentDirectoryAccount(
    Guid ObjectGuid,
    string Sid,
    string Upn,
    string DisplayName,
    string? Email,
    DirectoryAccountState State = DirectoryAccountState.Enabled);
