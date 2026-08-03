using EmpPortal.Application.Identity;

namespace EmpPortal.Infrastructure.Identity;

public sealed class DevelopmentEnterpriseIdentityProvider : IEnterpriseIdentityProvider
{
    private readonly IReadOnlyDictionary<string, DevelopmentDirectoryAccount> _accountsByUpn;
    private readonly bool _acceptAnyNonEmptyPassword;

    public DevelopmentEnterpriseIdentityProvider(
        IEnumerable<DevelopmentDirectoryAccount> accounts,
        bool acceptAnyNonEmptyPassword)
    {
        ArgumentNullException.ThrowIfNull(accounts);

        _accountsByUpn = accounts.ToDictionary(
            account => NormalizeUpn(account.Upn),
            StringComparer.OrdinalIgnoreCase);
        _acceptAnyNonEmptyPassword = acceptAnyNonEmptyPassword;
    }

    public Task<PasswordAuthenticationResult> AuthenticatePasswordAsync(
        string upn,
        string password,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!_accountsByUpn.TryGetValue(NormalizeUpn(upn), out DevelopmentDirectoryAccount? account) ||
            !_acceptAnyNonEmptyPassword ||
            string.IsNullOrWhiteSpace(password))
        {
            return Task.FromResult(PasswordAuthenticationResult.Failed(
                PasswordAuthenticationFailure.InvalidCredentials));
        }

        PasswordAuthenticationFailure failure = account.State switch
        {
            DirectoryAccountState.Enabled => PasswordAuthenticationFailure.None,
            DirectoryAccountState.Disabled => PasswordAuthenticationFailure.Disabled,
            DirectoryAccountState.Locked => PasswordAuthenticationFailure.Locked,
            DirectoryAccountState.PasswordExpired => PasswordAuthenticationFailure.PasswordExpired,
            DirectoryAccountState.Expired => PasswordAuthenticationFailure.Expired,
            _ => PasswordAuthenticationFailure.DirectoryUnavailable
        };

        if (failure != PasswordAuthenticationFailure.None)
        {
            return Task.FromResult(PasswordAuthenticationResult.Failed(failure));
        }

        return Task.FromResult(PasswordAuthenticationResult.Success(ToIdentity(account)));
    }

    public Task<EnterpriseIdentity?> FindByLoginNameAsync(
        string loginName,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        DevelopmentDirectoryAccount? account = _accountsByUpn.Values.FirstOrDefault(candidate =>
            string.Equals(candidate.Upn, loginName, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(candidate.Sid, loginName, StringComparison.OrdinalIgnoreCase));

        return Task.FromResult(account is null ? null : ToIdentity(account));
    }

    public Task<DirectoryAccountState> GetAccountStateAsync(
        Guid objectGuid,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        DevelopmentDirectoryAccount? account = _accountsByUpn.Values.FirstOrDefault(
            candidate => candidate.ObjectGuid == objectGuid);

        return Task.FromResult(account?.State ?? DirectoryAccountState.Unavailable);
    }

    private static EnterpriseIdentity ToIdentity(DevelopmentDirectoryAccount account) =>
        new(
            account.ObjectGuid,
            account.Sid,
            NormalizeUpn(account.Upn),
            account.DisplayName,
            account.Email,
            account.State);

    private static string NormalizeUpn(string upn)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(upn);
        return upn.Trim().ToLowerInvariant();
    }
}
