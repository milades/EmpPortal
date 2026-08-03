namespace EmpPortal.Application.Identity;

public sealed record EnterpriseIdentity(
    Guid ObjectGuid,
    string Sid,
    string Upn,
    string DisplayName,
    string? Email,
    DirectoryAccountState State);
