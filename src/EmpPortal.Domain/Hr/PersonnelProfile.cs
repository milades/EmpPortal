namespace EmpPortal.Domain.Hr;

public sealed class PersonnelProfile
{
    private PersonnelProfile()
    {
    }

    public Guid UserId { get; private set; }

    public string? InternalPhone { get; private set; }

    public Guid UpdatedByUserId { get; private set; }

    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public static PersonnelProfile Create(Guid userId, Guid actorUserId, DateTimeOffset nowUtc)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(userId, Guid.Empty);
        ArgumentOutOfRangeException.ThrowIfEqual(actorUserId, Guid.Empty);
        return new PersonnelProfile
        {
            UserId = userId,
            UpdatedByUserId = actorUserId,
            UpdatedAtUtc = nowUtc
        };
    }

    public void SetInternalPhone(string? internalPhone, Guid actorUserId, DateTimeOffset nowUtc)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(actorUserId, Guid.Empty);
        string? normalized = string.IsNullOrWhiteSpace(internalPhone) ? null : internalPhone.Trim();
        if (normalized is not null)
        {
            ArgumentOutOfRangeException.ThrowIfGreaterThan(normalized.Length, 32, nameof(internalPhone));
            if (normalized.Any(digit => digit is < '0' or > '9'))
            {
                throw new ArgumentException("شماره تلفن داخلی فقط می‌تواند شامل ارقام باشد.", nameof(internalPhone));
            }
        }

        InternalPhone = normalized;
        UpdatedByUserId = actorUserId;
        UpdatedAtUtc = nowUtc;
    }
}
