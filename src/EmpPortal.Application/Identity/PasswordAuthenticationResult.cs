namespace EmpPortal.Application.Identity;

public enum PasswordAuthenticationFailure
{
    None = 0,
    InvalidCredentials = 1,
    Disabled = 2,
    Locked = 3,
    PasswordExpired = 4,
    Expired = 5,
    DirectoryUnavailable = 6
}

public sealed record PasswordAuthenticationResult
{
    private PasswordAuthenticationResult(
        EnterpriseIdentity? identity,
        PasswordAuthenticationFailure failure)
    {
        Identity = identity;
        Failure = failure;
    }

    public EnterpriseIdentity? Identity { get; }
    public PasswordAuthenticationFailure Failure { get; }
    public bool Succeeded => Identity is not null && Failure == PasswordAuthenticationFailure.None;

    public static PasswordAuthenticationResult Success(EnterpriseIdentity identity)
    {
        ArgumentNullException.ThrowIfNull(identity);
        return new PasswordAuthenticationResult(identity, PasswordAuthenticationFailure.None);
    }

    public static PasswordAuthenticationResult Failed(PasswordAuthenticationFailure failure)
    {
        if (failure == PasswordAuthenticationFailure.None)
        {
            throw new ArgumentOutOfRangeException(nameof(failure));
        }

        return new PasswordAuthenticationResult(null, failure);
    }
}
