namespace EmpPortal.Application.Identity;

public enum PortalSignInStatus
{
    Succeeded = 0,
    InvalidCredentials = 1,
    AccountNotAllowed = 2,
    DirectoryUnavailable = 3,
    IdentityStoreFailure = 4
}

public sealed record PortalSignInResult(PortalSignInStatus Status)
{
    public bool Succeeded => Status == PortalSignInStatus.Succeeded;
}
