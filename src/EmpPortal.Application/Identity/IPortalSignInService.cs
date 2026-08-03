namespace EmpPortal.Application.Identity;

public interface IPortalSignInService
{
    public Task<PortalSignInResult> SsoSignInAsync(
        string loginName,
        CancellationToken cancellationToken = default);

    public Task<PortalSignInResult> PasswordSignInAsync(
        string upn,
        string password,
        CancellationToken cancellationToken = default);
}
