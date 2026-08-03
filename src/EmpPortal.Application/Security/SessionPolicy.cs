namespace EmpPortal.Application.Security;

public sealed record SessionPolicy(
    TimeSpan AbsoluteLifetime,
    TimeSpan IdleTimeout,
    int MaxConcurrentSessions,
    TimeSpan AccessTokenLifetime,
    TimeSpan DirectoryRevalidationInterval)
{
    public static SessionPolicy Default { get; } = new(
        TimeSpan.FromHours(3),
        TimeSpan.FromMinutes(30),
        3,
        TimeSpan.FromMinutes(5),
        TimeSpan.FromSeconds(60));

    public void EnsureValid()
    {
        if (AbsoluteLifetime <= TimeSpan.Zero)
        {
            throw new InvalidOperationException("Absolute session lifetime must be positive.");
        }

        if (IdleTimeout <= TimeSpan.Zero || IdleTimeout >= AbsoluteLifetime)
        {
            throw new InvalidOperationException("Idle timeout must be positive and less than the absolute lifetime.");
        }

        if (MaxConcurrentSessions <= 0)
        {
            throw new InvalidOperationException("At least one concurrent session must be allowed.");
        }

        if (AccessTokenLifetime <= TimeSpan.Zero || AccessTokenLifetime >= AbsoluteLifetime)
        {
            throw new InvalidOperationException("Access token lifetime must be positive and less than the session lifetime.");
        }

        if (DirectoryRevalidationInterval <= TimeSpan.Zero || DirectoryRevalidationInterval > TimeSpan.FromMinutes(1))
        {
            throw new InvalidOperationException("Directory revalidation must satisfy the one-minute revocation SLA.");
        }
    }
}
