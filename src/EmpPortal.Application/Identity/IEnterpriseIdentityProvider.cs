namespace EmpPortal.Application.Identity;

public interface IEnterpriseIdentityProvider
{
    public Task<PasswordAuthenticationResult> AuthenticatePasswordAsync(
        string upn,
        string password,
        CancellationToken cancellationToken = default);

    public Task<EnterpriseIdentity?> FindByLoginNameAsync(
        string loginName,
        CancellationToken cancellationToken = default);

    public Task<DirectoryAccountState> GetAccountStateAsync(
        Guid objectGuid,
        CancellationToken cancellationToken = default);
}
