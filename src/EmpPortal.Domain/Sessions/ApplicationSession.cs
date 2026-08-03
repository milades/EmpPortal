namespace EmpPortal.Domain.Sessions;

public sealed class ApplicationSession
{
    private ApplicationSession(
        Guid id,
        Guid userId,
        DateTimeOffset createdAtUtc,
        DateTimeOffset absoluteExpiresAtUtc,
        DateTimeOffset idleExpiresAtUtc)
    {
        Id = id;
        UserId = userId;
        CreatedAtUtc = createdAtUtc;
        LastActivityAtUtc = createdAtUtc;
        AbsoluteExpiresAtUtc = absoluteExpiresAtUtc;
        IdleExpiresAtUtc = idleExpiresAtUtc;
    }

    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset LastActivityAtUtc { get; private set; }
    public DateTimeOffset AbsoluteExpiresAtUtc { get; private set; }
    public DateTimeOffset IdleExpiresAtUtc { get; private set; }
    public DateTimeOffset? RevokedAtUtc { get; private set; }
    public string? RevocationReason { get; private set; }
    public bool IsRevoked => RevokedAtUtc.HasValue;

    public static ApplicationSession Create(
        Guid userId,
        DateTimeOffset nowUtc,
        TimeSpan absoluteLifetime,
        TimeSpan idleLifetime)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(userId, Guid.Empty);

        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(absoluteLifetime, TimeSpan.Zero);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(idleLifetime, TimeSpan.Zero);

        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(idleLifetime, absoluteLifetime);

        return new ApplicationSession(
            Guid.NewGuid(),
            userId,
            nowUtc,
            nowUtc.Add(absoluteLifetime),
            nowUtc.Add(idleLifetime));
    }

    public bool IsActiveAt(DateTimeOffset nowUtc) =>
        !IsRevoked && nowUtc < AbsoluteExpiresAtUtc && nowUtc < IdleExpiresAtUtc;

    public void RecordActivity(DateTimeOffset nowUtc, TimeSpan idleLifetime)
    {
        if (!IsActiveAt(nowUtc))
        {
            throw new InvalidOperationException("An inactive session cannot be extended.");
        }

        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(idleLifetime, TimeSpan.Zero);

        LastActivityAtUtc = nowUtc;
        DateTimeOffset proposedIdleExpiration = nowUtc.Add(idleLifetime);
        IdleExpiresAtUtc = proposedIdleExpiration < AbsoluteExpiresAtUtc
            ? proposedIdleExpiration
            : AbsoluteExpiresAtUtc;
    }

    public void Revoke(DateTimeOffset nowUtc, string reason)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);

        if (IsRevoked)
        {
            return;
        }

        RevokedAtUtc = nowUtc;
        RevocationReason = reason.Trim();
    }
}
